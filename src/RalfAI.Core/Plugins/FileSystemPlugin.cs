using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace RalfAI.Plugins;

/// <summary>
/// Gives the agent read/write access to the target project folder.
/// All paths are resolved relative to the root and protected against traversal.
/// Reads, listings and searches are bounded so one call cannot flood the model's context.
/// </summary>
public sealed class FileSystemPlugin(string rootFolder)
{
    private const int MaxListEntries = 500;
    private const int MaxReadChars = 100_000;
    private const int MaxSearchFileBytes = 1024 * 1024;
    private const int MaxLineChars = 200;
    private const int MaxReplaceFiles = 200;

    // Build output, VCS and tooling folders: never useful to list or search, and large.
    private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".vs", ".idea", ".ralfai", "bin", "obj", "node_modules", "packages",
    };

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>All file tools, named <c>FileSystem_*</c> - the single place contexts and profiles take them from.</summary>
    public IEnumerable<AITool> CreateTools() =>
    [
        AIFunctionFactory.Create(WriteFile,   name: "FileSystem_WriteFile"),
        AIFunctionFactory.Create(EditFile,    name: "FileSystem_EditFile"),
        AIFunctionFactory.Create(ReplaceInFiles, name: "FileSystem_ReplaceInFiles"),
        AIFunctionFactory.Create(ReadFile,    name: "FileSystem_ReadFile"),
        AIFunctionFactory.Create(ListFiles,   name: "FileSystem_ListFiles"),
        AIFunctionFactory.Create(FindFiles,   name: "FileSystem_FindFiles"),
        AIFunctionFactory.Create(SearchFiles, name: "FileSystem_SearchFiles"),
        AIFunctionFactory.Create(DeleteFile,  name: "FileSystem_DeleteFile"),
    ];

    // ── Write ────────────────────────────────────────────────────────────────

    [Description("Create a new file, or completely replace one, in the target project folder. Parent directories are created automatically. " +
                 "To change part of an existing file use EditFile instead - it is faster and does not risk losing the rest.")]
    public Task<string> WriteFile(
        [Description("Relative path from project root, e.g. 'src/MyApp/Program.cs'")] string relativePath,
        [Description("Complete file content to write")] string content)
    {
        var fullPath = ResolveSafe(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        // New files are written without a BOM; an existing file keeps the one it had.
        var keepBom = File.Exists(fullPath) && HasBom(fullPath);
        FileChangeNotifier.BeforeChange(Root, fullPath);
        File.WriteAllText(fullPath, content, keepBom ? new UTF8Encoding(true) : Utf8NoBom);
        return Task.FromResult($"OK — written {relativePath} ({content.Length} chars)");
    }

    [Description("Change an existing file by replacing an exact piece of text with another. oldText must match the file exactly " +
                 "(including indentation) and appear once - add surrounding lines to make it unique, or set replaceAll to change every occurrence. " +
                 "Line endings and BOM of the file are preserved.")]
    public Task<string> EditFile(
        [Description("Relative path of an existing file")] string relativePath,
        [Description("The exact text to find; copy it from the file")] string oldText,
        [Description("The text to put in its place (may be empty to delete oldText)")] string newText,
        [Description("Replace every occurrence instead of requiring exactly one")] bool replaceAll = false)
    {
        var fullPath = ResolveSafe(relativePath);
        if (!File.Exists(fullPath)) return Task.FromResult($"ERROR: file not found — {relativePath}. Use WriteFile to create it.");
        if (string.IsNullOrEmpty(oldText)) return Task.FromResult("ERROR: oldText is empty. To create or replace a whole file use WriteFile.");

        var bytes = File.ReadAllBytes(fullPath);
        var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        var text = Encoding.UTF8.GetString(bytes, hasBom ? 3 : 0, bytes.Length - (hasBom ? 3 : 0));

        // The model usually writes "\n"; match the file's own line-ending style.
        var crlf = text.Contains("\r\n", StringComparison.Ordinal);
        string Match(string s) => crlf
            ? s.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", "\r\n", StringComparison.Ordinal)
            : s.Replace("\r\n", "\n", StringComparison.Ordinal);
        var find = Match(oldText);
        var replacement = Match(newText);

        var count = CountOccurrences(text, find);
        if (count == 0)
            return Task.FromResult($"ERROR: oldText not found in {relativePath}. It must match exactly, including whitespace. {NearestLineHint(text, find)}" +
                                   "Read the region with ReadFile (startLine/endLine) and copy the text from there.");
        if (count > 1 && !replaceAll)
            return Task.FromResult($"ERROR: oldText appears {count} times in {relativePath}. Include more surrounding lines to make it unique, or set replaceAll.");

        var updated = text.Replace(find, replacement, StringComparison.Ordinal);
        FileChangeNotifier.BeforeChange(Root, fullPath);
        File.WriteAllText(fullPath, updated, hasBom ? new UTF8Encoding(true) : Utf8NoBom);

        var removed = find.Count(c => c == '\n') + 1;
        var added = replacement.Count(c => c == '\n') + 1;
        return Task.FromResult($"OK — edited {relativePath} ({count} replacement{(count == 1 ? "" : "s")}, -{removed}/+{added} lines per replacement)");
    }

    [Description("Replace text in many files at once - for renames and repeated changes across the project (e.g. rename a method everywhere, change a constant). " +
                 "Matches literal text by default, or a regular expression when useRegex is set (replacement may use $1, $2…). " +
                 "Limit it with fileGlob. Returns how many replacements were made in which files. For a single change in one file use EditFile.")]
    public Task<string> ReplaceInFiles(
        [Description("Text to find (literal, or a regular expression when useRegex is true)")] string oldText,
        [Description("Replacement text")] string newText,
        [Description("Optional glob limiting the files, e.g. '**/*.cs' or 'src/**/*.ts'; empty for every text file")] string fileGlob = "",
        [Description("Treat oldText as a regular expression")] bool useRegex = false,
        [Description("Ignore upper/lower case")] bool ignoreCase = false)
    {
        if (string.IsNullOrEmpty(oldText)) return Task.FromResult("ERROR: oldText is empty.");

        var options = RegexOptions.CultureInvariant | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
        Regex regex;
        try { regex = new Regex(useRegex ? oldText : Regex.Escape(oldText), options, TimeSpan.FromSeconds(2)); }
        catch (ArgumentException ex) { return Task.FromResult($"ERROR: invalid regular expression - {ex.Message}"); }

        // Literal replacements must not interpret $ in newText.
        var replacement = useRegex ? newText : newText.Replace("$", "$$", StringComparison.Ordinal);
        var inGlob = string.IsNullOrWhiteSpace(fileGlob) ? (_ => true) : GlobFilter(fileGlob);

        var changes = new List<(string File, int Count)>();
        var total = 0;
        foreach (var file in Walk(Root, includeDirectories: false))
        {
            var rel = Relative(file);
            if (!inGlob(rel) || !IsSearchable(file)) continue;

            var bytes = File.ReadAllBytes(file);
            var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            var text = Encoding.UTF8.GetString(bytes, hasBom ? 3 : 0, bytes.Length - (hasBom ? 3 : 0));

            var count = 0;
            string updated;
            try
            {
                updated = regex.Replace(text, m => { count++; return m.Result(replacement); });
            }
            catch (RegexMatchTimeoutException) { continue; }

            if (count == 0 || updated == text) continue;
            FileChangeNotifier.BeforeChange(Root, file);
            File.WriteAllText(file, updated, hasBom ? new UTF8Encoding(true) : Utf8NoBom);
            changes.Add((rel, count));
            total += count;
            if (changes.Count >= MaxReplaceFiles) break;
        }

        if (changes.Count == 0) return Task.FromResult($"No matches for '{oldText}' - nothing changed.");
        var shown = string.Join('\n', changes.Take(40).Select(c => $"  {c.File} ({c.Count})"));
        return Task.FromResult($"OK — {total} replacement{(total == 1 ? "" : "s")} in {changes.Count} file{(changes.Count == 1 ? "" : "s")}:\n{shown}" +
                               (changes.Count > 40 ? $"\n  ... and {changes.Count - 40} more files" : "") +
                               (changes.Count >= MaxReplaceFiles ? $"\nStopped after {MaxReplaceFiles} files; run it again for the rest." : ""));
    }

    [Description("Delete a file from the target project folder.")]
    public Task<string> DeleteFile(
        [Description("Relative path from project root")] string relativePath)
    {
        var fullPath = ResolveSafe(relativePath);
        if (!File.Exists(fullPath))
            return Task.FromResult($"File not found — nothing deleted: {relativePath}");
        FileChangeNotifier.BeforeChange(Root, fullPath);
        File.Delete(fullPath);
        return Task.FromResult($"OK — deleted {relativePath}");
    }

    // ── Read / explore ───────────────────────────────────────────────────────

    [Description("Read a file from the target project folder. For large files pass startLine/endLine (1-based, inclusive) to read just the part you need " +
                 "- find the right lines with SearchFiles first. Without a range, files over about 100,000 characters are cut short.")]
    public Task<string> ReadFile(
        [Description("Relative path from project root")] string relativePath,
        [Description("First line to read (1-based), or 0 for the start of the file")] int startLine = 0,
        [Description("Last line to read (inclusive), or 0 for the end of the file")] int endLine = 0)
    {
        var fullPath = ResolveSafe(relativePath);
        if (!File.Exists(fullPath))
            return Task.FromResult($"ERROR: file not found — {relativePath}");

        var text = File.ReadAllText(fullPath);
        if (startLine <= 0 && endLine <= 0)
        {
            return Task.FromResult(text.Length <= MaxReadChars
                ? text
                : text[..MaxReadChars] + $"\n... [file continues: {text.Length - MaxReadChars:N0} more characters. Use startLine/endLine to read further, or SearchFiles to locate what you need.]");
        }

        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var from = Math.Max(1, startLine);
        var to = endLine <= 0 ? lines.Length : Math.Min(endLine, lines.Length);
        if (from > lines.Length) return Task.FromResult($"ERROR: {relativePath} has only {lines.Length} lines.");
        if (to < from) return Task.FromResult($"ERROR: endLine ({endLine}) is before startLine ({from}).");

        var selected = string.Join('\n', lines[(from - 1)..to]);
        if (selected.Length > MaxReadChars) selected = selected[..MaxReadChars] + "\n... [range truncated; ask for a smaller range]";
        return Task.FromResult($"[{relativePath}, lines {from}-{to} of {lines.Length}]\n{selected}");
    }

    [Description("List files and directories inside the target project folder (recursive). Build output and tooling folders (bin, obj, .git, node_modules…) " +
                 "are not listed, and very long listings are cut off - use FindFiles or SearchFiles to look for something specific.")]
    public Task<string> ListFiles(
        [Description("Relative subdirectory to list, or empty string for the project root")] string subdirectory = "")
    {
        var dir = string.IsNullOrWhiteSpace(subdirectory) ? Root : ResolveSafe(subdirectory);
        if (!Directory.Exists(dir))
            return Task.FromResult($"ERROR: directory not found — {subdirectory}");

        var entries = new List<string>();
        var truncated = false;
        foreach (var entry in Walk(dir, includeDirectories: true))
        {
            if (entries.Count >= MaxListEntries) { truncated = true; break; }
            entries.Add(Directory.Exists(entry) ? $"[DIR]  {Relative(entry)}" : $"[FILE] {Relative(entry)}");
        }

        if (entries.Count == 0) return Task.FromResult("(empty folder)");
        var sb = new StringBuilder(string.Join('\n', entries));
        if (truncated)
            sb.Append($"\n... listing stopped after {MaxListEntries} entries. List a subfolder, or use FindFiles / SearchFiles.");
        return Task.FromResult(sb.ToString());
    }

    [Description("Find files by name with a glob pattern, e.g. '**/*.cs', 'src/**/Program.cs', '*.md' (a pattern without '/' matches file names at any depth). " +
                 "Returns relative paths.")]
    public Task<string> FindFiles(
        [Description("Glob pattern: * any characters except '/', ** any folders, ? one character")] string pattern,
        [Description("Maximum number of paths to return")] int maxResults = 200)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return Task.FromResult("ERROR: pattern is empty.");
        var matches = GlobFilter(pattern);
        var found = new List<string>();
        var more = false;
        foreach (var file in Walk(Root, includeDirectories: false))
        {
            var rel = Relative(file);
            if (!matches(rel)) continue;
            if (found.Count >= maxResults) { more = true; break; }
            found.Add(rel);
        }

        if (found.Count == 0) return Task.FromResult($"No files match '{pattern}'.");
        return Task.FromResult(string.Join('\n', found) + (more ? $"\n... more matches not shown (limit {maxResults})." : ""));
    }

    [Description("Search the text of the files in the project for a regular expression (a plain word works too) and return 'path:line: text' for each match. " +
                 "Use it to locate code, then read just that part with ReadFile startLine/endLine. Binary and very large files are skipped.")]
    public Task<string> SearchFiles(
        [Description("Regular expression, or literal text")] string pattern,
        [Description("Optional glob to limit the files searched, e.g. '**/*.cs' or '*.md'; empty for all files")] string fileGlob = "",
        [Description("Ignore upper/lower case")] bool ignoreCase = true,
        [Description("Maximum number of matching lines to return")] int maxResults = 50)
    {
        if (string.IsNullOrEmpty(pattern)) return Task.FromResult("ERROR: pattern is empty.");

        var options = RegexOptions.CultureInvariant | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
        Regex regex;
        var note = "";
        try { regex = new Regex(pattern, options, TimeSpan.FromSeconds(2)); }
        catch (ArgumentException)
        {
            regex = new Regex(Regex.Escape(pattern), options, TimeSpan.FromSeconds(2));
            note = "(not a valid regular expression - searched as literal text)\n";
        }

        var inGlob = string.IsNullOrWhiteSpace(fileGlob) ? (_ => true) : GlobFilter(fileGlob);
        var results = new List<string>();
        var filesWithMatches = new HashSet<string>();
        var more = false;

        foreach (var file in Walk(Root, includeDirectories: false))
        {
            var rel = Relative(file);
            if (!inGlob(rel) || !IsSearchable(file)) continue;

            string[] lines;
            try { lines = File.ReadAllLines(file); }
            catch (IOException) { continue; }

            for (var i = 0; i < lines.Length; i++)
            {
                try { if (!regex.IsMatch(lines[i])) continue; }
                catch (RegexMatchTimeoutException) { break; }

                if (results.Count >= maxResults) { more = true; break; }
                filesWithMatches.Add(rel);
                var line = lines[i].Trim();
                results.Add($"{rel}:{i + 1}: {(line.Length > MaxLineChars ? line[..MaxLineChars] + "…" : line)}");
            }
            if (more) break;
        }

        if (results.Count == 0) return Task.FromResult($"{note}No matches for '{pattern}'.");
        return Task.FromResult($"{note}{string.Join('\n', results)}\n[{results.Count} match{(results.Count == 1 ? "" : "es")} in {filesWithMatches.Count} file{(filesWithMatches.Count == 1 ? "" : "s")}" +
                               (more ? $"; more not shown (limit {maxResults}) - narrow the pattern or fileGlob" : "") + "]");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string Root => Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootFolder));

    private string Relative(string path) => Path.GetRelativePath(Root, path).Replace('\\', '/');

    private string ResolveSafe(string relativePath)
    {
        var root = Root;
        var full = Path.GetFullPath(Path.Combine(root, relativePath));
        // "D:\proj" must not admit "D:\proj-other": compare against the root plus a separator.
        if (!full.Equals(root, StringComparison.OrdinalIgnoreCase) &&
            !full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Path traversal blocked: {relativePath}");
        return full;
    }

    /// <summary>Every file under <paramref name="root"/> that listings and searches would see (no build/VCS folders).</summary>
    internal static IEnumerable<string> WalkFiles(string root) => Walk(root, includeDirectories: false);

    /// <summary>Every folder under <paramref name="root"/> that listings would see.</summary>
    internal static IEnumerable<string> WalkDirectories(string root) => Walk(root, includeDirectories: true).Where(Directory.Exists);

    /// <summary>Depth-first, name-ordered walk that skips build/VCS folders (and Cargo's <c>target</c>).</summary>
    private static IEnumerable<string> Walk(string directory, bool includeDirectories)
    {
        IEnumerable<string> files, subdirectories;
        try
        {
            files = Directory.EnumerateFiles(directory).OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
            subdirectories = Directory.EnumerateDirectories(directory).OrderBy(d => d, StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var sub in subdirectories)
        {
            if (IsSkippedFolder(sub)) continue;
            if (includeDirectories) yield return sub;
            foreach (var inner in Walk(sub, includeDirectories)) yield return inner;
        }
        foreach (var file in files) yield return file;
    }

    private static bool IsSkippedFolder(string path)
    {
        var name = Path.GetFileName(path);
        if (SkippedFolders.Contains(name)) return true;
        // Cargo's build output is only noise when it sits next to a Cargo.toml.
        return name.Equals("target", StringComparison.OrdinalIgnoreCase)
            && File.Exists(Path.Combine(Path.GetDirectoryName(path)!, "Cargo.toml"));
    }

    private static bool HasBom(string path)
    {
        using var stream = File.OpenRead(path);
        Span<byte> head = stackalloc byte[3];
        return stream.Read(head) == 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF;
    }

    private static bool IsSearchable(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (info.Length > MaxSearchFileBytes) return false;
            using var stream = info.OpenRead();
            var buffer = new byte[Math.Min(4096, (int)info.Length)];
            var read = stream.Read(buffer, 0, buffer.Length);
            return !buffer.AsSpan(0, read).Contains((byte)0);
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        for (var i = text.IndexOf(value, StringComparison.Ordinal); i >= 0; i = text.IndexOf(value, i + value.Length, StringComparison.Ordinal))
            count++;
        return count;
    }

    /// <summary>When oldText is not found: where its first non-blank line appears, to help the model fix whitespace.</summary>
    private static string NearestLineHint(string text, string find)
    {
        var first = find.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0);
        if (first is null) return "";
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var index = Array.FindIndex(lines, l => l.Trim() == first);
        return index >= 0 ? $"Its first line does appear at line {index + 1}, so the difference is further down or in the whitespace. " : "";
    }

    /// <summary>Glob to predicate over relative '/'-separated paths; a pattern without '/' matches file names at any depth.</summary>
    internal static Func<string, bool> GlobFilter(string glob)
    {
        glob = glob.Trim().Replace('\\', '/');
        var nameOnly = !glob.Contains('/');
        var sb = new StringBuilder("^");
        for (var i = 0; i < glob.Length; i++)
        {
            var c = glob[i];
            if (c == '*' && i + 1 < glob.Length && glob[i + 1] == '*')
            {
                var followedBySlash = i + 2 < glob.Length && glob[i + 2] == '/';
                sb.Append(followedBySlash ? "(?:.*/)?" : ".*");
                i += followedBySlash ? 2 : 1;
            }
            else if (c == '*') sb.Append("[^/]*");
            else if (c == '?') sb.Append("[^/]");
            else sb.Append(Regex.Escape(c.ToString()));
        }
        sb.Append('$');

        var regex = new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return nameOnly
            ? path => regex.IsMatch(path[(path.LastIndexOf('/') + 1)..])
            : path => regex.IsMatch(path);
    }
}
