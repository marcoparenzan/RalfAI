using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;

namespace RalfAI.Voice;

/// <summary>
/// Wraps Azure AI Speech for dictation (STT) and reading responses aloud (TTS).
/// App-level UX feature — not exposed to the model as a tool. Optional: <see cref="FromConfig"/>
/// returns null when Region/ApiKey are not configured, so RalfAI works fine without it.
/// </summary>
public sealed class SpeechService(string region, string apiKey, string recognitionLanguage, string voiceName)
{
    public static SpeechService? FromConfig(IConfiguration config)
    {
        var region = config["AzureSpeech:Region"];
        var apiKey = config["AzureSpeech:ApiKey"];
        if (string.IsNullOrWhiteSpace(region) || string.IsNullOrWhiteSpace(apiKey))
            return null;

        var language = config["AzureSpeech:RecognitionLanguage"] ?? "it-IT";
        var voice = config["AzureSpeech:VoiceName"] ?? "it-IT-IsabellaNeural";
        return new SpeechService(region, apiKey, language, voice);
    }

    /// <summary>
    /// Listens on the default microphone for one utterance, stopping automatically once the
    /// SDK detects a pause. Returns the recognized text, or an error message on failure.
    /// </summary>
    public async Task<(string? Text, string? Error)> RecognizeOnceAsync()
    {
        var speechConfig = SpeechConfig.FromSubscription(apiKey, region);
        speechConfig.SpeechRecognitionLanguage = recognitionLanguage;

        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

        var result = await recognizer.RecognizeOnceAsync();

        return result.Reason switch
        {
            ResultReason.RecognizedSpeech => (result.Text, null),
            ResultReason.NoMatch => (null, "Nessun parlato riconosciuto."),
            ResultReason.Canceled => (null, $"Riconoscimento annullato: {CancellationDetails.FromResult(result).ErrorDetails}"),
            _ => (null, "Riconoscimento non riuscito.")
        };
    }

    /// <summary>
    /// Synthesizes <paramref name="text"/> and plays it through the default speaker.
    /// Returns null on success, or an error message on failure. Cancelling <paramref name="ct"/>
    /// stops playback immediately and throws <see cref="OperationCanceledException"/>.
    /// </summary>
    public async Task<string?> SpeakAsync(string text, CancellationToken ct = default)
    {
        var speechConfig = SpeechConfig.FromSubscription(apiKey, region);
        speechConfig.SpeechSynthesisVoiceName = voiceName;

        using var synthesizer = new SpeechSynthesizer(speechConfig);
        // StopSpeakingAsync interrupts the playback under way inside SpeakTextAsync.
        await using var registration = ct.Register(() => _ = synthesizer.StopSpeakingAsync());

        var result = await synthesizer.SpeakTextAsync(MarkdownText.StripFormatting(text));

        ct.ThrowIfCancellationRequested();

        return result.Reason == ResultReason.SynthesizingAudioCompleted
            ? null
            : $"Sintesi vocale non riuscita: {SpeechSynthesisCancellationDetails.FromResult(result).ErrorDetails}";
    }
}
