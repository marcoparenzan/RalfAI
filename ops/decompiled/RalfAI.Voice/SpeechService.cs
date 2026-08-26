using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;

namespace RalfAI.Voice;

public sealed class SpeechService(string region, string apiKey, string recognitionLanguage, string voiceName)
{
	public static SpeechService? FromConfig(IConfiguration config)
	{
		string value = config["AzureSpeech:Region"];
		string value2 = config["AzureSpeech:ApiKey"];
		if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(value2))
		{
			return null;
		}
		string text = config["AzureSpeech:RecognitionLanguage"] ?? "it-IT";
		string text2 = config["AzureSpeech:VoiceName"] ?? "it-IT-IsabellaNeural";
		return new SpeechService(value, value2, text, text2);
	}

	public async Task<(string? Text, string? Error)> RecognizeOnceAsync()
	{
		SpeechConfig speechConfig = SpeechConfig.FromSubscription(apiKey, region);
		speechConfig.SpeechRecognitionLanguage = recognitionLanguage;
		using AudioConfig audioConfig = AudioConfig.FromDefaultMicrophoneInput();
		using SpeechRecognizer recognizer = new SpeechRecognizer(speechConfig, audioConfig);
		SpeechRecognitionResult result = await recognizer.RecognizeOnceAsync();
		ResultReason reason = result.Reason;
		if (1 == 0)
		{
		}
		(string, string) result2 = reason switch
		{
			ResultReason.RecognizedSpeech => (result.Text, null), 
			ResultReason.NoMatch => (null, "Nessun parlato riconosciuto."), 
			ResultReason.Canceled => (null, "Riconoscimento annullato: " + CancellationDetails.FromResult(result).ErrorDetails), 
			_ => (null, "Riconoscimento non riuscito."), 
		};
		if (1 == 0)
		{
		}
		return result2;
	}

	public async Task<string?> SpeakAsync(string text, CancellationToken ct = default(CancellationToken))
	{
		SpeechConfig speechConfig = SpeechConfig.FromSubscription(apiKey, region);
		speechConfig.SpeechSynthesisVoiceName = voiceName;
		SpeechSynthesizer synthesizer = new SpeechSynthesizer(speechConfig);
		try
		{
			string result2;
			await using (ct.Register(delegate
			{
				synthesizer.StopSpeakingAsync();
			}))
			{
				SpeechSynthesisResult result = await synthesizer.SpeakTextAsync(MarkdownText.StripFormatting(text));
				ct.ThrowIfCancellationRequested();
				result2 = ((result.Reason == ResultReason.SynthesizingAudioCompleted) ? null : ("Sintesi vocale non riuscita: " + SpeechSynthesisCancellationDetails.FromResult(result).ErrorDetails));
			}
			return result2;
		}
		finally
		{
			if (synthesizer != null)
			{
				((IDisposable)synthesizer).Dispose();
			}
		}
	}
}
