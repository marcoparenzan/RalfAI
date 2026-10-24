using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RalfAI.Profiles;

/// <summary>One context a source offers: <c>&lt;name&gt;.yaml</c> next to the index.</summary>
public sealed class ContextOffer
{
    public string Name { get; set; } = "";
    public int Version { get; set; }
    public string? Updated { get; set; }
    /// <summary>SHA-256 of this version's text (LF line endings, trailing whitespace trimmed); see <see cref="ContextSync.Hash"/>.</summary>
    public string? Sha256 { get; set; }
    /// <summary>SHA-256 of earlier published versions: a file still equal to one of them was not changed by the user.</summary>
    public List<string> Previous { get; set; } = [];
}

/// <summary>The <c>index.yaml</c> of a context source.</summary>
public sealed class ContextIndex
{
    public string? Updated { get; set; }
    public List<ContextOffer> Contexts { get; set; } = [];

    private static readonly IDeserializer Reader = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()                       // a newer index may carry fields this version does not know
        .Build();

    public static ContextIndex Parse(string yaml)
    {
        try { return Reader.Deserialize<ContextIndex>(yaml) ?? new ContextIndex(); }
        catch (YamlDotNet.Core.YamlException ex) { throw new FormatException($"index.yaml line {ex.Start.Line}: {ex.Message}", ex); }
    }
}

/// <summary>Where contexts are offered from: the copy built into RalfAI, a folder, or a web address.</summary>
public interface IContextSource
{
    string Description { get; }
    Task<string> ReadAsync(string fileName, CancellationToken cancellationToken);
}

public sealed class EmbeddedContextSource : IContextSource
{
    private const string ResourcePrefix = "RalfAI.ContextDefaults.";
    public string Description => "RalfAI";

    public Task<string> ReadAsync(string fileName, CancellationToken cancellationToken)
    {
        var assembly = typeof(EmbeddedContextSource).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourcePrefix + fileName)
                           ?? throw new FileNotFoundException($"{fileName} is not built into RalfAI.");
        using var reader = new StreamReader(stream);
        return Task.FromResult(reader.ReadToEnd());
    }
}

public sealed class FolderContextSource(string folder) : IContextSource
{
    public string Description => folder;
    public Task<string> ReadAsync(string fileName, CancellationToken cancellationToken)
        => File.ReadAllTextAsync(Path.Combine(folder, fileName), cancellationToken);
}

public sealed class HttpContextSource(Uri baseUri, HttpClient? client = null) : IContextSource
{
    private const int MaxBytes = 512 * 1024;
    private static readonly HttpClient Shared = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly HttpClient _client = client ?? Shared;

    public string Description => baseUri.ToString();

    public async Task<string> ReadAsync(string fileName, CancellationToken cancellationToken)
    {
        using var response = await _client.GetAsync(new Uri(baseUri, fileName), HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"{fileName}: HTTP {(int)response.StatusCode}");
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length > MaxBytes) throw new InvalidDataException($"{fileName} is larger than {MaxBytes / 1024} KB.");
        return Encoding.UTF8.GetString(bytes);
    }
}

/// <summary>What a sync did.</summary>
public sealed record ContextSyncReport(IReadOnlyList<string> Added, IReadOnlyList<string> Updated, IReadOnlyList<string> KeptChanged, IReadOnlyList<string> Rejected)
{
    public bool Any => Added.Count + Updated.Count + KeptChanged.Count + Rejected.Count > 0;
}

/// <summary>
/// Brings the contexts a source offers into the contexts folder, without ever overwriting the user's work:
/// <list type="bullet">
/// <item>a context the folder never had is added;</item>
/// <item>a newer version replaces the file only if the file is still exactly what RalfAI wrote (or an earlier published version);</item>
/// <item>a file the user changed is kept, and reported once per new version;</item>
/// <item>a context the user deleted stays deleted.</item>
/// </list>
/// What was written is kept in <c>.state.json</c> in the contexts folder.
/// </summary>
public static class ContextSync
{
    public const string DefaultSource = "https://github.com/marcoparenzan/RalfAI/config/contexts";
    public const string StateFile = ".state.json";
    private const string LegacySeededList = ".defaults";

    private sealed class State
    {
        public DateTimeOffset? LastCheck { get; set; }
        public string? Source { get; set; }
        public Dictionary<string, Written> Contexts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class Written
    {
        public int Version { get; set; }
        /// <summary>Hash of the text RalfAI wrote; null when the file was the user's from the start.</summary>
        public string? Sha256 { get; set; }
        /// <summary>The newest version already reported as not applied because the file was changed.</summary>
        public int Offered { get; set; }
    }

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Hash(string text)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text.ReplaceLineEndings("\n").TrimEnd())));

    // ── Sync ─────────────────────────────────────────────────────────────────

    public static async Task<ContextSyncReport> SyncAsync(IContextSource source, string folder, bool markChecked, CancellationToken cancellationToken = default)
    {
        var index = ContextIndex.Parse(await source.ReadAsync("index.yaml", cancellationToken));
        var state = LoadState(folder, index);
        List<string> added = [], updated = [], kept = [], rejected = [];

        foreach (var offer in index.Contexts)
        {
            var name = offer.Name.Trim();
            if (!ContextStore.IsValidKey(name) || string.Equals(name, ContextRegistry.IntegratedName, StringComparison.OrdinalIgnoreCase))
            {
                rejected.Add($"{name}: not a valid context name");          // never a path: an index cannot write outside the folder
                continue;
            }

            var path = Path.Combine(folder, name + ".yaml");
            var exists = File.Exists(path) || File.Exists(Path.Combine(folder, name + ".yml"));

            if (!state.Contexts.TryGetValue(name, out var written))
            {
                if (exists)
                {
                    state.Contexts[name] = new Written { Version = offer.Version };      // the user's own file with that name: never touched
                    continue;
                }
                if (await TryWriteAsync(source, name, path, offer, rejected, cancellationToken) is { } hash)
                {
                    state.Contexts[name] = new Written { Version = offer.Version, Sha256 = hash };
                    added.Add(name);
                }
                continue;
            }

            if (offer.Version <= written.Version || !exists) continue;               // up to date, or deleted by the user

            var current = File.Exists(path) ? Hash(await File.ReadAllTextAsync(path, cancellationToken)) : null;
            var untouched = current is not null && written.Sha256 is not null
                            && (current == written.Sha256 || offer.Previous.Contains(current, StringComparer.OrdinalIgnoreCase));
            if (untouched)
            {
                if (await TryWriteAsync(source, name, path, offer, rejected, cancellationToken) is { } hash)
                {
                    written.Version = offer.Version;
                    written.Sha256 = hash;
                    updated.Add(name);
                }
            }
            else if (markChecked && written.Offered < offer.Version)          // only a check the user sees reports it
            {
                written.Offered = offer.Version;
                kept.Add(name);
            }
        }

        if (markChecked)
        {
            state.LastCheck = DateTimeOffset.Now;
            state.Source = source.Description;
        }
        SaveState(folder, state);
        return new ContextSyncReport(added, updated, kept, rejected);
    }

    private static async Task<string?> TryWriteAsync(IContextSource source, string name, string path, ContextOffer offer, List<string> rejected, CancellationToken cancellationToken)
    {
        string yaml;
        try
        {
            yaml = (await source.ReadAsync(name + ".yaml", cancellationToken)).ReplaceLineEndings("\n");
            ContextStore.Parse(yaml, name);                                  // a broken or unknown-tool file is not written
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or IOException or HttpRequestException or InvalidDataException)
        {
            rejected.Add($"{name} v{offer.Version}: {ex.Message}");
            return null;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, yaml, cancellationToken);
        return Hash(yaml);
    }

    /// <summary>Adds what is built into RalfAI. Offline and quick: run whenever the contexts are read.</summary>
    public static ContextSyncReport SeedBuiltIn(string folder)
        => SyncAsync(new EmbeddedContextSource(), folder, markChecked: false).GetAwaiter().GetResult();

    // ── Checking the source at start ─────────────────────────────────────────

    /// <summary>
    /// Whether to look at the source now. <c>Contexts:Check</c> true (the default) asks for a check at the next start and is then
    /// set to false in the config file; <c>Contexts:CheckEveryDays</c> &gt; 0 also checks when the last check is older than that.
    /// </summary>
    public static bool IsDue(IConfiguration? configuration, string folder, bool sourceGivenOnCommandLine, DateTimeOffset now)
    {
        if (sourceGivenOnCommandLine) return true;
        var check = configuration?["Contexts:Check"];
        var lastCheck = ReadLastCheck(folder);
        if (bool.TryParse(check, out var on) && on) return true;
        if (check is null && lastCheck is null) return true;                     // not set: once, even if the config cannot be written
        return int.TryParse(configuration?["Contexts:CheckEveryDays"], out var days) && days > 0
               && (lastCheck is null || now - lastCheck.Value >= TimeSpan.FromDays(days));
    }

    /// <summary>The source to read: a folder path, or a web address (github.com page addresses are turned into raw file addresses).</summary>
    public static IContextSource CreateSource(string? setting)
    {
        var value = string.IsNullOrWhiteSpace(setting) ? DefaultSource : setting.Trim();
        if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return new FolderContextSource(Path.GetFullPath(Environment.ExpandEnvironmentVariables(value)));
        return new HttpContextSource(RawGitHubUri(value));
    }

    /// <summary>
    /// github.com/o/r/tree/branch/path and github.com/o/r/blob/branch/path become raw.githubusercontent.com/o/r/branch/path/;
    /// github.com/o/r/path (no branch) reads the main branch. Other addresses are used as they are.
    /// </summary>
    public static Uri RawGitHubUri(string address)
    {
        var uri = new Uri(address.TrimEnd('/') + "/");
        if (!uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) && !uri.Host.Equals("www.github.com", StringComparison.OrdinalIgnoreCase))
            return uri;

        var parts = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return uri;
        var rest = parts.Skip(2).ToList();
        var branchAndPath = rest.Count >= 2 && rest[0] is "tree" or "blob" ? rest.Skip(1) : rest.Prepend("main");
        return new Uri($"https://raw.githubusercontent.com/{parts[0]}/{parts[1]}/{string.Join('/', branchAndPath)}/");
    }

    /// <summary>
    /// Sets <c>Contexts:Check</c> to false in the JSON config file, keeping the other settings. Refuses a file with comments
    /// (they would be lost) and returns why it did not write.
    /// </summary>
    public static string? TryTurnOffCheck(string? configPath)
    {
        if (configPath is null || !File.Exists(configPath)) return "no config file of your own to write to";
        try
        {
            var text = File.ReadAllText(configPath);
            JsonNode? root;
            try
            {
                root = JsonNode.Parse(text);
            }
            catch (JsonException)
            {
                return "the config file has comments or trailing commas, which rewriting it would lose";
            }
            if (root is not JsonObject obj) return "the config file is not a JSON object";

            var section = obj["Contexts"] as JsonObject;
            if (section is null) obj["Contexts"] = section = new JsonObject();
            section["Check"] = false;

            var temp = configPath + ".tmp";
            File.WriteAllText(temp, obj.ToJsonString(Json) + Environment.NewLine, new UTF8Encoding(false));
            File.Move(temp, configPath, overwrite: true);
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return ex.Message;
        }
    }

    // ── State ────────────────────────────────────────────────────────────────

    private static DateTimeOffset? ReadLastCheck(string folder) => ReadState(folder)?.LastCheck;

    private static State? ReadState(string folder)
    {
        var path = Path.Combine(folder, StateFile);
        if (!File.Exists(path)) return null;
        try { return JsonSerializer.Deserialize<State>(File.ReadAllText(path), Json); }
        catch (JsonException) { return null; }
    }

    private static State LoadState(string folder, ContextIndex index)
    {
        if (ReadState(folder) is { } state)
        {
            state.Contexts = new Dictionary<string, Written>(state.Contexts, StringComparer.OrdinalIgnoreCase);
            return state;
        }

        state = new State();
        // Version 1.15 kept only the names it had written. A file still equal to a published version is RalfAI's; anything else
        // is treated as changed by the user.
        var legacy = Path.Combine(folder, LegacySeededList);
        if (!File.Exists(legacy)) return state;
        foreach (var name in File.ReadAllLines(legacy).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith('#')))
        {
            var path = Path.Combine(folder, name + ".yaml");
            var offer = index.Contexts.FirstOrDefault(o => string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase));
            string? hash = File.Exists(path) ? Hash(File.ReadAllText(path)) : null;
            var isCurrent = hash is not null && string.Equals(hash, offer?.Sha256, StringComparison.OrdinalIgnoreCase);
            var isEarlier = hash is not null && offer is not null && offer.Previous.Contains(hash, StringComparer.OrdinalIgnoreCase);
            state.Contexts[name] = isCurrent ? new Written { Version = offer!.Version, Sha256 = hash }
                                 : isEarlier ? new Written { Version = 0, Sha256 = hash }            // an older version: the next sync updates it
                                 : new Written();                  // changed by the user: kept, and the next version is reported once
        }
        return state;
    }

    private static void SaveState(string folder, State state)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, StateFile), JsonSerializer.Serialize(state, Json));
        var legacy = Path.Combine(folder, LegacySeededList);
        if (File.Exists(legacy)) File.Delete(legacy);
    }
}
