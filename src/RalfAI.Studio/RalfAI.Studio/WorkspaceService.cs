namespace RalfAI.Studio;

public sealed class WorkspaceService
{
    private static readonly HashSet<string> Hidden = new(StringComparer.OrdinalIgnoreCase) { ".git", "bin", "obj" };
    private string _root;

    public WorkspaceService(string root) => _root = Path.GetFullPath(root);
    public string Root => _root;

    public void SetRoot(string root) => _root = Path.GetFullPath(root);

    public IReadOnlyList<WorkspaceEntry> GetTree() => ReadDirectory(_root, 0);

    public string Read(string relativePath) => File.ReadAllText(Resolve(relativePath));

    public async Task WriteAsync(string relativePath, string content)
    {
        var path = Resolve(relativePath);
        await File.WriteAllTextAsync(path, content).ConfigureAwait(false);
    }

    private List<WorkspaceEntry> ReadDirectory(string directory, int depth)
    {
        if (depth > 8) return [];
        try
        {
            return Directory.EnumerateFileSystemEntries(directory)
                .Where(path => !Hidden.Contains(Path.GetFileName(path)))
                .OrderByDescending(Directory.Exists).ThenBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .Select(path => new WorkspaceEntry(
                    Path.GetFileName(path),
                    Path.GetRelativePath(_root, path).Replace('\\', '/'),
                    Directory.Exists(path),
                    Directory.Exists(path) ? ReadDirectory(path, depth + 1) : []))
                .ToList();
        }
        catch (UnauthorizedAccessException) { return []; }
        catch (IOException) { return []; }
    }

    private string Resolve(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, relativePath));
        var prefix = _root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !string.Equals(fullPath, _root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The path is outside the workspace.");
        return fullPath;
    }
}

public sealed record WorkspaceEntry(string Name, string Path, bool IsDirectory, IReadOnlyList<WorkspaceEntry> Children);
