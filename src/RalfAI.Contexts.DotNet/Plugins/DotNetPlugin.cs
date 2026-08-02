using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace RalfAI.Plugins;

/// <summary>
/// Exposes dotnet CLI commands to the agent: new, build, test, and arbitrary commands.
/// All commands run inside the target project folder (or a subdirectory thereof).
/// </summary>
public sealed class DotNetPlugin(string rootFolder)
{
    [Description(
        "Run 'dotnet new' to scaffold a project or file template. " +
        "Returns the full command output. " +
        "Use '--framework net10.0' for new projects.")]
    public Task<string> RunDotnetNew(
        [Description("Template name: 'console', 'classlib', 'xunit', 'webapi', 'sln', etc.")] string template,
        [Description("Project or solution name (optional)")] string? name = null,
        [Description("Subdirectory relative to project root to run in (optional)")] string? outputDir = null,
        [Description("Extra arguments, e.g. '--framework net10.0 --no-restore'")] string? additionalArgs = null)
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
