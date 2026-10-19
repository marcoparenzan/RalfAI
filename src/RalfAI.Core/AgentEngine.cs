using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using RalfAI.Contexts;
using RalfAI.Plugins;
using RalfAI.Providers;
using RalfAI.Voice;

namespace RalfAI;

public sealed class AgentEngine
{
    private readonly AIAgent _agent;
    private readonly AgentSession _agentSession;
    private readonly List<ChatMessage> _pendingResumeMessages;
    private readonly List<AgentMessage> _messages;
    private readonly ConversationLogger _logger;
    private readonly RateLimiter _rateLimiter;
    private readonly IConfiguration _configuration;
    private readonly string _provider;
    private readonly CostHistoryStore _costHistory;

    public IAgentContext Context { get; }
    public string ModelId { get; }
    public string TargetFolder { get; }
    public string LogPath => _logger.LogPath;
    public string CostHistoryPath => _costHistory.Path;
    public CostSummary GetCostSummary() => _costHistory.Summarize();
    public SpeechService? Speech { get; }
    public string? LastFinalText { get; private set; }
    public int ResumedMessageCount { get; }
    public IReadOnlyList<AgentMessage> Messages => _messages;
    public event Action<ToolCallStart>? ToolCallStarted;
    public event Action<ToolCallResult>? ToolCallCompleted;
    public event Action<AgentNotice>? Notice;
    public event Action<TurnProgress>? Progress;
    /// <summary>Raised for each fragment of the answer or the provider-exposed reasoning as it streams in.</summary>
    public event Action<StreamChunk>? Streamed;
    /// <summary>Raised a few times per second while generating, and once more when the turn ends.</summary>
    public event Action<TokenRate>? RateChanged;

    private long _sessionOutputTokens;
    private double _sessionGeneratingSeconds;
    private bool _requestReasoningSummary;

    /// <summary>Average output tokens/second over the completed turns of this session (0 until one completes).</summary>
    public double SessionTokensPerSecond => _sessionGeneratingSeconds > 0 ? _sessionOutputTokens / _sessionGeneratingSeconds : 0;

    private AgentEngine(AIAgent agent, AgentSession session, string modelId, IAgentContext context, string targetFolder,
        ConversationLogger logger, RateLimiter limiter, SpeechService? speech, IConfiguration configuration,
        string provider, List<ChatMessage> pending, List<AgentMessage> messages)
    {
        _agent = agent; _agentSession = session; ModelId = modelId; Context = context; TargetFolder = targetFolder;
        _logger = logger; _rateLimiter = limiter; Speech = speech; _configuration = configuration; _provider = provider;
        _pendingResumeMessages = pending; _messages = messages; ResumedMessageCount = messages.Count;
        _costHistory = new CostHistoryStore(targetFolder);
        // Ask the provider to expose a reasoning summary; Reasoning:Summary = "off" disables it.
        _requestReasoningSummary = !string.Equals(configuration["Reasoning:Summary"], "off", StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<AgentEngine> CreateAsync(IConfiguration config, string targetFolder, string contextKey,
        IAgentContext context, CancellationToken ct = default)
    {
        var logger = new ConversationLogger(targetFolder);
        var rpm = int.TryParse(config["AzureAIFoundry:RateLimits:RequestsPerMinute"], out var r) ? r : 20;
        var tpm = int.TryParse(config["AzureAIFoundry:RateLimits:TokensPerMinute"], out var t) ? t : 20000;
        var safety = double.TryParse(config["AzureAIFoundry:RateLimits:SafetyFactor"], System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var s) ? s : 1.25;
        Action<AgentNotice>? noticeHandler = null;
        var limiter = new RateLimiter(rpm, tpm, safety, n => noticeHandler?.Invoke(n));
        var speech = SpeechService.FromConfig(config);
        var imageGen = ImageGeneration.ImageGenerationService.FromConfig(config);
        var provider = (config["AIProvider"] ?? "foundry").Trim().ToLowerInvariant();
        var (chatClient, modelId) = ChatClientFactory.Create(config, contextKey);
        var image = new ImagePlugin(targetFolder);
        var tools = new List<AITool>(context.GetTools(targetFolder))
        {
            AIFunctionFactory.Create(new AIPlugin(chatClient).Ask, name: "AI_Ask"),
            AIFunctionFactory.Create(image.GetInfo, name: "Image_GetInfo"),
            AIFunctionFactory.Create(image.Resize, name: "Image_Resize"),
            AIFunctionFactory.Create(image.Crop, name: "Image_Crop"),
            AIFunctionFactory.Create(image.Rotate, name: "Image_Rotate"),
            AIFunctionFactory.Create(image.Flip, name: "Image_Flip"),
            AIFunctionFactory.Create(image.Grayscale, name: "Image_Grayscale"),
            AIFunctionFactory.Create(image.ConvertFormat, name: "Image_ConvertFormat")
        };
        if (speech is not null)
        {
            var plugin = new SpeechPlugin(speech);
            tools.Add(AIFunctionFactory.Create(plugin.Speak, name: "Speech_Speak"));
            tools.Add(AIFunctionFactory.Create(plugin.Listen, name: "Speech_Listen"));
        }
        if (imageGen is not null)
        {
            var plugin = new ImageGenerationPlugin(imageGen, targetFolder);
            tools.Add(AIFunctionFactory.Create(plugin.GenerateAI, name: "Image_GenerateAI"));
        }

        Action<ToolCallStart>? started = null;
        Action<ToolCallResult>? completed = null;
        var agent = chatClient.AsAIAgent(context.BuildSystemPrompt(targetFolder) + UniversalPrompt(speech is not null, imageGen is not null), "RalfAI", tools: tools)
            .AsBuilder().Use(async (AIAgent _, FunctionInvocationContext fctx,
                Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken token) =>
            {
                var name = FriendlyToolName(fctx.Function.Name);
                var description = string.IsNullOrWhiteSpace(fctx.Function.Description) ? name : fctx.Function.Description;
                var args = string.Join(", ", fctx.Arguments.Where(x => x.Value is not null).Select(x => $"{x.Key}={x.Value}"));
                var preview = args.Length > 120 ? args[..120] + "…" : args;
                started?.Invoke(new ToolCallStart(name, description, preview, args));
                var result = await next(fctx, token);
                var resultText = result?.ToString() ?? "";
                completed?.Invoke(new ToolCallResult(name, args, resultText));
                logger.LogTool(name, args, resultText);
                return result;
            }).Build();
        var session = await agent.CreateSessionAsync().ConfigureAwait(false);
        var history = logger.LoadExistingHistory();
        var engine = new AgentEngine(agent, session, modelId, context, targetFolder, logger, limiter, speech, config, provider,
            history.Select(x => new ChatMessage(x.Role == "user" ? ChatRole.User : ChatRole.Assistant, x.Content)).ToList(),
            history.Select(x => new AgentMessage(x.Role, x.Content)).ToList());
        started = x => engine.ToolCallStarted?.Invoke(x);
        completed = x => engine.ToolCallCompleted?.Invoke(x);
        noticeHandler = x => engine.Notice?.Invoke(x);
        return engine;
    }

    public async Task<AgentTurnResult> SendAsync(string userText, CancellationToken ct = default)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        Progress?.Invoke(new("prepare", $"Preparing request for {_provider}/{ModelId}", timer.Elapsed));
        _logger.LogUser(userText); _messages.Add(new("user", userText));
        for (var attempt = 0; attempt <= 3; attempt++)
        {
            try
            {
                Progress?.Invoke(new("rate-limit", "Checking request and token limits", timer.Elapsed));
                await _rateLimiter.ThrottleAsync(_messages.Sum(m => m.Content.Length / 4 + 5), ct);
                Progress?.Invoke(new("model", "Waiting for model response", timer.Elapsed));
                var turn = new StreamingTurn();
                var options = _requestReasoningSummary
                    ? new ChatClientAgentRunOptions(new ChatOptions { Reasoning = new ReasoningOptions { Output = ReasoningOutput.Summary } })
                    : null;
                var resumed = _pendingResumeMessages.Count > 0;
                var stream = resumed
                    ? _agent.RunStreamingAsync(new List<ChatMessage>(_pendingResumeMessages) { new(ChatRole.User, userText) }, _agentSession, options, ct)
                    : _agent.RunStreamingAsync(userText, _agentSession, options, ct);

                var lastRate = TimeSpan.Zero;
                await foreach (var update in stream.WithCancellation(ct))
                {
                    if (update.Contents.OfType<TextContent>().Any(t => !string.IsNullOrEmpty(t.Text)))
                        turn.BeginMessage(update.MessageId, chunk => Streamed?.Invoke(chunk));
                    foreach (var content in update.Contents)
                        turn.Add(content, chunk => Streamed?.Invoke(chunk));

                    if (timer.Elapsed - lastRate >= TimeSpan.FromMilliseconds(250))
                    {
                        lastRate = timer.Elapsed;
                        RateChanged?.Invoke(turn.Snapshot(SessionTokensPerSecond));
                    }
                }
                turn.Flush(chunk => Streamed?.Invoke(chunk));
                if (resumed) _pendingResumeMessages.Clear();

                Progress?.Invoke(new("response", "Response received; extracting answer and usage", timer.Elapsed));
                var raw = turn.RawText;
                var (tagThinking, final) = ExtractThinking(raw);
                var summary = turn.ReasoningSummary;
                var thinking = tagThinking ?? (summary.Length > 0 ? summary : null);
                var (turnTokens, turnSeconds) = turn.Totals();
                _sessionOutputTokens += turnTokens;
                _sessionGeneratingSeconds += turnSeconds;
                RateChanged?.Invoke(turn.Snapshot(SessionTokensPerSecond));
                var display = string.IsNullOrWhiteSpace(final) ? "(no final answer yet)" : final;
                _messages.Add(new("assistant", string.IsNullOrWhiteSpace(final) ? raw : final));
                LastFinalText = string.IsNullOrWhiteSpace(final) ? null : final;
                _logger.LogAssistant(thinking, display);
                timer.Stop();
                var usage = TurnMetrics.FromDetails(turn.Usage, _configuration, _provider, ModelId);
                await _costHistory.AppendAsync(new(DateTimeOffset.UtcNow, _provider, ModelId, usage?.InputTokens,
                    usage?.OutputTokens, usage?.TotalTokens, usage?.EstimatedCostUsd, timer.Elapsed.TotalSeconds), ct);
                Progress?.Invoke(new("complete", $"Turn saved; usage history → {_costHistory.Path}", timer.Elapsed));
                return new(thinking, display, null, usage, timer.Elapsed, turnSeconds > 0 ? turnTokens / turnSeconds : null);
            }
            catch (OperationCanceledException) { if (_messages.LastOrDefault()?.Role == "user") _messages.RemoveAt(_messages.Count - 1); throw; }
            // Not every deployment accepts a reasoning summary request: drop it for the session and retry.
            catch (Exception ex) when (_requestReasoningSummary && IsReasoningOptionError(ex))
            {
                _requestReasoningSummary = false;
                Notice?.Invoke(new("Reasoning summary not supported by this model; continuing without it.", NoticeLevel.Warning));
                attempt--;
            }
            catch (Exception ex) when (attempt < 3 && Is429(ex)) { await _rateLimiter.RetryDelayAsync(null, ct); }
            catch (Exception ex) { timer.Stop(); return new(null, null, ex.Message, Duration: timer.Elapsed); }
        }
        return new(null, null, "Rate limit retry budget exceeded (HTTP 429).", Duration: timer.Elapsed);
    }

    private static (string? Thinking, string Final) ExtractThinking(string raw)
    {
        var open = raw.IndexOf("<think>", StringComparison.OrdinalIgnoreCase);
        if (open < 0) return (null, raw.Trim());
        var close = raw.IndexOf("</think>", open, StringComparison.OrdinalIgnoreCase);
        if (close < 0) return (raw[(open + 7)..].Trim(), "");
        var thought = raw[(open + 7)..close].Trim();
        return (string.IsNullOrWhiteSpace(thought) ? null : thought, (raw[..open] + raw[(close + 8)..]).Trim());
    }

    private static bool IsReasoningOptionError(Exception ex) =>
        (ex.Message.Contains("reasoning", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("summary", StringComparison.OrdinalIgnoreCase))
        && (ex.Message.Contains("400", StringComparison.Ordinal) || ex.Message.Contains("unsupported", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("not supported", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase));

    private static bool Is429(Exception ex) => ex.Message.Contains("429", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) || (ex.InnerException is not null && Is429(ex.InnerException));
    private static string FriendlyToolName(string name) { var i = name.IndexOf('_'); return i > 0 ? name[..i] + "." + name[(i + 1)..] : name; }
    private static string UniversalPrompt(bool speech, bool imageGen) => "\n\n## Engine tools\nAI.Ask and JPG/PNG manipulation are available."
        + (imageGen ? " Image.GenerateAI is available." : "") + (speech ? " Speech.Speak and Speech.Listen are available." : "");
}
