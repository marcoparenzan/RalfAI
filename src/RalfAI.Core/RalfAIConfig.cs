using Microsoft.Extensions.Configuration;

namespace RalfAI;

/// <summary>
/// Shared configuration loading, used by every host (console, Desktop, …). Resolves which
/// JSON file to load in this order:
///
///   1. <paramref name="explicitConfigPath"/> (e.g. from a --config CLI flag) — wins outright.
///   2. RALFAI_CONFIG_PATH environment variable — an absolute path to a config file living
///      outside the tool's install directory, so it survives `dotnet tool update` (which
///      overwrites the package's own bundled appsettings.json on every reinstall).
///   3. appsettings.json next to the entry assembly — the zero-config default.
///
/// Whichever file loads, individual settings can still be overridden with RALFAI_-prefixed
/// environment variables (e.g. RALFAI_AzureAIFoundry__ApiKey).
/// </summary>
public static class RalfAIConfig
{
    public static IConfiguration Load(string? explicitConfigPath = null)
    {
        var configPath = explicitConfigPath ?? Environment.GetEnvironmentVariable("RALFAI_CONFIG_PATH");

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
