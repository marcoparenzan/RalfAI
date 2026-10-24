using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using RalfAI.Contexts;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RalfAI.Profiles;

/// <summary>
/// Contexts are YAML files (<c>name</c>, <c>description</c>, <c>tools</c>, <c>prompt</c>); the registry key is the file name.
/// They are read, in increasing precedence, from:
/// <list type="bullet">
/// <item>the contexts folder: <c>Contexts:Folder</c>, else <c>contexts/</c> next to the external config file
/// (--config / RALFAI_CONFIG_PATH), else <c>~/.ralfai/contexts</c> - never the tool's install folder, which an update replaces;</item>
/// <item><c>&lt;target&gt;/.ralfai/contexts</c>, for contexts that belong to one project.</item>
/// </list>
/// Older Markdown profiles (<c>profiles/*.md</c>, see <see cref="ProfileLoader"/>) are still read, under the YAML ones.
/// The contexts that ship with RalfAI are written to the contexts folder the first time it is used, and from then on they
/// are ordinary files: edit them, delete them (they do not come back) or add new ones.
/// </summary>
public static class ContextStore
{
    public const string FolderName = "contexts";
    private const string SeededListFile = ".defaults";
    private const string ResourcePrefix = "RalfAI.ContextDefaults.";

    private static readonly Regex ValidKey = new("^[a-z0-9][a-z0-9_.-]{0,47}$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly IDeserializer Reader = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    /// <summary>Where the user's contexts live.</summary>
    public static string Folder { get; private set; } = DefaultFolder(null, null);

    public static void Configure(IConfiguration? configuration, string? externalConfigPath)
    {
        Folder = DefaultFolder(configuration?["Contexts:Folder"], externalConfigPath);
    }

    /// <summary>Used by tests to point the store at a temporary folder.</summary>
    internal static void UseFolder(string folder) => Folder = Path.GetFullPath(folder);

    internal static string DefaultFolder(string? setting, string? externalConfigPath)
    {
        if (!string.IsNullOrWhiteSpace(setting)) return Path.GetFullPath(Environment.ExpandEnvironmentVariables(setting));
        if (!string.IsNullOrWhiteSpace(externalConfigPath))
            return Path.Combine(Path.GetDirectoryName(Path.GetFullPath(externalConfigPath))!, FolderName);
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ralfai", FolderName);
    }

    public static string ProjectFolder(string targetFolder) => Path.Combine(targetFolder, ".ralfai", FolderName);

    public static bool IsValidKey(string key) => ValidKey.IsMatch(key);

    // ── Loading ──────────────────────────────────────────────────────────────

    /// <summary>A context loaded from a file, with where it came from.</summary>
    public sealed record Entry(string Key, ProfileContext Context, string Path, bool InProject);

    /// <summary>All file contexts keyed by name; later sources override earlier ones.</summary>
    public static Dictionary<string, Entry> LoadAll(string? targetFolder, Action<string>? onError = null)
    {
        Seed(onError);
        var result = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        void Add(string folder, bool inProject)
        {
            if (!Directory.Exists(folder)) return;
            var files = Directory.EnumerateFiles(folder, "*.yaml").Concat(Directory.EnumerateFiles(folder, "*.yml"));
            foreach (var file in files.Order(StringComparer.OrdinalIgnoreCase))
            {
                var key = Path.GetFileNameWithoutExtension(file);
                try
                {
                    result[key] = new Entry(key, Parse(File.ReadAllText(file), key), file, inProject);
                }
                catch (Exception ex) when (ex is IOException or FormatException or ArgumentException)
                {
                    // A broken file must not take the app down - skip it and say why.
                    onError?.Invoke($"{file}: {ex.Message}");
                }
            }
        }

        void AddLegacy(string folder, bool inProject)
        {
            foreach (var (key, (context, path)) in ProfileLoader.LoadFolder(folder, onError))
                result[key] = new Entry(key, context, path, inProject);
        }

        AddLegacy(ProfileLoader.UserFolder, inProject: false);
        Add(Folder, inProject: false);
        if (targetFolder is not null)
        {
            AddLegacy(ProfileLoader.ProjectFolder(targetFolder), inProject: true);
            Add(ProjectFolder(targetFolder), inProject: true);
        }
        return result;
    }

    private sealed class ContextFile
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<string>? Tools { get; set; }
        public string? Prompt { get; set; }
    }

    public static ProfileContext Parse(string yaml, string fallbackName)
    {
        ContextFile? file;
        try
        {
            file = Reader.Deserialize<ContextFile>(yaml);
        }
        catch (YamlException ex)
        {
            // "(Line: 3, Col: 1, …): Property 'promt' not found on type …" - keep the position, drop the .NET type name.
            var message = (ex.InnerException?.Message ?? ex.Message).Replace($" on type '{typeof(ContextFile).FullName}'", "");
            throw new FormatException($"line {ex.Start.Line}: {message}", ex);
        }

        if (file is null) throw new FormatException("the file is empty.");
        var prompt = file.Prompt?.Trim() ?? "";
        if (prompt.Length == 0) throw new FormatException("'prompt' is missing or empty.");
        var tools = file.Tools?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToList() ?? [];
        if (tools.Count == 0) throw new FormatException("'tools' is missing or empty.");

        // Fail at load time, not mid-session, on a typo in the tool list.
        ToolCatalog.Resolve(tools, Path.GetTempPath()).ToList();

        return new ProfileContext(string.IsNullOrWhiteSpace(file.Name) ? fallbackName : file.Name.Trim(), file.Description?.Trim() ?? "", tools, prompt);
    }

    // ── Writing ──────────────────────────────────────────────────────────────

    /// <summary>The context as a YAML file. Tool groups that are used whole are written by group name.</summary>
    public static string ToYaml(IAgentContext context)
    {
        // The placeholder survives interpolation, so the target folder stays a placeholder in the file.
        var prompt = context.BuildSystemPrompt(ProfileContext.TargetFolderPlaceholder).ReplaceLineEndings("\n").Trim();
        var tools = ToolCatalog.Compact(context.GetTools(Path.GetTempPath()).Select(t => t.Name));

        var lines = new List<string>
        {
            $"name: {Quote(context.Name)}",
            $"description: {Quote(context.Description)}",
            $"tools: [{string.Join(", ", tools)}]",
            "# {{targetFolder}} in the prompt becomes the absolute path of the project folder.",
            "prompt: |",
        };
        lines.AddRange(prompt.Split('\n').Select(l => l.Length == 0 ? "" : "  " + l.TrimEnd()));
        return string.Join("\n", lines) + "\n";
    }

    private static readonly JsonSerializerOptions QuoteOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    // A YAML double-quoted scalar is a superset of a JSON string.
    private static string Quote(string value) => JsonSerializer.Serialize(value, QuoteOptions);

    /// <summary>Checks <paramref name="yaml"/> and writes it as <c>&lt;key&gt;.yaml</c>; returns the file written.</summary>
    public static string Save(string key, string yaml, string? projectFolder = null)
    {
        CheckKey(key);
        Parse(yaml, key);                                                    // throws FormatException/ArgumentException with the reason
        var folder = projectFolder is null ? Folder : ProjectFolder(projectFolder);
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, key + ".yaml");
        File.WriteAllText(path, yaml.ReplaceLineEndings("\n"));
        var legacyYml = Path.Combine(folder, key + ".yml");
        if (File.Exists(legacyYml)) File.Delete(legacyYml);                 // otherwise two files would claim the same key
        return path;
    }

    /// <summary>Deletes the context file; returns the file deleted, or null if there was none.</summary>
    public static string? Delete(string key, string? projectFolder = null)
    {
        CheckKey(key);
        var folder = projectFolder is null ? Folder : ProjectFolder(projectFolder);
        foreach (var path in new[] { Path.Combine(folder, key + ".yaml"), Path.Combine(folder, key + ".yml") })
        {
            if (!File.Exists(path)) continue;
            File.Delete(path);
            return path;
        }
        return null;
    }

    private static void CheckKey(string key)
    {
        if (!IsValidKey(key))
            throw new ArgumentException($"'{key}' is not a valid context name: use letters, digits, '-', '_' or '.', up to 48 characters, no spaces.");
        if (string.Equals(key, ContextRegistry.IntegratedName, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"'{key}' is the integrated context and cannot be replaced; choose another name.");
    }

    // ── Shipped contexts ─────────────────────────────────────────────────────

    /// <summary>The contexts that ship with RalfAI, as (key, YAML).</summary>
    public static IEnumerable<(string Key, string Yaml)> Defaults()
    {
        var assembly = typeof(ContextStore).Assembly;
        foreach (var resource in assembly.GetManifestResourceNames().Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal)).Order())
        {
            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var reader = new StreamReader(stream);
            yield return (resource[ResourcePrefix.Length..^".yaml".Length], reader.ReadToEnd());
        }
    }

    /// <summary>
    /// Writes each shipped context the contexts folder has never had. A list of the ones already written is kept, so a context
    /// the user deleted stays deleted, and a context added by a later version still arrives. Returns the keys written.
    /// </summary>
    public static IReadOnlyList<string> Seed(Action<string>? onError = null)
    {
        var written = new List<string>();
        try
        {
            var listPath = Path.Combine(Folder, SeededListFile);
            var seeded = File.Exists(listPath)
                ? File.ReadAllLines(listPath).Select(l => l.Trim()).Where(l => l.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var pending = Defaults().Where(d => !seeded.Contains(d.Key)).ToList();
            if (pending.Count == 0) return written;

            Directory.CreateDirectory(Folder);
            foreach (var (key, yaml) in pending)
            {
                var path = Path.Combine(Folder, key + ".yaml");
                if (!File.Exists(path) && !File.Exists(Path.Combine(Folder, key + ".yml")))
                {
                    File.WriteAllText(path, yaml);
                    written.Add(key);
                }
                seeded.Add(key);
            }
            File.WriteAllLines(listPath,
                ["# Contexts RalfAI has already written here. Delete a line to get that context back at the next start."
                    , .. seeded.Where(k => !k.StartsWith('#')).Order(StringComparer.OrdinalIgnoreCase)]);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            onError?.Invoke($"{Folder}: cannot write the shipped contexts: {ex.Message}");
        }
        return written;
    }
}
