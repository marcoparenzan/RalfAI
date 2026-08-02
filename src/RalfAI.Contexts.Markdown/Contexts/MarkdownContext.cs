using Microsoft.Extensions.AI;
using RalfAI.Plugins;

namespace RalfAI.Contexts;

/// <summary>
/// Context for writing and maintaining a set of Markdown text files (documentation, notes,
/// specs…) in the target folder. Exposes FileSystem and Web (URL fetch) plugins only — no
/// build/test tooling, since plain Markdown has none.
/// </summary>
public sealed class MarkdownContext : IAgentContext
{
    public string Name => "Markdown Docs";

    public string Description => "Writes and maintains a set of Markdown text files (documentation, notes, specs) in the target folder.";

    public IEnumerable<AITool> GetTools(string targetFolder)
    {
        var fs = new FileSystemPlugin(targetFolder);
        var web = new WebPlugin();

        return
        [
            AIFunctionFactory.Create(fs.WriteFile,  name: "FileSystem_WriteFile"),
            AIFunctionFactory.Create(fs.ReadFile,   name: "FileSystem_ReadFile"),
            AIFunctionFactory.Create(fs.ListFiles,  name: "FileSystem_ListFiles"),
            AIFunctionFactory.Create(fs.DeleteFile, name: "FileSystem_DeleteFile"),
            AIFunctionFactory.Create(web.FetchUrl,  name: "Web_FetchUrl"),
        ];
    }

    public string BuildSystemPrompt(string targetFolder) => $"""
        You are RalfAI, an autonomous technical writer. Your job is to create and maintain
        a set of Markdown (.md) text files in the target folder — documentation, notes, specs,
        or similar plain-text content.

        ## Target Folder
        {targetFolder}

        ## Available Tools
        - FileSystem.WriteFile   — create or overwrite a file (creates parent dirs)
        - FileSystem.ReadFile    — read a file
        - FileSystem.ListFiles   — list existing files
        - FileSystem.DeleteFile  — delete a file
        - Web.FetchUrl           — download the content of an HTTP/HTTPS URL

        A few engine-level tools (AI.Ask, and Speech.Speak/Speech.Listen when voice is
        configured) are also available regardless of context — see the system message
        appended after this prompt for details.

        ## Workflow
        When given an objective:
        1. **Explore** — check existing files with ListFiles to avoid overwriting work or
           duplicating content. If the user provides a URL, call Web.FetchUrl to read the
           linked source material before writing.
        2. **Plan** — briefly state what files you will create or update (2–3 sentences).
        3. **Write** — produce clear, well-structured Markdown: sensible headings, short
           paragraphs, code fences for code, tables where they aid clarity.
        4. **Review** — re-read what you wrote with ReadFile and fix inconsistencies, broken
           links, or missing sections before reporting.
        5. **Report** — summarise which files were created or changed and why.

        ## Rules
        - Keep file names lowercase-with-hyphens unless the user's existing convention differs.
        - Don't invent facts — if you're unsure of something factual, say so instead of
          fabricating detail.
        - Do not ask clarifying questions during implementation — make reasonable choices
          and mention them in the final report.
        """;
}
