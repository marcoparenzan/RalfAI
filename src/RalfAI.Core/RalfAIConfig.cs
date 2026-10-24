using Microsoft.Extensions.Configuration;

namespace RalfAI;

/// <summary>
/// Shared configuration loading, used by every host (console, Desktop, …). Resolves which
/// JSON file to load in this order:
///
///   1. <paramref name="explicitConfigPath"/> (e.g. from a --config CLI flag) — wins outright.
///   2. RALFAI_CONFIG_PATH environment variable — an absolute path to a config file (or to the
///      folder holding config.json) living outside the tool's install directory, so it survives
///      `dotnet tool update` (which overwrites the package's own bundled appsettings.json on every
///      reinstall). The same folder holds the contexts (see ContextStore).
///   3. appsettings.json next to the entry assembly — the zero-config default.
///
/// Whichever file loads, individual settings can still be overridden with RALFAI_-prefixed
/// environment variables (e.g. RALFAI_AzureAIFoundry__ApiKey).
/// </summary>
public static class RalfAIConfig
{
    /// <summary>The config file names looked for when --config / RALFAI_CONFIG_PATH names a folder.</summary>
    public static readonly string[] FileNamesInFolder = ["config.json", "appsettings.json"];

    /// <summary>
    /// The external config file in use (--config or RALFAI_CONFIG_PATH), or null for the bundled appsettings.json.
    /// Either may name the file or the folder that holds it (then <c>config.json</c>, else <c>appsettings.json</c>).
    /// </summary>
    public static string? ExternalPath(string? explicitConfigPath = null)
    {
        var configPath = explicitConfigPath ?? Environment.GetEnvironmentVariable("RALFAI_CONFIG_PATH");
        if (string.IsNullOrWhiteSpace(configPath)) return null;

        var full = Path.GetFullPath(Environment.ExpandEnvironmentVariables(configPath.Trim().Trim('"')));
        if (!Directory.Exists(full)) return full;
        return FileNamesInFolder.Select(name => Path.Combine(full, name)).FirstOrDefault(File.Exists)
               ?? Path.Combine(full, FileNamesInFolder[0]);       // missing: the error names the file expected
    }

    public static IConfiguration Load(string? explicitConfigPath = null)
    {
        var configPath = ExternalPath(explicitConfigPath);

        var builder = new ConfigurationBuilder();
        if (!string.IsNullOrWhiteSpace(configPath))
        {
            builder.AddJsonFile(Path.GetFullPath(configPath), optional: false, reloadOnChange: false);
        }
        else
        {
            builder
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        }

        return builder.AddEnvironmentVariables("RALFAI_").Build();
    }
}
