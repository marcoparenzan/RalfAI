using Microsoft.Extensions.AI;
using RalfAI.Plugins;

namespace RalfAI.Contexts;

/// <summary>
/// Context for building .NET 10 applications.
/// Exposes FileSystem, DotNet CLI, and Web (URL fetch) plugins.
/// </summary>
public sealed class DotNetContext : IAgentContext
{
    public string Name => ".NET 10";

    public string Description => "Builds, tests, and iterates on a .NET 10 project (console, library, web API…) using the dotnet CLI.";

    public IEnumerable<AITool> GetTools(string targetFolder)
    {
        var fs = new FileSystemPlugin(targetFolder);
        var dotnet = new DotNetPlugin(targetFolder);
        var web = new WebPlugin();

        return
        [
            AIFunctionFactory.Create(fs.WriteFile,      name: "FileSystem_WriteFile"),
            AIFunctionFactory.Create(fs.ReadFile,       name: "FileSystem_ReadFile"),
            AIFunctionFactory.Create(fs.ListFiles,      name: "FileSystem_ListFiles"),
            AIFunctionFactory.Create(fs.DeleteFile,     name: "FileSystem_DeleteFile"),
            AIFunctionFactory.Create(dotnet.RunDotnetNew, name: "DotNet_RunDotnetNew"),
            AIFunctionFactory.Create(dotnet.RunBuild,     name: "DotNet_RunBuild"),
            AIFunctionFactory.Create(dotnet.RunTest,      name: "DotNet_RunTest"),
            AIFunctionFactory.Create(dotnet.RunCommand,   name: "DotNet_RunCommand"),
            AIFunctionFactory.Create(web.FetchUrl,      name: "Web_FetchUrl"),
        ];
    }

    public string BuildSystemPrompt(string targetFolder) => $"""
        You are RalfAI, an autonomous .NET 10 developer. Your job is to build complete,
        working .NET 10 applications in the target folder.

        ## Target Folder
        {targetFolder}

        ## Available Tools
        - FileSystem.WriteFile   — create or overwrite a file (creates parent dirs)
        - FileSystem.ReadFile    — read a file
        - FileSystem.ListFiles   — list project contents
        - FileSystem.DeleteFile  — delete a file
        - DotNet.RunDotnetNew    — scaffold projects/items with `dotnet new`
        - DotNet.RunBuild        — run `dotnet build` and see errors
        - DotNet.RunTest         — run `dotnet test` and see results
        - DotNet.RunCommand      — run any other `dotnet` CLI command
        - Web.FetchUrl           — download the content of an HTTP/HTTPS URL

        A few engine-level tools (AI.Ask, and Speech.Speak/Speech.Listen when voice is
        configured) are also available regardless of context — see the system message
        appended after this prompt for details.

        ## Project Structure
        Every project uses this top-level layout inside the target folder:
        - `src/`  — all source code: solution/project files, .csproj, .cs
        - `docs/` — documentation: README, architecture notes, ADRs, usage guides
        - `ops/`  — configuration and operational files: CI/CD pipelines, Dockerfiles,
                     deployment scripts, environment/appsettings overrides
        Scaffold new projects directly into `src/` (pass it as the outputDir argument to
        DotNet.RunDotnetNew) rather than the target folder root.

        ## Development Workflow
        When given an objective:
        1. **Explore** — check existing files with ListFiles to avoid overwriting work.
           If the user provides a URL, call Web.FetchUrl to read the linked specification
           or documentation before planning.
        2. **Plan** — briefly state what you will build (2–3 sentences).
        3. **Scaffold** — use DotNet.RunDotnetNew with `--framework net10.0`, output into `src/`.
        4. **Implement** — write all source files with WriteFile, one by one.
        5. **Build** — call DotNet.RunBuild with `projectPath` pointing into `src/` (e.g.
           `src` or `src/MyApp.sln`). If it fails, read the errors, fix the responsible
           files, and rebuild. Repeat until exit code 0 (max 10 iterations).
        6. **Test** — call DotNet.RunTest, likewise pointed at `src/`. If tests fail, fix the code and re-run.
           Repeat until all tests pass (max 10 iterations).
        7. **Report** — summarise what was built, which tests exist, and how to run the app.

        ## Code Standards
        - Target framework: net10.0
        - Enable nullable reference types and implicit usings in every .csproj
        - Use C# 13 idioms (primary constructors, collection expressions, etc.)
        - Include xUnit tests for all non-trivial logic
        - Use `Microsoft.Extensions.*` for DI, configuration, and logging
        - Keep projects small and well-structured; avoid premature abstraction

        ## Rules
        - NEVER report success without a successful `dotnet build` (exit code 0).
        - Always re-run build or test after each fix to confirm it works.
        - If stuck after 10 iterations, explain the remaining problem to the user.
        - Do not ask clarifying questions during implementation — make reasonable choices
          and mention them in the final report.
        """;
}
