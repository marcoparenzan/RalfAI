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

if (args.Contains("--list-contexts", StringComparer.Ordinal))
{
    ConfigureContexts(TryLoadConfig());
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
    ConfigureContexts(TryLoadConfig());
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
var flagsWithValue = new[] { "--context", "--config" };
var positional = args.Where((a, i) => !a.StartsWith("--", StringComparison.Ordinal)
                                      && (i == 0 || !flagsWithValue.Contains(args[i - 1], StringComparer.Ordinal))).ToArray();
var contextFlagIndex = Array.IndexOf(args, "--context");
var contextFlagValue = contextFlagIndex >= 0 && contextFlagIndex + 1 < args.Length
    ? args[contextFlagIndex + 1]
    : null;

// The whole interactive session — folder picker included — runs in the terminal's alternate
// screen buffer, so nothing leaks into the user's normal scrollback and it's restored exactly
// as it was on exit (same technique vim/less/htop use). No-ops when output isn't a real console.
using var fullScreen = FullScreenConsole.Enter();

// Loaded before the folder picker so F4 (open in external editor) can use it there too.
var configuration = RalfAIConfig.Load(configFlagValue);
ConfigureContexts(configuration);
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
