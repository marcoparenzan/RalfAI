using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RalfAI;

public enum FileChangeKind { Created, Modified, Deleted }

/// <param name="Restorable">False for changes made by an external command to a file that existed before: its old content was never saved.</param>
public sealed record FileChange(string Path, FileChangeKind Kind, int Added, int Removed, bool Restorable);

public sealed record TurnChanges(string Id, DateTimeOffset Time, string Label, IReadOnlyList<FileChange> Files)
{
    public int RestorableCount => Files.Count(f => f.Restorable);
}

/// <summary>What <see cref="UndoJournal.Undo"/> did.</summary>
public sealed record UndoReport(IReadOnlyList<string> Restored, IReadOnlyList<string> Removed, IReadOnlyList<string> Skipped);

/// <summary>
/// Tools announce a file they are about to change here, so the journal of that folder can save its old content first.
/// Keyed by folder, so plugins need no reference to the engine; without a registered journal it does nothing.
/// </summary>
public static class FileChangeNotifier
{
    private static readonly ConcurrentDictionary<string, Action<string>> Sinks = new(StringComparer.OrdinalIgnoreCase);

    private static string Key(string root) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));

    internal static void Register(string root, Action<string> beforeChange) => Sinks[Key(root)] = beforeChange;

    internal static void Unregister(string root, Action<string> beforeChange)
    {
        if (Sinks.TryGetValue(Key(root), out var current) && current == beforeChange) Sinks.TryRemove(Key(root), out _);
    }

    /// <summary>Call before creating, overwriting or deleting <paramref name="fullPath"/>.</summary>
    public static void BeforeChange(string root, string fullPath)
    {
        if (Sinks.TryGetValue(Key(root), out var sink)) sink(fullPath);
    }
}

/// <summary>
/// Records what the agent changes during a turn so it can be reverted with <see cref="Undo"/>.
/// Files changed through the file tools are saved just before the change (exact, no git needed).
/// Changes made by external commands (dotnet new, cargo new…) are found by comparing the folder before and after:
/// new files can be removed again, but files a command modified or deleted cannot be restored and are only reported.
/// Checkpoints live in <c>.ralfai/undo</c>; the last <c>keep</c> turns are kept.
/// </summary>
public sealed class UndoJournal : IDisposable
{
    private const int MaxManifestFiles = 50_000;
    private const long MaxDiffBytes = 1024 * 1024;

    private readonly string _root;
    private readonly string _undoRoot;
    private readonly int _keep;
    private readonly Action<string> _sink;
    private readonly object _gate = new();

    private sealed class Entry
    {
        public bool Existed;
        public bool BackedUp;
    }

    private sealed record Manifest(Dictionary<string, (long Size, long Ticks)> Files, HashSet<string> Directories);

    private sealed class ActiveTurn(string id, string label, string directory, Manifest? manifest)
    {
        public string Id { get; } = id;
        public string Label { get; } = label;
        public string Directory { get; } = directory;
        public Manifest? Manifest { get; } = manifest;
        public Dictionary<string, Entry> Recorded { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private ActiveTurn? _active;

    public UndoJournal(string root, int keep = 10)
    {
        _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        _undoRoot = Path.Combine(_root, ".ralfai", "undo");
        _keep = keep;
        _sink = Record;
        if (_keep > 0) FileChangeNotifier.Register(_root, _sink);
    }

    public void Dispose() => FileChangeNotifier.Unregister(_root, _sink);

    /// <summary>Number of turns that can still be reverted.</summary>
    public int Count => Directory.Exists(_undoRoot) ? Directory.EnumerateDirectories(_undoRoot).Count() : 0;

    // ── Recording ────────────────────────────────────────────────────────────

    public void BeginTurn(string label)
    {
        if (_keep <= 0) return;
        lock (_gate)
        {
            var id = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            _active = new ActiveTurn(id, label.Length > 200 ? label[..200] : label, Path.Combine(_undoRoot, id), TakeManifest());
        }
    }

    private void Record(string fullPath)
    {
        lock (_gate)
        {
            if (_active is null) return;
            var rel = Relative(fullPath);
            if (rel.StartsWith(".ralfai/", StringComparison.OrdinalIgnoreCase) || _active.Recorded.ContainsKey(rel)) return;

            // "Existed" means at the start of the turn: a file a command created earlier in this same turn did not,
            // even though it is on disk now. (Without a start manifest, fall back to the file's state right now.)
            var entry = new Entry { Existed = File.Exists(fullPath) && (_active.Manifest is null || _active.Manifest.Files.ContainsKey(rel)) };
            if (entry.Existed)
            {
                try
                {
                    var backup = BackupPath(_active.Directory, rel);
                    Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                    File.Copy(fullPath, backup, overwrite: true);
                    entry.BackedUp = true;
                }
                catch (IOException) { /* cannot back it up: it will be reported as not restorable */ }
            }
            _active.Recorded[rel] = entry;
        }
    }

    /// <summary>Finishes the turn: works out what changed, stores the checkpoint, and returns the changes (null if none).</summary>
    public TurnChanges? EndTurn()
    {
        ActiveTurn? turn;
        lock (_gate) { turn = _active; _active = null; }
        if (turn is null) return null;

        try
        {
            var files = new List<FileChange>();
            var meta = new List<MetaEntry>();

            foreach (var (rel, entry) in turn.Recorded)
            {
                var full = FullPath(rel);
                var exists = File.Exists(full);
                string? backup = entry.BackedUp ? BackupPath(turn.Directory, rel) : null;

                if (entry.Existed && exists)
                {
                    if (backup is null) { files.Add(new(rel, FileChangeKind.Modified, 0, 0, false)); continue; }
                    if (FilesEqual(backup, full)) { TryDelete(backup); continue; }
                    var (added, removed) = CountLines(backup, full);
                    files.Add(new(rel, FileChangeKind.Modified, added, removed, true));
                    meta.Add(new(rel, FileChangeKind.Modified, true, Hash(full)));
                }
                else if (entry.Existed)
                {
                    var restorable = backup is not null;
                    files.Add(new(rel, FileChangeKind.Deleted, 0, CountLines(backup, null).Removed, restorable));
                    if (restorable) meta.Add(new(rel, FileChangeKind.Deleted, true, null));
                }
                else if (exists)
                {
                    files.Add(new(rel, FileChangeKind.Created, CountLines(null, full).Added, 0, true));
                    meta.Add(new(rel, FileChangeKind.Created, true, Hash(full)));
                }
            }

            // What external commands did: compare the folder with how it looked at the start of the turn.
            var after = turn.Manifest is null ? null : TakeManifest();
            var newDirectories = new List<string>();
            if (turn.Manifest is not null && after is not null)
            {
                newDirectories.AddRange(after.Directories.Where(d => !turn.Manifest.Directories.Contains(d)));
                foreach (var (rel, now) in after.Files)
                {
                    if (turn.Recorded.ContainsKey(rel)) continue;
                    if (!turn.Manifest.Files.TryGetValue(rel, out var before))
                    {
                        files.Add(new(rel, FileChangeKind.Created, CountLines(null, FullPath(rel)).Added, 0, true));
                        meta.Add(new(rel, FileChangeKind.Created, true, Hash(FullPath(rel))));
                    }
                    else if (before != now)
                        files.Add(new(rel, FileChangeKind.Modified, 0, 0, false));
                }
                foreach (var rel in turn.Manifest.Files.Keys.Where(r => !after.Files.ContainsKey(r) && !turn.Recorded.ContainsKey(r)))
                    files.Add(new(rel, FileChangeKind.Deleted, 0, 0, false));
            }

            if (files.Count == 0)
            {
                // Nothing to revert; folders a command left empty are not worth a checkpoint either.
                TryDeleteDirectory(turn.Directory);
                return null;
            }

            var changes = new TurnChanges(turn.Id, DateTimeOffset.Now, turn.Label, files.OrderBy(f => f.Path, StringComparer.OrdinalIgnoreCase).ToList());
            Directory.CreateDirectory(turn.Directory);
            File.WriteAllText(Path.Combine(turn.Directory, "meta.json"), JsonSerializer.Serialize(new Meta(turn.Id, changes.Time, turn.Label, meta, changes.Files.ToList(), newDirectories)));
            Prune();
            return changes;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null; // best effort: never fail a turn over bookkeeping
        }
    }

    // ── Undo ─────────────────────────────────────────────────────────────────

    /// <summary>The changes the next <see cref="Undo"/> would revert, or null when there is nothing to undo.</summary>
    public TurnChanges? Preview()
    {
        var meta = LoadLatest();
        return meta is null ? null : new TurnChanges(meta.Id, meta.Time, meta.Label, meta.Files);
    }

    public UndoReport Undo()
    {
        var latest = LatestDirectory();
        var meta = LoadLatest();
        if (latest is null || meta is null) return new UndoReport([], [], []);

        var restored = new List<string>();
        var removed = new List<string>();
        var skipped = meta.Files.Where(f => !f.Restorable).Select(f => $"{f.Path} - changed by a command, old content was not saved").ToList();

        foreach (var entry in meta.Entries)
        {
            var full = FullPath(entry.Path);
            try
            {
                switch (entry.Kind)
                {
                    case FileChangeKind.Created:
                        if (!File.Exists(full)) break;
                        if (entry.PostHash is not null && Hash(full) != entry.PostHash) { skipped.Add($"{entry.Path} - edited since, not removed"); break; }
                        File.Delete(full);
                        RemoveEmptyParents(full);
                        removed.Add(entry.Path);
                        break;

                    case FileChangeKind.Modified:
                        if (entry.PostHash is not null && File.Exists(full) && Hash(full) != entry.PostHash) { skipped.Add($"{entry.Path} - edited since, not restored"); break; }
                        File.Copy(BackupPath(latest, entry.Path), full, overwrite: true);
                        restored.Add(entry.Path);
                        break;

                    case FileChangeKind.Deleted:
                        if (File.Exists(full)) { skipped.Add($"{entry.Path} - exists again, not restored"); break; }
                        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                        File.Copy(BackupPath(latest, entry.Path), full);
                        restored.Add(entry.Path);
                        break;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                skipped.Add($"{entry.Path} - {ex.Message}");
            }
        }

        // Folders created during the turn that are empty now (deepest first).
        foreach (var rel in meta.NewDirectories.OrderByDescending(d => d.Length))
        {
            var dir = FullPath(rel);
            try { if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any()) Directory.Delete(dir); }
            catch (IOException) { }
        }

        TryDeleteDirectory(latest);
        return new UndoReport(restored, removed, skipped);
    }

    // ── Storage ──────────────────────────────────────────────────────────────

    private sealed record MetaEntry(string Path, FileChangeKind Kind, bool Restorable, string? PostHash);

    private sealed record Meta(string Id, DateTimeOffset Time, string Label, List<MetaEntry> Entries, List<FileChange> Files, List<string>? NewDirectories = null)
    {
        public new List<string> NewDirectories { get; } = NewDirectories ?? [];
    }

    private string? LatestDirectory() =>
        Directory.Exists(_undoRoot)
            ? Directory.EnumerateDirectories(_undoRoot).Where(d => File.Exists(Path.Combine(d, "meta.json"))).OrderBy(d => d, StringComparer.Ordinal).LastOrDefault()
            : null;

    private Meta? LoadLatest()
    {
        var dir = LatestDirectory();
        if (dir is null) return null;
        try { return JsonSerializer.Deserialize<Meta>(File.ReadAllText(Path.Combine(dir, "meta.json"))); }
        catch (Exception ex) when (ex is IOException or JsonException) { return null; }
    }

    private void Prune()
    {
        if (!Directory.Exists(_undoRoot)) return;
        var all = Directory.EnumerateDirectories(_undoRoot).OrderBy(d => d, StringComparer.Ordinal).ToList();
        foreach (var old in all.Take(Math.Max(0, all.Count - _keep))) TryDeleteDirectory(old);
    }

    private static string BackupPath(string turnDirectory, string rel) => Path.Combine(turnDirectory, "files", rel.Replace('/', Path.DirectorySeparatorChar));

    private string Relative(string fullPath) => Path.GetRelativePath(_root, fullPath).Replace('\\', '/');

    private string FullPath(string rel) => Path.Combine(_root, rel.Replace('/', Path.DirectorySeparatorChar));

    private void RemoveEmptyParents(string fullPath)
    {
        for (var dir = Path.GetDirectoryName(fullPath);
             dir is not null && dir.Length > _root.Length && Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any();
             dir = Path.GetDirectoryName(dir))
            Directory.Delete(dir);
    }

    private Manifest? TakeManifest()
    {
        var files = new Dictionary<string, (long, long)>(StringComparer.OrdinalIgnoreCase);
        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var file in Plugins.FileSystemPlugin.WalkFiles(_root))
            {
                if (files.Count >= MaxManifestFiles) return null; // too big to compare cheaply
                var info = new FileInfo(file);
                files[Relative(file)] = (info.Length, info.LastWriteTimeUtc.Ticks);
            }
            foreach (var directory in Plugins.FileSystemPlugin.WalkDirectories(_root))
                directories.Add(Relative(directory));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return null; }
        return new Manifest(files, directories);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static bool FilesEqual(string a, string b)
    {
        var (fa, fb) = (new FileInfo(a), new FileInfo(b));
        return fa.Length == fb.Length && File.ReadAllBytes(a).AsSpan().SequenceEqual(File.ReadAllBytes(b));
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch (IOException) { }
    }

    private static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); } catch (IOException) { }
    }

    /// <summary>Lines added/removed between two versions; a missing side counts as empty. Binary or huge files count as 0/0.</summary>
    private static (int Added, int Removed) CountLines(string? before, string? after)
    {
        var a = ReadLines(before);
        var b = ReadLines(after);
        if (a is null || b is null) return (0, 0);
        return LineDiff.Count(a, b);
    }

    private static string[]? ReadLines(string? path)
    {
        if (path is null) return [];
        try
        {
            var info = new FileInfo(path);
            if (info.Length > MaxDiffBytes) return null;
            var bytes = File.ReadAllBytes(path);
            if (bytes.AsSpan(0, Math.Min(bytes.Length, 4096)).Contains((byte)0)) return null;
            var text = Encoding.UTF8.GetString(bytes).Replace("\r\n", "\n", StringComparison.Ordinal);
            return text.Length == 0 ? [] : text.TrimEnd('\n').Split('\n');
        }
        catch (IOException) { return null; }
    }
}

/// <summary>Counts added and removed lines between two line arrays with Myers' O(ND) shortest-edit-script search.</summary>
public static class LineDiff
{
    private const int MaxEditDistance = 4000;

    public static (int Added, int Removed) Count(string[] a, string[] b)
    {
        int n = a.Length, m = b.Length;
        if (n == 0) return (m, 0);
        if (m == 0) return (0, n);

        var max = Math.Min(n + m, MaxEditDistance);
        var v = new int[2 * max + 2];
        var offset = max;
        for (var d = 0; d <= max; d++)
        {
            for (var k = -d; k <= d; k += 2)
            {
                int x;
                if (k == -d || (k != d && v[offset + k - 1] < v[offset + k + 1])) x = v[offset + k + 1];
                else x = v[offset + k - 1] + 1;
                var y = x - k;
                while (x < n && y < m && a[x] == b[y]) { x++; y++; }
                v[offset + k] = x;
                if (x >= n && y >= m)
                {
                    // d = added + removed and LCS = (n + m - d) / 2.
                    var lcs = (n + m - d) / 2;
                    return (m - lcs, n - lcs);
                }
            }
        }
        return (m, n); // too different to bother: report everything as replaced
    }
}
