using System.ComponentModel;

namespace RalfAI.Plugins;

/// <summary>
/// Gives the agent read/write access to the target project folder.
/// All paths are resolved relative to the root and protected against traversal.
/// </summary>
public sealed class FileSystemPlugin(string rootFolder)
{
    [Description("Create or overwrite a file in the target project folder. Parent directories are created automatically.")]
    public Task<string> WriteFile(
        [Description("Relative path from project root, e.g. 'src/MyApp/Program.cs'")] string relativePath,
        [Description("Complete file content to write")] string content)
    {
        var fullPath = ResolveSafe(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content, System.Text.Encoding.UTF8);
        return Task.FromResult($"OK — written {relativePath} ({content.Length} chars)");
    }

    [Description("Read the full content of a file in the target project folder.")]
    public Task<string> ReadFile(
        [Description("Relative path from project root")] string relativePath)
    {
        var fullPath = ResolveSafe(relativePath);
        if (!File.Exists(fullPath))
            return Task.FromResult($"ERROR: file not found — {relativePath}");
        return Task.FromResult(File.ReadAllText(fullPath));
    }

    [Description("List all files and directories inside the target project folder (recursive).")]
    public Task<string> ListFiles(
        [Description("Relative subdirectory to list, or empty string for the project root")] string subdirectory = "")
    {
        var dir = string.IsNullOrWhiteSpace(subdirectory)
            ? rootFolder
            : ResolveSafe(subdirectory);

        if (!Directory.Exists(dir))
            return Task.FromResult($"ERROR: directory not found — {subdirectory}");

        var sb = new System.Text.StringBuilder();
        foreach (var entry in Directory.EnumerateFileSystemEntries(dir, "*", SearchOption.AllDirectories)
                                       .OrderBy(e => e))
        {
            var rel = Path.GetRelativePath(rootFolder, entry);
            sb.AppendLine(Directory.Exists(entry) ? $"[DIR]  {rel}" : $"[FILE] {rel}");
        }

        return Task.FromResult(sb.Length == 0 ? "(empty folder)" : sb.ToString().TrimEnd());
    }

    [Description("Delete a file from the target project folder.")]
    public Task<string> DeleteFile(
        [Description("Relative path from project root")] string relativePath)
    {
        var fullPath = ResolveSafe(relativePath);
        if (!File.Exists(fullPath))
            return Task.FromResult($"File not found — nothing deleted: {relativePath}");
        File.Delete(fullPath);
        return Task.FromResult($"OK — deleted {relativePath}");
    }

    private string ResolveSafe(string relativePath)
    {
        var full = Path.GetFullPath(Path.Combine(rootFolder, relativePath));
        if (!full.StartsWith(rootFolder, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Path traversal blocked: {relativePath}");
        return full;
    }
}
