using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;

const string studioExecutable = "RalfAI.Studio.exe";
const string payloadFileName = "RalfAI.Studio.Payload.zip";
var assembly = Assembly.GetExecutingAssembly();
var version = assembly.GetName().Version?.ToString(3) ?? "unknown";

if (args.Any(arg => arg is "--version" or "-v"))
{
    Console.WriteLine($"RalfAI Studio {version}");
    return 0;
}

if (args.Any(arg => arg is "--help" or "-h"))
{
    Console.WriteLine($"RalfAI Studio {version}");
    Console.WriteLine("Usage: ralf-studio [workspace]");
    Console.WriteLine("Starts the Windows Blazor Hybrid editor for the selected workspace.");
    return 0;
}

if (!OperatingSystem.IsWindows())
{
    Console.Error.WriteLine("RalfAI Studio currently requires Windows and Microsoft Edge WebView2 Runtime.");
    return 1;
}

var cacheRoot = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "RalfAI", "Studio", version);
var executablePath = Path.Combine(cacheRoot, studioExecutable);

try
{
    if (!File.Exists(executablePath))
        ExtractPayload(cacheRoot);

    if (!File.Exists(executablePath))
        throw new FileNotFoundException($"The Studio payload did not contain {studioExecutable}.", executablePath);

    var workspace = args.FirstOrDefault() ?? Directory.GetCurrentDirectory();
    workspace = Path.GetFullPath(workspace);
    if (!Directory.Exists(workspace))
        throw new DirectoryNotFoundException($"Workspace not found: {workspace}");

    var startInfo = new ProcessStartInfo(executablePath)
    {
        UseShellExecute = false,
        WorkingDirectory = workspace
    };
    startInfo.ArgumentList.Add(workspace);

    var process = Process.Start(startInfo)
        ?? throw new InvalidOperationException("Windows did not start the Studio process.");
    Console.WriteLine($"RalfAI Studio {version} started (PID {process.Id}) for {workspace}");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Unable to start RalfAI Studio {version}: {exception.Message}");
    Console.Error.WriteLine("Repair the global installation with: .\\ops\\install-studio-tool.ps1");
    return 1;
}

static void ExtractPayload(string destination)
{
    var launcherDirectory = AppContext.BaseDirectory;
    var payloadPath = Path.Combine(launcherDirectory, payloadFileName);
    if (!File.Exists(payloadPath))
        throw new FileNotFoundException("The installed tool does not contain the Studio payload.", payloadPath);

    var temporaryDirectory = destination + ".extracting-" + Guid.NewGuid().ToString("N");
    try
    {
        Directory.CreateDirectory(temporaryDirectory);
        ZipFile.ExtractToDirectory(payloadPath, temporaryDirectory, overwriteFiles: true);

        if (!File.Exists(Path.Combine(temporaryDirectory, studioExecutable)))
            throw new InvalidDataException($"Studio payload does not contain {studioExecutable}.");

        if (Directory.Exists(destination))
            Directory.Delete(destination, recursive: true);
        Directory.Move(temporaryDirectory, destination);
        File.WriteAllText(Path.Combine(destination, ".complete"), DateTimeOffset.UtcNow.ToString("O"));
    }
    finally
    {
        if (Directory.Exists(temporaryDirectory))
            Directory.Delete(temporaryDirectory, recursive: true);
    }
}
