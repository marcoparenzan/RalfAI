namespace RalfAI;

/// <summary>
/// A snapshot of the files under the target folder, searched by the prompt editor's
/// <c>@</c> mention popup. Skips VCS/build/tooling folders so results stay relevant.
/// </summary>
public sealed class FileIndex
{
    private const int MaxFiles = 5000;

    private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".vs", ".vscode", ".ralfai", "bin", "obj", "node_modules", "packages",
    };

    private readonly List<string> _paths;

    private FileIndex(List<string> paths) => _paths = paths;

    public static FileIndex Empty { get; } = new([]);

    /// <summary>Paths relative to <paramref name="root"/>, with forward slashes.</summary>
    public static FileIndex Build(string root)
    {
        var paths = new List<string>();
        try { Walk(root, root, paths); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        return new FileIndex(paths);
    }

    private static void Walk(string root, string folder, List<string> paths)
    {
        foreach (var file in Directory.EnumerateFiles(folder))
        {
            if (paths.Count >= MaxFiles) return;
            paths.Add(Path.GetRelativePath(root, file).Replace('\\', '/'));
        }

        foreach (var sub in Directory.EnumerateDirectories(folder))
        {
            if (paths.Count >= MaxFiles) return;
            var name = Path.GetFileName(sub);
            if (name.StartsWith('.') || SkippedFolders.Contains(name)) continue;
            try { Walk(root, sub, paths); }
            catch (UnauthorizedAccessException) { }
        }
    }

    /// <summary>
    /// Best matches for <paramref name="query"/>: file-name substring first, then path
    /// substring, then a loose in-order character match. Empty query lists the shallowest files.
    /// </summary>
    public List<string> Search(string query, int max)
    {
        if (query.Length == 0)
            return _paths.OrderBy(p => p.Count(c => c == '/')).ThenBy(p => p, StringComparer.OrdinalIgnoreCase)
                .Take(max).ToList();

        query = query.Replace('\\', '/');
        return _paths
            .Select(p => (Path: p, Score: Score(p, query)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Path.Length)
            .ThenBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .Select(x => x.Path)
            .ToList();
    }

    private static int Score(string path, string query)
    {
        var name = path[(path.LastIndexOf('/') + 1)..];
        if (name.StartsWith(query, StringComparison.OrdinalIgnoreCase)) return 4;
        if (name.Contains(query, StringComparison.OrdinalIgnoreCase)) return 3;
        if (path.Contains(query, StringComparison.OrdinalIgnoreCase)) return 2;

        var i = 0;
        foreach (var c in path)
            if (i < query.Length && char.ToLowerInvariant(c) == char.ToLowerInvariant(query[i])) i++;
        return i == query.Length ? 1 : 0;
    }
}
