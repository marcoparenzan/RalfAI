using RalfAI;
using RalfAI.Contexts;

if (args.Contains("--version", StringComparer.Ordinal) || args.Contains("-v", StringComparer.Ordinal))
{
    Console.WriteLine($"RalfAI {AppVersion.Current}");
    return 0;
}

if (args.Contains("--list-contexts", StringComparer.Ordinal))
{
    ContextRegistry.ProfileError = msg => Console.Error.WriteLine($"ralf: skipped profile {msg}");
    foreach (var (key, ctx) in ContextRegistry.All(Directory.GetCurrentDirectory()))
        Console.WriteLine($"{key,-10} {ctx.Name} — {ctx.Description}");
    return 0;
}

// --export-profile <name>: print a built-in context as a profile file, as a starting point
// for ~/.ralfai/profiles/<name>.md or <target>/.ralfai/profiles/<name>.md.
var exportIndex = Array.IndexOf(args, "--export-profile");
if (exportIndex >= 0)
{
    var exportName = exportIndex + 1 < args.Length ? args[exportIndex + 1] : "";
    var builtIn = ContextRegistry.CreateBuiltIn(exportName);
    if (builtIn is null)
    {
        Console.Error.WriteLine($"Unknown built-in context '{exportName}'. Try: dotnet, markdown, html.");
        return 1;
    }
    Console.WriteLine(RalfAI.Profiles.ProfileLoader.Export(builtIn));
    return 0;
}

var positional = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();
var contextFlagIndex = Array.IndexOf(args, "--context");
var contextFlagValue = contextFlagIndex >= 0 && contextFlagIndex + 1 < args.Length
    ? args[contextFlagIndex + 1]
    : null;

// --config wins over the RALFAI_CONFIG_PATH environment variable (see RalfAIConfig.Load).
var configFlagIndex = Array.IndexOf(args, "--config");
var configFlagValue = configFlagIndex >= 0 && configFlagIndex + 1 < args.Length
    ? args[configFlagIndex + 1]
    : null;

// The whole interactive session — folder picker included — runs in the terminal's alternate
// screen buffer, so nothing leaks into the user's normal scrollback and it's restored exactly
// as it was on exit (same technique vim/less/htop use). No-ops when output isn't a real console.
using var fullScreen = FullScreenConsole.Enter();

// Loaded before the folder picker so F4 (open in external editor) can use it there too.
var configuration = RalfAIConfig.Load(configFlagValue);
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
ContextRegistry.ProfileError = msg => Console.Error.WriteLine($"ralf: skipped profile {msg}");
IAgentContext context = ContextRegistry.Create(contextKey, targetFolder);
FolderConfigStore.SaveContext(targetFolder, contextKey);

// --debug flag wins, then the target folder's remembered setting, then off by default.
var debug = args.Contains("--debug", StringComparer.Ordinal)
    || (FolderConfigStore.LoadDebug(targetFolder) ?? false);
FolderConfigStore.SaveDebug(targetFolder, debug);

var app = await RalfAIApp.CreateAsync(configuration, targetFolder, contextKey, context, debug);
await app.RunAsync();
return 0;
