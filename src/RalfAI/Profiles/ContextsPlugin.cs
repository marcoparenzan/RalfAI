using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.AI;

namespace RalfAI.Profiles;

/// <summary>
/// Tools to list, read, write and delete context files (<see cref="ContextStore"/>). They work on the contexts folder, not on
/// the project, and only on <c>&lt;name&gt;.yaml</c> files there: a context is checked before it is written.
/// </summary>
public sealed class ContextsPlugin(string targetFolder)
{
    public IEnumerable<AITool> CreateTools() =>
    [
        AIFunctionFactory.Create(ListContexts,  name: "Contexts_ListContexts"),
        AIFunctionFactory.Create(ReadContext,   name: "Contexts_ReadContext"),
        AIFunctionFactory.Create(ListTools,     name: "Contexts_ListTools"),
        AIFunctionFactory.Create(SaveContext,   name: "Contexts_SaveContext"),
        AIFunctionFactory.Create(DeleteContext, name: "Contexts_DeleteContext"),
    ];

    [Description("List the contexts RalfAI knows: name to use, display name, description and the file that defines it.")]
    public string ListContexts()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Contexts folder: {ContextStore.Folder}");
        sb.AppendLine($"- {ContextRegistry.IntegratedName} - {ContextDesignerContext.DisplayName}: built into RalfAI, cannot be changed");
        foreach (var entry in ContextStore.LoadAll(targetFolder).Values.OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase))
        {
            var where = entry.InProject ? "project" : "user";
            sb.AppendLine($"- {entry.Key} - {entry.Context.Name}: {entry.Context.Description} [{where}: {entry.Path}]");
        }
        return sb.ToString().TrimEnd();
    }

    [Description("Read the YAML of a context, to copy its structure or to change it. Older Markdown profiles are shown converted to YAML.")]
    public string ReadContext([Description("The context name, as listed by ListContexts (the file name without .yaml)")] string name)
    {
        var entries = ContextStore.LoadAll(targetFolder);
        if (!entries.TryGetValue(name, out var entry))
        {
            if (string.Equals(name, ContextRegistry.IntegratedName, StringComparison.OrdinalIgnoreCase))
                return ContextStore.ToYaml(new ContextDesignerContext());
            return $"No context named '{name}'. Known: {string.Join(", ", entries.Keys.Order(StringComparer.OrdinalIgnoreCase))}";
        }
        return entry.Path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ? ContextStore.ToYaml(entry.Context) : File.ReadAllText(entry.Path);
    }

    [Description("List every tool a context can enable, by group, with what each tool does. In 'tools' a context names a whole group " +
                 "(e.g. FileSystem) or single tools (e.g. FileSystem_ReadFile).")]
    public string ListTools()
    {
        var sb = new StringBuilder();
        foreach (var (group, tools) in ToolCatalog.Describe(targetFolder))
        {
            sb.AppendLine($"## {group}");
            foreach (var tool in tools)
                sb.AppendLine($"- {tool.Name}: {FirstSentence(tool.Description)}");
        }
        return sb.ToString().TrimEnd();
    }

    [Description("Create or replace a context. The YAML is checked first (fields, tool names); if it is wrong nothing is written and " +
                 "the reason is returned. Saved contexts are available at once from /switch-context (F6).")]
    public string SaveContext(
        [Description("File name of the context: lowercase letters, digits, '-', '_' or '.', no spaces (e.g. 'python' or 'blazor-wasm')")] string name,
        [Description("The whole YAML file: name, description, tools, prompt")] string yaml,
        [Description("'user' (default) for all projects, 'project' to keep it in this project's .ralfai/contexts folder")] string scope = "user")
    {
        try
        {
            var path = ContextStore.Save(name, yaml, ProjectScope(scope));
            return $"OK - saved context '{name}' to {path}. The user can switch to it with /switch-context (F6) or `ralf --context {name}`.";
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return $"Not saved: {ex.Message}";
        }
    }

    [Description("Delete a context file. Use only when the user asked for it.")]
    public string DeleteContext(
        [Description("The context name")] string name,
        [Description("'user' (default) or 'project'")] string scope = "user")
    {
        try
        {
            var path = ContextStore.Delete(name, ProjectScope(scope));
            return path is null ? $"No {scope} context named '{name}'." : $"OK - deleted {path}.";
        }
        catch (ArgumentException ex)
        {
            return $"Not deleted: {ex.Message}";
        }
    }

    private string? ProjectScope(string scope) => scope.Trim().Equals("project", StringComparison.OrdinalIgnoreCase) ? targetFolder : null;

    private static string FirstSentence(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var end = text.IndexOf(". ", StringComparison.Ordinal);
        return end > 0 ? text[..(end + 1)] : text;
    }
}
