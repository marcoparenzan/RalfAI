namespace RalfAI;

/// <summary>
/// Persists RalfAI's per-folder configuration — the selected generation context and the
/// debug-messages toggle — in &lt;target&gt;/.ralfai/config.yaml, alongside the session log.
/// Lets re-launching against the same folder (without explicit CLI flags) resume with the same
/// settings automatically, with different target folders each remembering their own.
/// </summary>
public static class FolderConfigStore
{
    private const string FolderName = ".ralfai";
    private const string FileName = "config.yaml";

    private static string PathFor(string targetFolder) => Path.Combine(targetFolder, FolderName, FileName);

    public static string? LoadContext(string targetFolder) => ReadKey(targetFolder, "context");

    public static void SaveContext(string targetFolder, string contextKey) => WriteKey(targetFolder, "context", contextKey);

    public static bool? LoadDebug(string targetFolder) => ReadKey(targetFolder, "debug") switch
    {
        "true" => true,
        "false" => false,
        _ => null
    };

    public static void SaveDebug(string targetFolder, bool debug) => WriteKey(targetFolder, "debug", debug ? "true" : "false");

    private static string? ReadKey(string targetFolder, string key)
    {
        try
        {
            var path = PathFor(targetFolder);
            if (!File.Exists(path)) return null;

            var prefix = key + ":";
            foreach (var line in File.ReadAllLines(path))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;

                var value = trimmed[prefix.Length..].Trim().Trim('"', '\'');
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static void WriteKey(string targetFolder, string key, string value)
    {
        try
        {
            var folder = Path.Combine(targetFolder, FolderName);
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, FileName);

            var lines = File.Exists(path) ? File.ReadAllLines(path).ToList() : [];

            var prefix = key + ":";
            var index = lines.FindIndex(l => l.Trim().StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            var newLine = $"{key}: {value}";

            if (index >= 0) lines[index] = newLine;
            else lines.Add(newLine);

            File.WriteAllLines(path, lines);
        }
        catch (IOException)
        {
            // best-effort — not worth failing the app over
        }
    }
}
