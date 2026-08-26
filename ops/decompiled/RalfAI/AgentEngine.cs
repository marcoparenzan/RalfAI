using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Anthropic;
using Anthropic.Exceptions;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using RalfAI.Contexts;
using RalfAI.ImageGeneration;
using RalfAI.Plugins;
using RalfAI.Voice;

namespace RalfAI;

public sealed class AgentEngine
{
	private readonly AIAgent _agent;

	private readonly AgentSession _agentSession;

	private readonly List<ChatMessage> _pendingResumeMessages;

	private readonly List<AgentMessage> _messages = new List<AgentMessage>();

	private readonly ConversationLogger _logger;

	private readonly RateLimiter _rateLimiter;

	private readonly IConfiguration _configuration;

	private readonly string _provider;

	public IAgentContext Context { get; }

	public string ModelId { get; }

	public string TargetFolder { get; }

	public string LogPath => _logger.LogPath;

	public SpeechService? Speech { get; }

	public string? LastFinalText { get; private set; }

	public int ResumedMessageCount { get; }

	public IReadOnlyList<AgentMessage> Messages => _messages;

	public event Action<ToolCallStart>? ToolCallStarted;

	public event Action<ToolCallResult>? ToolCallCompleted;

	public event Action<AgentNotice>? Notice;

	public event Action<TurnProgress>? Progress;

	private AgentEngine(AIAgent agent, AgentSession agentSession, string modelId, IAgentContext context, string targetFolder, ConversationLogger logger, RateLimiter rateLimiter, SpeechService? speech, IConfiguration configuration, string provider, List<ChatMessage> pendingResumeMessages, List<AgentMessage> resumedMessages)
	{
		_agent = agent;
		_agentSession = agentSession;
		ModelId = modelId;
		Context = context;
		TargetFolder = targetFolder;
		_logger = logger;
		_rateLimiter = rateLimiter;
		_configuration = configuration;
		_provider = provider;
		Speech = speech;
		_pendingResumeMessages = pendingResumeMessages;
		_messages = resumedMessages;
		ResumedMessageCount = resumedMessages.Count;
	}

	public static async Task<AgentEngine> CreateAsync(IConfiguration config, string targetFolder, string contextKey, IAgentContext context, CancellationToken ct = default(CancellationToken))
	{
		ConversationLogger logger = new ConversationLogger(targetFolder);
		int rpm = (int.TryParse(config["AzureAIFoundry:RateLimits:RequestsPerMinute"], out var r) ? r : 20);
		int tpm = (int.TryParse(config["AzureAIFoundry:RateLimits:TokensPerMinute"], out var t) ? t : 20000);
		double safety = (double.TryParse(config["AzureAIFoundry:RateLimits:SafetyFactor"], NumberStyles.Float, CultureInfo.InvariantCulture, out var s) ? s : 1.25);
		Action<AgentNotice> noticeHandler = null;
		RateLimiter rateLimiter = new RateLimiter(rpm, tpm, safety, delegate(AgentNotice n)
		{
			noticeHandler?.Invoke(n);
		});
		SpeechService speech = SpeechService.FromConfig(config);
		ImageGenerationService imageGen = ImageGenerationService.FromConfig(config);
		string provider = (config["AIProvider"] ?? "foundry").Trim().ToLowerInvariant();
		(IChatClient, string) tuple = BuildChatClient(config, contextKey);
		IChatClient chatClient = tuple.Item1;
		string modelId = tuple.Item2;
		ImagePlugin image = new ImagePlugin(targetFolder);
		List<AITool> tools = new List<AITool>(context.GetTools(targetFolder))
		{
			AIFunctionFactory.Create(new Func<string, Task<string>>(new AIPlugin(chatClient).Ask), "AI_Ask"),
			AIFunctionFactory.Create(new Func<string, Task<string>>(image.GetInfo), "Image_GetInfo"),
			AIFunctionFactory.Create(new _003C_003Ef__AnonymousDelegate0<string, string, int, int, bool, Task<string>>(image.Resize), "Image_Resize"),
			AIFunctionFactory.Create(new Func<string, string, int, int, int, int, Task<string>>(image.Crop), "Image_Crop"),
			AIFunctionFactory.Create(new Func<string, string, float, Task<string>>(image.Rotate), "Image_Rotate"),
			AIFunctionFactory.Create(new Func<string, string, bool, Task<string>>(image.Flip), "Image_Flip"),
			AIFunctionFactory.Create(new Func<string, string, Task<string>>(image.Grayscale), "Image_Grayscale"),
			AIFunctionFactory.Create(new Func<string, string, Task<string>>(image.ConvertFormat), "Image_ConvertFormat")
		};
		if (speech != null)
		{
			SpeechPlugin speechPlugin = new SpeechPlugin(speech);
			tools.Add(AIFunctionFactory.Create(new _003C_003Ef__AnonymousDelegate1<string, CancellationToken, Task<string>>(speechPlugin.Speak), "Speech_Speak"));
			tools.Add(AIFunctionFactory.Create(new Func<Task<string>>(speechPlugin.Listen), "Speech_Listen"));
		}
		if (imageGen != null)
		{
			ImageGenerationPlugin imageGenPlugin = new ImageGenerationPlugin(imageGen, targetFolder);
			tools.Add(AIFunctionFactory.Create(new _003C_003Ef__AnonymousDelegate2<string, string, string, Task<string>>(imageGenPlugin.GenerateAI), "Image_GenerateAI"));
		}
		string systemPrompt = context.BuildSystemPrompt(targetFolder) + BuildUniversalToolsPrompt(speech != null, imageGen != null);
		Action<ToolCallStart> startHandler = null;
		Action<ToolCallResult> completedHandler = null;
		AIAgent agent = chatClient.AsAIAgent(systemPrompt, "RalfAI", null, tools).AsBuilder().Use(async delegate(AIAgent innerAgent, FunctionInvocationContext fctx, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken fct)
		{
			string displayName = FriendlyToolName(fctx.Function.Name);
			string rawDescription = fctx.Function.Description;
			string description = (string.IsNullOrWhiteSpace(rawDescription) ? displayName : ((rawDescription.Length > 80) ? (rawDescription.Substring(0, 80).TrimEnd() + "…") : rawDescription));
			List<(string Key, string Value)> argPairs = (from kv in fctx.Arguments
				where kv.Value != null
				select (Key: kv.Key, Value: kv.Value.ToString() ?? "")).ToList();
			string argPreview = string.Join(", ", argPairs.Select(delegate((string Key, string Value) a)
			{
				string text = ((a.Value.Length > 40) ? (a.Value.Substring(0, 40).Replace("\n", "↵") + "…") : a.Value.Replace("\n", "↵"));
				return a.Key + "=" + text;
			}));
			string argsForLog = string.Join(", ", argPairs.Select(((string Key, string Value) a) => a.Key + "=" + a.Value));
			startHandler?.Invoke(new ToolCallStart(displayName, description, argPreview, argsForLog));
			object result = await next(fctx, fct);
			string resultText = result?.ToString() ?? "";
			completedHandler?.Invoke(new ToolCallResult(displayName, argsForLog, resultText));
			logger.LogTool(displayName, argsForLog, resultText);
			return result;
		})
			.Build();
		AgentSession agentSession = await agent.CreateSessionAsync().ConfigureAwait(continueOnCapturedContext: false);
		List<(string Role, string Content)> existing = logger.LoadExistingHistory();
		AgentEngine engine = new AgentEngine(pendingResumeMessages: existing.Select<(string, string), ChatMessage>(((string Role, string Content) e) => new ChatMessage((e.Role == "user") ? ChatRole.User : ChatRole.Assistant, e.Content)).ToList(), resumedMessages: existing.Select<(string, string), AgentMessage>(((string Role, string Content) e) => new AgentMessage(e.Role, e.Content)).ToList(), agent: agent, agentSession: agentSession, modelId: modelId, context: context, targetFolder: targetFolder, logger: logger, rateLimiter: rateLimiter, speech: speech, configuration: config, provider: provider);
		startHandler = delegate(ToolCallStart call)
		{
			engine.ToolCallStarted?.Invoke(call);
		};
		completedHandler = delegate(ToolCallResult call)
		{
			engine.ToolCallCompleted?.Invoke(call);
		};
		noticeHandler = delegate(AgentNotice notice)
		{
			engine.Notice?.Invoke(notice);
		};
		return engine;
	}

	public async Task<AgentTurnResult> SendAsync(string userText, CancellationToken ct = default(CancellationToken))
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		Progress?.Invoke(new TurnProgress("prepare", "Preparing request for " + _provider + "/" + ModelId, stopwatch.Elapsed));
		_logger.LogUser(userText);
		_messages.Add(new AgentMessage("user", userText));
		for (int attempt = 0; attempt <= 3; attempt++)
		{
			try
			{
				Progress?.Invoke(new TurnProgress("rate-limit", "Checking request and token limits", stopwatch.Elapsed));
				await _rateLimiter.ThrottleAsync(EstimateTokens(_messages), ct);
				Progress?.Invoke(new TurnProgress("model", "Waiting for the model; tool activity will appear separately", stopwatch.Elapsed));
				AgentResponse response;
				if (_pendingResumeMessages.Count > 0)
				{
					List<ChatMessage> seeded = new List<ChatMessage>(_pendingResumeMessages)
					{
						new ChatMessage(ChatRole.User, userText)
					};
					response = await _agent.RunAsync(seeded, _agentSession, null, ct);
					_pendingResumeMessages.Clear();
				}
				else
				{
					response = await _agent.RunAsync(userText, _agentSession, null, ct);
				}
				Progress?.Invoke(new TurnProgress("response", "Model response received; extracting answer and usage", stopwatch.Elapsed));
				string raw = response.Text ?? "(no response)";
				(string, string) tuple = ExtractThinking(raw);
				string thinking = tuple.Item1;
				string finalText = tuple.Item2;
				string displayText = (string.IsNullOrWhiteSpace(finalText) ? "(no final answer yet)" : finalText);
				string historyText = (string.IsNullOrWhiteSpace(finalText) ? raw : finalText);
				_messages.Add(new AgentMessage("assistant", historyText));
				LastFinalText = (string.IsNullOrWhiteSpace(finalText) ? null : finalText);
				_logger.LogAssistant(thinking, displayText);
				stopwatch.Stop();
				TurnUsage usage = TurnMetrics.Read(response, _configuration, _provider, ModelId);
				Progress?.Invoke(new TurnProgress("complete", "Turn completed and saved", stopwatch.Elapsed));
				return new AgentTurnResult(thinking, displayText, null, usage, stopwatch.Elapsed);
			}
			catch (OperationCanceledException)
			{
				int num;
				if (_messages.Count > 0)
				{
					List<AgentMessage> messages = _messages;
					num = ((messages[messages.Count - 1].Role == "user") ? 1 : 0);
				}
				else
				{
					num = 0;
				}
				if (num != 0)
				{
					_messages.RemoveAt(_messages.Count - 1);
				}
				throw;
			}
			catch (Exception ex2) when (attempt < 3 && Is429(ex2))
			{
				await _rateLimiter.RetryDelayAsync(ExtractRetryAfter(ex2), ct);
			}
			catch (Exception ex3)
			{
				stopwatch.Stop();
				return new AgentTurnResult(null, null, ex3.Message, null, stopwatch.Elapsed);
			}
		}
		return new AgentTurnResult(null, null, "Rate limit retry budget exceeded (HTTP 429).");
	}

	private static (string? Thinking, string Final) ExtractThinking(string raw)
	{
		int num = raw.IndexOf("<think>", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return (Thinking: null, Final: raw.Trim());
		}
		int num2 = raw.IndexOf("</think>", num, StringComparison.OrdinalIgnoreCase);
		if (num2 < 0)
		{
			string item = raw.Substring(num + "<think>".Length).Trim();
			return (Thinking: item, Final: "");
		}
		int num3 = num + "<think>".Length;
		string text = raw.Substring(num3, num2 - num3).Trim();
		string item2 = (raw.Substring(0, num) + raw.Substring(num2 + "</think>".Length)).Trim();
		return (Thinking: string.IsNullOrWhiteSpace(text) ? null : text, Final: item2);
	}

	private static int EstimateTokens(IEnumerable<AgentMessage> messages)
	{
		return messages.Sum((AgentMessage m) => m.Content.Length / 4 + 5);
	}

	private static bool Is429(Exception ex)
	{
		if (ex is ClientResultException { Status: 429 })
		{
			return true;
		}
		if (ex is AnthropicRateLimitException)
		{
			return true;
		}
		if (ex.Message.Contains("429", StringComparison.Ordinal) || ex.Message.Contains("TooManyRequests", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("rate_limit", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return ex.InnerException != null && Is429(ex.InnerException);
	}

	private static TimeSpan? ExtractRetryAfter(Exception ex)
	{
		Match match = Regex.Match(ex.Message, "retry.after[:\\s]+(\\d+)", RegexOptions.IgnoreCase);
		int result;
		return (match.Success && int.TryParse(match.Groups[1].Value, out result)) ? new TimeSpan?(TimeSpan.FromSeconds(result)) : ((TimeSpan?)null);
	}

	private static string BuildUniversalToolsPrompt(bool hasSpeech, bool hasImageGen)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("## Engine Tools (always available, independent of context)");
		stringBuilder.AppendLine("- AI.Ask — delegate an isolated sub-question or sub-task to the same model (e.g. summarize a long file) without adding it to the main conversation history.");
		stringBuilder.AppendLine("- Image.GetInfo — read a JPG/PNG image's dimensions, format and file size.");
		stringBuilder.AppendLine("- Image.Resize — resize a JPG/PNG image (optionally keeping aspect ratio).");
		stringBuilder.AppendLine("- Image.Crop — crop a rectangular region out of a JPG/PNG image.");
		stringBuilder.AppendLine("- Image.Rotate — rotate a JPG/PNG image by an arbitrary angle.");
		stringBuilder.AppendLine("- Image.Flip — mirror a JPG/PNG image horizontally or vertically.");
		stringBuilder.AppendLine("- Image.Grayscale — convert a JPG/PNG image to grayscale.");
		stringBuilder.AppendLine("- Image.ConvertFormat — convert a JPG/PNG image between .jpg and .png.");
		if (hasImageGen)
		{
			stringBuilder.AppendLine("- Image.GenerateAI — generate a new image (e.g. a logo or icon) from a text prompt and save it as JPG/PNG.");
		}
		if (hasSpeech)
		{
			stringBuilder.AppendLine("- Speech.Speak — speak text aloud through the configured voice.");
			stringBuilder.AppendLine("- Speech.Listen — listen on the microphone for one utterance and return the recognized text.");
		}
		return stringBuilder.ToString();
	}

	private static string FriendlyToolName(string name)
	{
		int num = name.IndexOf('_');
		return (num > 0) ? string.Concat(name.AsSpan(0, num), ".".AsSpan(), name.AsSpan(num + 1)) : name;
	}

	private static (IChatClient ChatClient, string ModelId) BuildChatClient(IConfiguration config, string contextKey)
	{
		string text = (config["AIProvider"] ?? "foundry").Trim().ToLowerInvariant();
		if (1 == 0)
		{
		}
		(IChatClient, string) result = ((text == "openai") ? BuildOpenAI(config) : ((!(text == "anthropic")) ? BuildFoundry(config, contextKey) : BuildAnthropic(config)));
		if (1 == 0)
		{
		}
		return result;
	}

	private static (IChatClient, string) BuildOpenAI(IConfiguration config)
	{
		string apiKey = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey missing in appsettings.json or RALFAI_OpenAI__ApiKey env var");
		string text = config["OpenAI:ModelId"] ?? "gpt-5.1";
		OpenAIClient openAIClient = new OpenAIClient(apiKey);
		return (openAIClient.GetChatClient(text).AsIChatClient(), text);
	}

	private static (IChatClient, string) BuildAnthropic(IConfiguration config)
	{
		string apiKey = config["Anthropic:ApiKey"] ?? throw new InvalidOperationException("Anthropic:ApiKey missing in appsettings.json or RALFAI_Anthropic__ApiKey env var");
		string text = config["Anthropic:ModelId"] ?? "claude-sonnet-4-5";
		AnthropicClient client = new AnthropicClient
		{
			ApiKey = apiKey
		};
		return (client.AsIChatClient(text), text);
	}

	private static (IChatClient, string) BuildFoundry(IConfiguration config, string contextKey)
	{
		string text = config["AzureAIFoundry:Endpoint"] ?? throw new InvalidOperationException("AzureAIFoundry:Endpoint missing in appsettings.json or RALFAI_AzureAIFoundry__Endpoint env var");
		string key = config["AzureAIFoundry:ApiKey"] ?? throw new InvalidOperationException("AzureAIFoundry:ApiKey missing in appsettings.json or RALFAI_AzureAIFoundry__ApiKey env var");
		string text2 = config["AzureAIFoundry:ContextModels:" + contextKey] ?? config["AzureAIFoundry:DefaultModel"];
		string text3 = config["AzureAIFoundry:ModelId"] ?? "deepseek-r1";
		if (text2 != null)
		{
			IConfigurationSection section = config.GetSection("AzureAIFoundry:Models:" + text2);
			if (section.Exists())
			{
				text3 = section["ModelId"] ?? text2;
			}
		}
		string text4 = text.TrimEnd('/');
		if (text4.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
		{
			string text5 = text4;
			int length = "/chat/completions".Length;
			text4 = text5.Substring(0, text5.Length - length).TrimEnd('/');
		}
		string leftPart = new Uri(text4).GetLeftPart(UriPartial.Authority);
		AzureOpenAIClient azureOpenAIClient = new AzureOpenAIClient(new Uri(leftPart), new AzureKeyCredential(key));
		return (azureOpenAIClient.GetChatClient(text3).AsIChatClient(), text3);
	}
}
