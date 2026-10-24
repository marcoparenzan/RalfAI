using RalfAI;
using RalfAI.Contexts;

if (args.Contains("--version", StringComparer.Ordinal) || args.Contains("-v", StringComparer.Ordinal))
{
    Console.WriteLine($"RalfAI {AppVersion.Current}");
    return 0;
}

// --config wins over the RALFAI_CONFIG_PATH environment variable (see RalfAIConfig.Load).
var configFlagIndex = Array.IndexOf(args, "--config");
var configFlagValue = configFlagIndex >= 0 && configFlagIndex + 1 < args.Length
    ? args[configFlagIndex + 1]
    : null;

// Contexts are YAML files in a folder next to the config file (or Contexts:Folder); see ContextStore.
ContextRegistry.ProfileError = msg => Console.Error.WriteLine($"ralf: skipped context {msg}");
void ConfigureContexts(Microsoft.Extensions.Configuration.IConfiguration? config)
    => RalfAI.Profiles.ContextStore.Configure(config, RalfAIConfig.ExternalPath(configFlagValue));

// --list-contexts and --export-context print the files' text: UTF-8 when redirected, so "—" and "…" survive `> file.yaml`.
void ConfigureForPrinting(Microsoft.Extensions.Configuration.IConfiguration? config)
{
    ConfigureContexts(config);
    TerminalGlyphs.Configure(config?["Ui:Glyphs"], args.Contains("--ascii", StringComparer.Ordinal));
}

if (args.Contains("--list-contexts", StringComparer.Ordinal))
{
    ConfigureForPrinting(TryLoadConfig());
    Console.WriteLine($"Contexts folder: {RalfAI.Profiles.ContextStore.Folder}");
    foreach (var (key, ctx) in ContextRegistry.All(Directory.GetCurrentDirectory()))
        Console.WriteLine($"{key,-10} {ctx.Name} — {ctx.Description}");
    return 0;
}

// --export-context <name>: print a context as YAML, as a starting point for a new one.
var exportIndex = Array.IndexOf(args, "--export-context");
if (exportIndex < 0) exportIndex = Array.IndexOf(args, "--export-profile");
if (exportIndex >= 0)
{
    ConfigureForPrinting(TryLoadConfig());
    var exportName = exportIndex + 1 < args.Length ? args[exportIndex + 1] : "";
    try
    {
        Console.Write(RalfAI.Profiles.ContextStore.ToYaml(ContextRegistry.Create(exportName, Directory.GetCurrentDirectory())));
        return 0;
    }
    catch (ArgumentException ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
}

// Values of flags (--context rust, --config x.json) are not the target folder.
var flagsWithValue = new[] { "--context", "--config", "--contexts-source" };
var positional = args.Where((a, i) => !a.StartsWith("--", StringComparison.Ordinal)
                                      && (i == 0 || !flagsWithValue.Contains(args[i - 1], StringComparer.Ordinal))).ToArray();
var contextFlagIndex = Array.IndexOf(args, "--context");
var contextFlagValue = contextFlagIndex >= 0 && contextFlagIndex + 1 < args.Length
    ? args[contextFlagIndex + 1]
    : null;

// Loaded before the folder picker so F4 (open in external editor) can use it there too.
var configuration = RalfAIConfig.Load(configFlagValue);

// The chat runs in the normal screen buffer so the conversation stays in the terminal's scrollback. Ui:FullScreen true
// (or --fullscreen) runs the whole session in the alternate buffer instead, like vim/less: nothing is left behind on exit,
// but there is no scrolling back either. No-op when output isn't a real console.
using var fullScreen = args.Contains("--fullscreen", StringComparer.Ordinal)
                       || string.Equals(configuration["Ui:FullScreen"], "true", StringComparison.OrdinalIgnoreCase)
    ? FullScreenConsole.Enter()
    : null;
ConfigureContexts(configuration);
// --contexts-source <folder or URL> wins over Contexts:Source for this run, and always checks it.
var sourceFlagIndex = Array.IndexOf(args, "--contexts-source");
var sourceFlagValue = sourceFlagIndex >= 0 && sourceFlagIndex + 1 < args.Length ? args[sourceFlagIndex + 1] : null;
await CheckContextSourceAsync(configuration, sourceFlagValue);
// Before anything is drawn: UTF-8 where the terminal copes, plain-ASCII stand-ins for what the console cannot show.
TerminalGlyphs.Configure(configuration["Ui:Glyphs"], args.Contains("--ascii", StringComparer.Ordinal));
var editorCommand = configuration["Editor"] ?? FileBrowser.DefaultEditor;

string targetFolder;
if (positional.Length == 0)
{
    // No folder on the command line: resume the last one used with no prompt at all, and
    // only fall back to the picker the very first time (when nothing is remembered yet).
    var lastFolder = LastFolderStore.Load();
    if (lastFolder is not null)
    {
        targetFolder = lastFolder;
    }
    else
    {
        var picked = FileBrowser.Pick(Directory.GetCurrentDirectory(), editorCommand);
        if (picked is null) return 1;
        targetFolder = Path.GetFullPath(picked);
    }
}
else
{
    targetFolder = Path.GetFullPath(positional[0]);
}

Directory.CreateDirectory(targetFolder);
LastFolderStore.Save(targetFolder);

// --context flag wins, then the target folder's own remembered context
// (.ralfai/config.yaml), then appsettings/env "AgentContext", then ContextRegistry.DefaultName.
var contextKey = contextFlagValue
    ?? FolderConfigStore.LoadContext(targetFolder)
    ?? configuration["AgentContext"]
    ?? ContextRegistry.DefaultName;
IAgentContext context;
try
{
    context = ContextRegistry.Create(contextKey, targetFolder);
}
catch (ArgumentException ex)
{
    // A context remembered by the folder may have been deleted or renamed since: start with the context designer.
    Console.Error.WriteLine($"ralf: {ex.Message}");
    contextKey = ContextRegistry.IntegratedName;
    context = ContextRegistry.Create(contextKey, targetFolder);
}
FolderConfigStore.SaveContext(targetFolder, contextKey);

// --debug flag wins, then the target folder's remembered setting, then off by default.
var debug = args.Contains("--debug", StringComparer.Ordinal)
    || (FolderConfigStore.LoadDebug(targetFolder) ?? false);
FolderConfigStore.SaveDebug(targetFolder, debug);

var app = await RalfAIApp.CreateAsync(configuration, targetFolder, contextKey, context, debug);
await app.RunAsync();
return 0;

Microsoft.Extensions.Configuration.IConfiguration? TryLoadConfig()
{
    try { return RalfAIConfig.Load(configFlagValue); }
    catch (Exception ex) when (ex is IOException or InvalidDataException or FormatException) { return null; }
}

// Looks at the context source (Contexts:Source, by default RalfAI's GitHub folder) when Contexts:Check asks for it or
// Contexts:CheckEveryDays says it is time, adds the contexts the folder lacks, updates the unchanged ones, and then turns
// Contexts:Check off in the config file. Offline or unreachable: says so and tries again at the next start.
async Task CheckContextSourceAsync(Microsoft.Extensions.Configuration.IConfiguration config, string? sourceFromCommandLine)
{
    var folder = RalfAI.Profiles.ContextStore.Folder;
    if (!RalfAI.Profiles.ContextSync.IsDue(config, folder, sourceFromCommandLine is not null, DateTimeOffset.Now)) return;

    RalfAI.Profiles.ContextStore.Seed(ContextRegistry.ProfileError);        // first what is built in, so the state knows it
    var source = RalfAI.Profiles.ContextSync.CreateSource(sourceFromCommandLine ?? config["Contexts:Source"]);
    try
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var report = await RalfAI.Profiles.ContextSync.SyncAsync(source, folder, markChecked: true, timeout.Token);

        if (report.Added.Count > 0) Console.WriteLine($"ralf: contesti nuovi da {source.Description}: {string.Join(", ", report.Added)}");
        if (report.Updated.Count > 0) Console.WriteLine($"ralf: contesti aggiornati: {string.Join(", ", report.Updated)}");
        if (report.KeptChanged.Count > 0)
            Console.WriteLine($"ralf: c'è una versione nuova di {string.Join(", ", report.KeptChanged)}, ma hai modificato il file e l'ho lasciato com'è " +
                              "(per riceverla: cancella il file e la sua voce in .state.json).");
        foreach (var problem in report.Rejected) Console.Error.WriteLine($"ralf: contesto scartato: {problem}");

        if (sourceFromCommandLine is null && !string.Equals(config["Contexts:Check"], "false", StringComparison.OrdinalIgnoreCase))
        {
            var why = RalfAI.Profiles.ContextSync.TryTurnOffCheck(RalfAIConfig.ExternalPath(configFlagValue));
            if (why is not null && bool.TryParse(config["Contexts:Check"], out var on) && on)
                Console.Error.WriteLine($"ralf: non ho potuto rimettere Contexts:Check a false ({why}): controllerò a ogni avvio.");
        }
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or FormatException or UnauthorizedAccessException)
    {
        Console.Error.WriteLine($"ralf: contesti: non riesco a leggere {source.Description} ({ex.Message}); riprovo al prossimo avvio.");
    }
}
