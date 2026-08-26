using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace RalfAI.Plugins;

public sealed class AIPlugin(IChatClient chat)
{
	[Description("Ask the underlying AI model an isolated question or sub-task, separate from the main conversation (e.g. summarize a long piece of text, rewrite a passage, answer a factual question). Returns the model's plain-text answer. Does not affect the main conversation history.")]
	public async Task<string> Ask([Description("The prompt or question to send to the model")] string prompt)
	{
		return (await chat.GetResponseAsync(prompt)).Text ?? "";
	}
}
