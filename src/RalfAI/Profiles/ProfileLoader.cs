using RalfAI.Contexts;

namespace RalfAI.Profiles;

/// <summary>
/// Reads the older Markdown profile files (<c>*.md</c> with frontmatter) from <c>~/.ralfai/profiles</c> and
/// <c>&lt;target&gt;/.ralfai/profiles</c>. Contexts are now YAML files (<see cref="ContextStore"/>); these are still
/// loaded so existing profiles keep working. The registry key is the file name without extension.
/// </summary>
public static class ProfileLoader
{
    public const string FolderName = "profiles";

    public static string UserFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ralfai", FolderName);

    public static string ProjectFolder(string targetFolder) =>
        Path.Combine(targetFolder, ".ralfai", FolderName);

    /// <summary>The profiles of one folder, keyed by file name.</summary>
    public static Dictionary<string, (ProfileContext Context, string Path)> LoadFolder(string folder, Action<string>? onError = null)
    {
        var result = new Dictionary<string, (ProfileContext, string)>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(folder)) return result;

        foreach (var file in Directory.EnumerateFiles(folder, "*.md").Order(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                result[Path.GetFileNameWithoutExtension(file)] = (Parse(File.ReadAllText(file), Path.GetFileNameWithoutExtension(file)), file);
            }
            catch (Exception ex) when (ex is IOException or FormatException or ArgumentException)
            {
                // A broken profile must not take the app down — skip it and say why.
                onError?.Invoke($"{file}: {ex.Message}");
            }
        }
        return result;
    }

    public static ProfileContext Parse(string text, string fallbackName)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        var name = fallbackName;
        var description = "";
        var tools = new List<string>();
        var bodyStart = 0;

        if (lines.Length > 0 && lines[0].Trim() == "---")
        {
            var end = Array.FindIndex(lines, 1, l => l.Trim() == "---");
            if (end < 0) throw new FormatException("frontmatter not closed with '---'.");
            bodyStart = end + 1;

            for (var i = 1; i < end; i++)
            {
                var line = lines[i];
                var colon = line.IndexOf(':');
                if (colon <= 0 || line.TrimStart().StartsWith('#')) continue;

                var key = line[..colon].Trim().ToLowerInvariant();
                var value = StripComment(line[(colon + 1)..]).Trim();
                switch (key)
                {
                    case "name": name = Unquote(value); break;
                    case "description": description = Unquote(value); break;
                    case "tools":
                        tools.AddRange(value.Trim('[', ']')
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(Unquote));
                        break;
                }
            }
        }

        var body = string.Join('\n', lines.Skip(bodyStart)).Trim();
        if (body.Length == 0) throw new FormatException("empty prompt body.");
        if (tools.Count == 0) throw new FormatException("'tools' is missing or empty.");

        // Fail at load time, not mid-session, on a typo in the tool list.
        ToolCatalog.Resolve(tools, Path.GetTempPath()).ToList();

        return new ProfileContext(name, description, tools, body);
    }

    private static string StripComment(string value)
    {
        var hash = value.IndexOf(" #", StringComparison.Ordinal);
        return hash >= 0 ? value[..hash] : value;
    }

    private static string Unquote(string value) => value.Trim().Trim('"', '\'');
}
