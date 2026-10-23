using Microsoft.Extensions.AI;
using RalfAI.Contexts;
using RalfAI.Plugins;

namespace RalfAI.Profiles;

/// <summary>
/// An <see cref="IAgentContext"/> loaded from a Markdown file with a small frontmatter header
/// (name, description, tools) and the system prompt as the body. <c>{{targetFolder}}</c> in the
/// body is replaced with the absolute target path.
/// </summary>
public sealed class ProfileContext(string name, string description, IReadOnlyList<string> tools, string promptTemplate)
    : IAgentContext
{
    public const string TargetFolderPlaceholder = "{{targetFolder}}";

    public string Name => name;
    public string Description => description;
    public IReadOnlyList<string> Tools => tools;

    public string BuildSystemPrompt(string targetFolder)
        => promptTemplate.Replace(TargetFolderPlaceholder, targetFolder, StringComparison.Ordinal);

    public IEnumerable<AITool> GetTools(string targetFolder) => ToolCatalog.Resolve(tools, targetFolder);
}

/// <summary>
/// Every tool a profile can enable. Profiles list either a whole group (<c>FileSystem</c>) or a
/// single tool (<c>FileSystem_ReadFile</c>). Tools stay code — profiles only pick by name.
/// </summary>
public static class ToolCatalog
{
    private static readonly Dictionary<string, Func<string, IEnumerable<AITool>>> Groups =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["FileSystem"] = folder =>
            {
                var fs = new FileSystemPlugin(folder);
                return
                [
                    AIFunctionFactory.Create(fs.WriteFile,  name: "FileSystem_WriteFile"),
                    AIFunctionFactory.Create(fs.ReadFile,   name: "FileSystem_ReadFile"),
                    AIFunctionFactory.Create(fs.ListFiles,  name: "FileSystem_ListFiles"),
                    AIFunctionFactory.Create(fs.DeleteFile, name: "FileSystem_DeleteFile"),
                ];
            },
            ["Web"] = _ =>
            {
                var web = new WebPlugin();
                return [AIFunctionFactory.Create(web.FetchUrl, name: "Web_FetchUrl")];
            },
            ["DotNet"] = folder =>
            {
                var dotnet = new DotNetPlugin(folder);
                return
                [
                    AIFunctionFactory.Create(dotnet.RunDotnetNew, name: "DotNet_RunDotnetNew"),
                    AIFunctionFactory.Create(dotnet.RunBuild,     name: "DotNet_RunBuild"),
                    AIFunctionFactory.Create(dotnet.RunTest,      name: "DotNet_RunTest"),
                    AIFunctionFactory.Create(dotnet.RunCommand,   name: "DotNet_RunCommand"),
                ];
            },
            ["Rust"] = folder =>
            {
                var rust = new RustPlugin(folder);
                return
                [
                    AIFunctionFactory.Create(rust.EnsureToolchain, name: "Rust_EnsureToolchain"),
                    AIFunctionFactory.Create(rust.RunCargo,        name: "Rust_RunCargo"),
                    AIFunctionFactory.Create(rust.RunRustup,       name: "Rust_RunRustup"),
                ];
            },
            ["StaticServer"] = folder =>
            {
                var server = new StaticServerPlugin(folder);
                return
                [
                    AIFunctionFactory.Create(server.Start, name: "StaticServer_Start"),
                    AIFunctionFactory.Create(server.Stop,  name: "StaticServer_Stop"),
                ];
            },
        };

    public static IEnumerable<string> GroupNames => Groups.Keys;

    public static IEnumerable<AITool> Resolve(IEnumerable<string> entries, string targetFolder)
    {
        // Build each group at most once, even when a profile names several tools from it.
        var built = new Dictionary<string, List<AITool>>(StringComparer.OrdinalIgnoreCase);
        var result = new List<AITool>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        List<AITool> Group(string group)
        {
            if (!built.TryGetValue(group, out var tools))
                built[group] = tools = Groups[group](targetFolder).ToList();
            return tools;
        }

        foreach (var entry in entries)
        {
            var group = entry.Contains('_') ? entry[..entry.IndexOf('_')] : entry;
            if (!Groups.ContainsKey(group))
                throw new ArgumentException(
                    $"Unknown tool '{entry}'. Available groups: {string.Join(", ", Groups.Keys)}");

            foreach (var tool in Group(group))
            {
                var wanted = entry.Contains('_')
                    ? string.Equals(tool.Name, entry, StringComparison.OrdinalIgnoreCase)
                    : true;
                if (wanted && seen.Add(tool.Name)) result.Add(tool);
            }
        }
        return result;
    }
}
