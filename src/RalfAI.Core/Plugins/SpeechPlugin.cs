using System.ComponentModel;
using RalfAI.Voice;

namespace RalfAI.Plugins;

/// <summary>
/// Lets the agent speak text aloud and listen for spoken input during a task, via the same
/// <see cref="SpeechService"/> used for the app's /voice and /speak UX. Only registered when
/// AzureSpeech is configured.
/// </summary>
public sealed class SpeechPlugin(SpeechService speech)
{
    [Description(
        "Speak text aloud through the configured voice (Azure AI Speech text-to-speech). " +
        "Use for short spoken summaries or confirmations. Returns 'Spoken successfully.' or an error message.")]
    public async Task<string> Speak(
        [Description("Text to speak aloud")] string text,
        CancellationToken cancellationToken = default)
    {
        var error = await speech.SpeakAsync(text, cancellationToken);
        return error ?? "Spoken successfully.";
    }

    [Description(
        "Listen on the microphone for one spoken utterance and return the recognized text. " +
        "Use when you need spoken input from the user during a task. Blocks until the user " +
        "stops speaking (or a short silence is detected).")]
    public async Task<string> Listen()
    {
        var (text, error) = await speech.RecognizeOnceAsync();
        return text ?? $"Error: {error}";
    }
}
