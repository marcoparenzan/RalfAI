namespace RalfAI;

/// <summary>One entry in a directory listing — a file, a subfolder, or the synthetic ".." parent.</summary>
public readonly record struct DirectoryEntry(string Name, bool IsDirectory, long Size);

/// <summary>
/// Directory listing and formatting shared by every console file-browsing UI
/// (<see cref="FileBrowser"/>) so they list/sort/format
/// identically instead of drifting apart.
/// </summary>
public static class DirectoryListing
{
    /// <summary>Lists a directory: ".." first (if it has a parent), then subfolders, then files — each group sorted by name.</summary>
    public static List<DirectoryEntry> Load(string path)
    {
        var entries = new List<DirectoryEntry>();
        try
        {
            var di = new DirectoryInfo(path);
            if (di.Parent is not null)
                entries.Add(new DirectoryEntry("..", true, 0));

            foreach (var d in di.GetDirectories().OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
                entries.Add(new DirectoryEntry(d.Name, true, 0));

            foreach (var f in di.GetFiles().OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
                entries.Add(new DirectoryEntry(f.Name, false, f.Length));
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            // Leave entries as loaded so far (possibly just ".."); the panel just looks empty.
        }
        return entries;
    }

    public static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return unit == 0 ? $"{bytes} B" : $"{size:0.#} {units[unit]}";
    }

    public static string Truncate(string text, int maxLength)
        => text.Length <= maxLength ? text : "…" + text[^(maxLength - 1)..];
}
