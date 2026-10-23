namespace RalfAI.Plugins;

/// <summary>Resolves a path the model gave relative to a project root and refuses anything outside it.</summary>
internal static class SafePath
{
    public static string Resolve(string rootFolder, string relativePath)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootFolder));
        var full = Path.GetFullPath(Path.Combine(root, relativePath));
        // "D:\proj" must not admit "D:\proj-other": compare against the root plus a separator.
        if (!full.Equals(root, StringComparison.OrdinalIgnoreCase) &&
            !full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Path traversal blocked: {relativePath}");
        return full;
    }
}
