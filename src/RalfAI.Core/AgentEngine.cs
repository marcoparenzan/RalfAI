using Anthropic;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using RalfAI.Contexts;
using RalfAI.Plugins;
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

    private AgentEngine(AIAgent agent, AgentSession session, string modelId, IAgentContext context, string targetFolder,
        ConversationLogger logger, RateLimiter limiter, SpeechService? speech, IConfiguration configuration,
        string provider, List<ChatMessage> pending, List<AgentMessage> messages)
    {
        _agent = agent; _agentSession = session; ModelId = modelId; Context = context; TargetFolder = targetFolder;
        _logger = logger; _rateLimiter = limiter; Speech = speech; _configuration = configuration; _provider = provider;
        _pendingResumeMessages = pending; _messages = messages; ResumedMessageCount = messages.Count;
        _costHistory = new CostHistoryStore(targetFolder);
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
        var (chatClient, modelId) = BuildChatClient(config, contextKey);
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
                AgentResponse response;
                if (_pendingResumeMessages.Count > 0)
                {
                    var seeded = new List<ChatMessage>(_pendingResumeMessages) { new(ChatRole.User, userText) };
                    response = await _agent.RunAsync(seeded, _agentSession, cancellationToken: ct);
                    _pendingResumeMessages.Clear();
                }
                else response = await _agent.RunAsync(userText, _agentSession, cancellationToken: ct);
                Progress?.Invoke(new("response", "Response received; extracting answer and usage", timer.Elapsed));
                var raw = response.Text ?? "(no response)";
                var (thinking, final) = ExtractThinking(raw);
                var display = string.IsNullOrWhiteSpace(final) ? "(no final answer yet)" : final;
                _messages.Add(new("assistant", string.IsNullOrWhiteSpace(final) ? raw : final));
                LastFinalText = string.IsNullOrWhiteSpace(final) ? null : final;
                _logger.LogAssistant(thinking, display);
                timer.Stop();
                var usage = TurnMetrics.Read(response, _configuration, _provider, ModelId);
                await _costHistory.AppendAsync(new(DateTimeOffset.UtcNow, _provider, ModelId, usage?.InputTokens,
                    usage?.OutputTokens, usage?.TotalTokens, usage?.EstimatedCostUsd, timer.Elapsed.TotalSeconds), ct);
                Progress?.Invoke(new("complete", $"Turn saved; usage history → {_costHistory.Path}", timer.Elapsed));
                return new(thinking, display, null, usage, timer.Elapsed);
            }
            catch (OperationCanceledException) { if (_messages.LastOrDefault()?.Role == "user") _messages.RemoveAt(_messages.Count - 1); throw; }
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

    private static bool Is429(Exception ex) => ex.Message.Contains("429", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) || (ex.InnerException is not null && Is429(ex.InnerException));
    private static string FriendlyToolName(string name) { var i = name.IndexOf('_'); return i > 0 ? name[..i] + "." + name[(i + 1)..] : name; }
    private static string UniversalPrompt(bool speech, bool imageGen) => "\n\n## Engine tools\nAI.Ask and JPG/PNG manipulation are available."
        + (imageGen ? " Image.GenerateAI is available." : "") + (speech ? " Speech.Speak and Speech.Listen are available." : "");

    private static (IChatClient, string) BuildChatClient(IConfiguration config, string contextKey) =>
        (config["AIProvider"] ?? "foundry").Trim().ToLowerInvariant() switch
        { "openai" => BuildOpenAI(config), "anthropic" => BuildAnthropic(config), _ => BuildFoundry(config, contextKey) };
    private static (IChatClient, string) BuildOpenAI(IConfiguration c) { var key = c["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey missing"); var model = c["OpenAI:ModelId"] ?? "gpt-5.1"; return (new OpenAIClient(key).GetChatClient(model).AsIChatClient(), model); }
    private static (IChatClient, string) BuildAnthropic(IConfiguration c) { var key = c["Anthropic:ApiKey"] ?? throw new InvalidOperationException("Anthropic:ApiKey missing"); var model = c["Anthropic:ModelId"] ?? "claude-sonnet-4-5"; return (new AnthropicClient { ApiKey = key }.AsIChatClient(model), model); }
    private static (IChatClient, string) BuildFoundry(IConfiguration c, string context)
    {
        var endpoint = c["AzureAIFoundry:Endpoint"] ?? throw new InvalidOperationException("AzureAIFoundry:Endpoint missing");
        var key = c["AzureAIFoundry:ApiKey"] ?? throw new InvalidOperationException("AzureAIFoundry:ApiKey missing");
        var named = c[$"AzureAIFoundry:ContextModels:{context}"] ?? c["AzureAIFoundry:DefaultModel"];
        var model = named is null ? c["AzureAIFoundry:ModelId"] ?? "deepseek-r1" : c[$"AzureAIFoundry:Models:{named}:ModelId"] ?? named;
        var host = new Uri(endpoint).GetLeftPart(UriPartial.Authority);
        return (new AzureOpenAIClient(new Uri(host), new AzureKeyCredential(key)).GetChatClient(model).AsIChatClient(), model);
    }
}
