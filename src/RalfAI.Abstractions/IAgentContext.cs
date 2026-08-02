using Microsoft.Extensions.AI;

namespace RalfAI.Contexts;

/// <summary>
/// Defines one target generation context (e.g. a .NET project, a set of Markdown docs, a
/// Python package…). Implement this to add new content/technology stacks without touching
/// RalfAIApp.
/// </summary>
public interface IAgentContext
{
    /// <summary>Display name shown in the banner.</summary>
    string Name { get; }

    /// <summary>
    /// One-line description of what this context is for, shown by --list-contexts and by
    /// the in-app context-info command.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Full system prompt sent to the model.
    /// Receives the absolute path of the target folder.
    /// </summary>
    string BuildSystemPrompt(string targetFolder);

    /// <summary>
    /// The tools (function tools built with <c>AIFunctionFactory.Create</c>) that belong to
    /// this context. Called once when the agent is built.
    /// </summary>
    IEnumerable<AITool> GetTools(string targetFolder);
}
