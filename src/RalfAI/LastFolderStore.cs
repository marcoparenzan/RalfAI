namespace RalfAI;

/// <summary>
/// Persists the last-used target folder next to the RalfAI executable (not inside any
/// target folder), so a launch with no CLI argument can default the folder picker there
/// instead of the current working directory.
/// </summary>
public static class LastFolderStore
{
    private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "last-folder.txt");

    public static string? Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            var path = File.ReadAllText(FilePath).Trim();
            return !string.IsNullOrWhiteSpace(path) && Directory.Exists(path) ? path : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public static void Save(string folder)
    {
        try { File.WriteAllText(FilePath, folder); }
        catch (IOException) { /* best-effort — not worth failing the app over */ }
    }
}
