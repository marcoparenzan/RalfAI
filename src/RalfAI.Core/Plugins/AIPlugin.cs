using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace RalfAI.Plugins;

/// <summary>
/// Lets the agent delegate an isolated sub-question or sub-task to the same underlying model
/// (same provider/deployment as the main conversation), without adding it to the main
/// conversation history. Useful for summarizing a long file, rewriting a passage, or reasoning
/// about something without polluting the main task context.
/// </summary>
public sealed class AIPlugin(IChatClient chat)
{
    [Description(
        "Ask the underlying AI model an isolated question or sub-task, separate from the main " +
        "conversation (e.g. summarize a long piece of text, rewrite a passage, answer a factual " +
        "question). Returns the model's plain-text answer. Does not affect the main conversation history.")]
    public async Task<string> Ask(
        [Description("The prompt or question to send to the model")] string prompt)
    {
        var response = await chat.GetResponseAsync(prompt);
        return response.Text ?? "";
    }
}
