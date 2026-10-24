using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace RalfAI.Plugins;

/// <summary>
/// Exposes dotnet CLI commands to the agent: new, build, test, publish, installing a project as a global tool, and
/// arbitrary commands.
/// All commands run inside the target project folder (or a subdirectory thereof).
/// </summary>
public sealed class DotNetPlugin(string rootFolder)
{
    [Description(
        "Run 'dotnet new' to scaffold a project or file template. " +
        "Returns the full command output. " +
        "Use the '--framework' given in the system prompt (e.g. net10.0 or net11.0) for new projects.")]
    public Task<string> RunDotnetNew(
        [Description("Template name: 'console', 'classlib', 'xunit', 'webapi', 'sln', etc.")] string template,
        [Description("Project or solution name (optional)")] string? name = null,
        [Description("Subdirectory relative to project root to run in (optional)")] string? outputDir = null,
        [Description("Extra arguments, e.g. '--framework net11.0 --no-restore'")] string? additionalArgs = null)
    {
        var args = new StringBuilder($"new {template}");
        if (!string.IsNullOrWhiteSpace(name)) args.Append($" --name {name}");
        if (!string.IsNullOrWhiteSpace(additionalArgs)) args.Append($" {additionalArgs}");

        var workDir = ResolveWorkDir(outputDir);
        Directory.CreateDirectory(workDir);
        return RunDotnet(args.ToString(), workDir);
    }

    [Description(
        "Run 'dotnet build' and return the full output including compilation errors. " +
        "Exit code 0 means success.")]
    public Task<string> RunBuild(
        [Description("Subdirectory or project/solution file relative to project root (optional)")] string? projectPath = null)
    {
        var workDir = ResolveWorkDir(Path.GetDirectoryName(projectPath));
        var target = string.IsNullOrWhiteSpace(projectPath) ? "" : $" \"{ResolveFile(projectPath)}\"";
        return RunDotnet($"build{target}", workDir);
    }

    [Description(
        "Run 'dotnet test' and return the full output including individual test failures. " +
        "Exit code 0 means all tests passed.")]
    public Task<string> RunTest(
        [Description("Subdirectory or test project file relative to project root (optional)")] string? projectPath = null)
    {
        var workDir = ResolveWorkDir(Path.GetDirectoryName(projectPath));
        var target = string.IsNullOrWhiteSpace(projectPath) ? "" : $" \"{ResolveFile(projectPath)}\"";
        return RunDotnet($"test{target} --logger \"console;verbosity=normal\"", workDir);
    }

    [Description(
        "Run 'dotnet publish' to produce the files to deploy or ship. Returns the full output; exit code 0 means success. " +
        "The output goes to the given folder, by default 'publish/<project name>' under the project root.")]
    public Task<string> RunPublish(
        [Description("Project file relative to project root, e.g. 'src/MyApp/MyApp.csproj'")] string projectPath,
        [Description("Build configuration (default Release)")] string configuration = "Release",
        [Description("Runtime identifier for a platform-specific build, e.g. 'win-x64', 'linux-x64', 'osx-arm64' (optional)")] string? runtime = null,
        [Description("true to bundle the .NET runtime (needs a runtime), false to depend on an installed one (optional)")] bool? selfContained = null,
        [Description("Output folder relative to project root (optional)")] string? outputDir = null,
        [Description("Extra arguments, e.g. '-p:PublishSingleFile=true' or '-p:PublishAot=true' (optional)")] string? additionalArgs = null)
    {
        var project = ResolveFile(projectPath);
        var output = ResolveWorkDir(string.IsNullOrWhiteSpace(outputDir)
            ? Path.Combine("publish", Path.GetFileNameWithoutExtension(project))
            : outputDir);

        var args = new StringBuilder($"publish \"{project}\" -c {configuration} -o \"{output}\"");
        if (!string.IsNullOrWhiteSpace(runtime)) args.Append($" -r {runtime}");
        if (selfContained is { } sc) args.Append(sc ? " --self-contained true" : " --self-contained false");
        if (!string.IsNullOrWhiteSpace(additionalArgs)) args.Append($" {additionalArgs}");
        return RunDotnet(args.ToString(), ResolveWorkDir(Path.GetDirectoryName(projectPath)));
    }

    [Description(
        "Install or update a project of the target folder as a .NET global tool for the current user, so its command can be run " +
        "from any terminal: packs it ('dotnet pack', into .ralfai/nupkg) and runs 'dotnet tool update -g' from that package. " +
        "The project must set <PackAsTool>true</PackAsTool> and should set <ToolCommandName>; raise <Version> to update an installed tool. " +
        "It fails if the tool is running.")]
    public async Task<string> InstallGlobalTool(
        [Description("Project file relative to project root, e.g. 'src/MyTool/MyTool.csproj'")] string projectPath)
    {
        var project = ResolveFile(projectPath);
        if (!File.Exists(project)) return $"Project not found: {projectPath}";

        var info = ToolProject.Read(File.ReadAllText(project), Path.GetFileNameWithoutExtension(project));
        if (!info.PackAsTool)
            return $"{projectPath} is not a tool project: add <PackAsTool>true</PackAsTool> and <ToolCommandName>name</ToolCommandName> " +
                   "to its <PropertyGroup>, then call this again.";

        var packages = Path.Combine(rootFolder, ".ralfai", "nupkg");
        Directory.CreateDirectory(packages);
        var workDir = ResolveWorkDir(Path.GetDirectoryName(projectPath));

        var pack = await RunDotnet($"pack \"{project}\" -c Release -o \"{packages}\"", workDir);
        if (!pack.Contains("Exit code: 0", StringComparison.Ordinal)) return "Packing failed, nothing installed.\n" + pack;

        var version = info.Version is null ? "" : $" --version {info.Version}";
        var install = await RunDotnet($"tool update -g {info.PackageId} --add-source \"{packages}\"{version}", workDir);

        var command = info.CommandName ?? info.PackageId;
        return $"Package {info.PackageId}{(info.Version is null ? "" : " " + info.Version)}; the command is '{command}'.\n" +
               install + "\n--- pack ---\n" + pack;
    }

    [Description(
        "Run any other dotnet CLI command in the target folder, " +
        "e.g. 'add package xunit', 'restore', 'sln add MyProject/MyProject.csproj'.")]
    public Task<string> RunCommand(
        [Description("The arguments to pass to dotnet, e.g. 'add package Newtonsoft.Json'")] string arguments,
        [Description("Subdirectory relative to project root to run in (optional)")] string? workingDirectory = null)
    {
        var workDir = ResolveWorkDir(workingDirectory);
        return RunDotnet(arguments, workDir);
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private string ResolveWorkDir(string? relative)
    {
        if (string.IsNullOrWhiteSpace(relative)) return rootFolder;
        var full = Path.GetFullPath(Path.Combine(rootFolder, relative));
        if (!full.StartsWith(rootFolder, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Path traversal blocked: {relative}");
        return full;
    }

    private string ResolveFile(string relative)
    {
        var full = Path.GetFullPath(Path.Combine(rootFolder, relative));
        if (!full.StartsWith(rootFolder, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Path traversal blocked: {relative}");
        return full;
    }

    private static async Task<string> RunDotnet(string arguments, string workingDirectory)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        var sb = new StringBuilder();
        sb.AppendLine($"$ dotnet {arguments}");
        sb.AppendLine($"Working dir: {workingDirectory}");
        sb.AppendLine($"Exit code: {process.ExitCode}");

        if (!string.IsNullOrWhiteSpace(stdout))
        {
            sb.AppendLine("--- STDOUT ---");
            sb.AppendLine(stdout.TrimEnd());
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            sb.AppendLine("--- STDERR ---");
            sb.AppendLine(stderr.TrimEnd());
        }

        return sb.ToString();
    }
}

/// <summary>What a project file says about packing it as a .NET tool.</summary>
public sealed record ToolProject(bool PackAsTool, string PackageId, string? Version, string? CommandName)
{
    public static ToolProject Read(string csproj, string projectName)
    {
        string? Property(string name)
        {
            var match = System.Text.RegularExpressions.Regex.Match(csproj, $@"<{name}>\s*([^<]+?)\s*</{name}>");
            return match.Success ? match.Groups[1].Value : null;
        }

        var packAsTool = string.Equals(Property("PackAsTool"), "true", StringComparison.OrdinalIgnoreCase);
        var packageId = Property("PackageId") ?? Property("AssemblyName") ?? projectName;
        return new ToolProject(packAsTool, packageId, Property("Version"), Property("ToolCommandName"));
    }
}
