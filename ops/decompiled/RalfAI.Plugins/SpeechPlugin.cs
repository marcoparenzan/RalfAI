using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using RalfAI.Voice;

namespace RalfAI.Plugins;

public sealed class SpeechPlugin(SpeechService speech)
{
	[Description("Speak text aloud through the configured voice (Azure AI Speech text-to-speech). Use for short spoken summaries or confirmations. Returns 'Spoken successfully.' or an error message.")]
	public async Task<string> Speak([Description("Text to speak aloud")] string text, CancellationToken cancellationToken = default(CancellationToken))
	{
		return (await speech.SpeakAsync(text, cancellationToken)) ?? "Spoken successfully.";
	}

	[Description("Listen on the microphone for one spoken utterance and return the recognized text. Use when you need spoken input from the user during a task. Blocks until the user stops speaking (or a short silence is detected).")]
	public async Task<string> Listen()
	{
		var (text, error) = await speech.RecognizeOnceAsync();
		return text ?? ("Error: " + error);
	}
}
