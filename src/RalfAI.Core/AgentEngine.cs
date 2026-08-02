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

/// <summary>
/// The agent engine: Microsoft Agent Framework chat/tool-calling loop, rate limiting, logging,
/// optional voice — everything that isn't tied to a specific frontend (console, WinForms/Blazor
/// Hybrid, …). Frontends drive it via <see cref="SendAsync"/> and render by subscribing to
/// <see cref="ToolCallStarted"/>/<see cref="ToolCallCompleted"/> and reading <see cref="Messages"/>.
///
/// Provider-agnostic by design: which AI backend to talk to (Azure AI Foundry, native OpenAI, or
/// Anthropic Claude) is picked from config via <see cref="BuildChatClient"/>, each using that
/// provider's own official .NET SDK behind the shared <see cref="IChatClient"/> abstraction —
/// swapping providers doesn't touch anything else in this class.
/// </summary>
public sealed class AgentEngine
{
    private readonly AIAgent _agent;
    private readonly AgentSession _agentSession;
    private readonly List<ChatMessage> _pendingResumeMessages;
    private readonly List<AgentMessage> _messages = [];
    private readonly ConversationLogger _logger;
    private readonly RateLimiter _rateLimiter;

    public IAgentContext Context { get; }
    public string ModelId { get; }
    public string TargetFolder { get; }
    public string LogPath => _logger.LogPath;
    public SpeechService? Speech { get; }
    public string? LastFinalText { get; private set; }
    public int ResumedMessageCount { get; }
    public IReadOnlyList<AgentMessage> Messages => _messages;

    /// <summary>Raised just before a tool call runs (name + preview args).</summary>
    public event Action<ToolCallStart>? ToolCallStarted;

    /// <summary>Raised right after a tool call returns (full args + result).</summary>
    public event Action<ToolCallResult>? ToolCallCompleted;

    /// <summary>Pacing/TPM/429 notices from the rate limiter — purely informational.</summary>
    public event Action<AgentNotice>? Notice;

    private AgentEngine(
        AIAgent agent, AgentSession agentSession, string modelId, IAgentContext context, string targetFolder,
        ConversationLogger logger, RateLimiter rateLimiter, SpeechService? speech,
        List<ChatMessage> pendingResumeMessages, List<AgentMessage> resumedMessages)
    {
        _agent = agent;
        _agentSession = agentSession;
        ModelId = modelId;
        Context = context;
        TargetFolder = targetFolder;
        _logger = logger;
        _rateLimiter = rateLimiter;
        Speech = speech;
        _pendingResumeMessages = pendingResumeMessages;
        _messages = resumedMessages;
        ResumedMessageCount = resumedMessages.Count;
    }

    /// <summary>
    /// Builds the engine: resolves the configured AI provider, registers this context's tools
    /// plus the universal engine-level ones (AI.Ask, Speech.*), and replays any prior transcript
    /// for this folder. Async because creating the framework's <see cref="AgentSession"/> is.
    /// </summary>
    public static async Task<AgentEngine> CreateAsync(
        IConfiguration config, string targetFolder, string contextKey, IAgentContext context,
        CancellationToken ct = default)
    {
        var logger = new ConversationLogger(targetFolder);

        var rpm    = int.TryParse   (config["AzureAIFoundry:RateLimits:RequestsPerMinute"], out var r) ? r : 20;
        var tpm    = int.TryParse   (config["AzureAIFoundry:RateLimits:TokensPerMinute"],   out var t) ? t : 20000;
        var safety = double.TryParse(config["AzureAIFoundry:RateLimits:SafetyFactor"],
                         System.Globalization.NumberStyles.Float,
                         System.Globalization.CultureInfo.InvariantCulture,
                         out var s) ? s : 1.25;

        Action<AgentNotice>? noticeHandler = null;
        var rateLimiter = new RateLimiter(rpm, tpm, safety, n => noticeHandler?.Invoke(n));
        var speech = SpeechService.FromConfig(config);

        var (chatClient, modelId) = BuildChatClient(config, contextKey);

        var tools = new List<AITool>(context.GetTools(targetFolder))
        {
            AIFunctionFactory.Create(new AIPlugin(chatClient).Ask, name: "AI_Ask")
        };
        if (speech is not null)
        {
            var speechPlugin = new SpeechPlugin(speech);
            tools.Add(AIFunctionFactory.Create(speechPlugin.Speak, name: "Speech_Speak"));
            tools.Add(AIFunctionFactory.Create(speechPlugin.Listen, name: "Speech_Listen"));
        }

        var systemPrompt = context.BuildSystemPrompt(targetFolder) + BuildUniversalToolsPrompt(speech is not null);

        // ToolCallStarted/ToolCallCompleted/Notice are instance events, but the middleware and
        // rate limiter callback have to be wired before the instance exists — buffer through a
        // local Action that gets redirected to the real events once the engine is constructed.
        Action<ToolCallStart>? startHandler = null;
        Action<ToolCallResult>? completedHandler = null;

        var agent = chatClient
            .AsAIAgent(instructions: systemPrompt, name: "RalfAI", tools: tools)
            .AsBuilder()
                .Use(async (AIAgent innerAgent, FunctionInvocationContext fctx,
                            Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
                            CancellationToken fct) =>
                {
                    var displayName = FriendlyToolName(fctx.Function.Name);
                    var rawDescription = fctx.Function.Description;
                    var description = string.IsNullOrWhiteSpace(rawDescription)
                        ? displayName
                        : rawDescription.Length > 80 ? rawDescription[..80].TrimEnd() + "…" : rawDescription;

                    var argPairs = fctx.Arguments
                        .Where(kv => kv.Value is not null)
                        .Select(kv => (kv.Key, Value: kv.Value!.ToString() ?? ""))
                        .ToList();
                    var argPreview = string.Join(", ", argPairs.Select(a =>
                    {
                        var p = a.Value.Length > 40 ? a.Value[..40].Replace("\n", "↵") + "…" : a.Value.Replace("\n", "↵");
                        return $"{a.Key}={p}";
                    }));
                    var argsForLog = string.Join(", ", argPairs.Select(a => $"{a.Key}={a.Value}"));

                    startHandler?.Invoke(new ToolCallStart(displayName, description, argPreview, argsForLog));

                    var result = await next(fctx, fct);

                    var resultText = result?.ToString() ?? "";
                    completedHandler?.Invoke(new ToolCallResult(displayName, argsForLog, resultText));
                    logger.LogTool(displayName, argsForLog, resultText);

                    return result;
                })
            .Build();

        var agentSession = await agent.CreateSessionAsync().ConfigureAwait(false);

        var existing = logger.LoadExistingHistory();
        var resumeMessages = existing
            .Select(e => new ChatMessage(e.Role == "user" ? ChatRole.User : ChatRole.Assistant, e.Content))
            .ToList();
        var resumedAgentMessages = existing.Select(e => new AgentMessage(e.Role, e.Content)).ToList();

        var engine = new AgentEngine(
            agent, agentSession, modelId, context, targetFolder, logger, rateLimiter, speech,
            resumeMessages, resumedAgentMessages);

        startHandler = call => engine.ToolCallStarted?.Invoke(call);
        completedHandler = call => engine.ToolCallCompleted?.Invoke(call);
        noticeHandler = notice => engine.Notice?.Invoke(notice);

        return engine;
    }

    // ── Turn processing ──────────────────────────────────────────────────────

    /// <summary>
    /// Sends one user message and returns the agent's final answer. Retries up to 3 times on
    /// HTTP 429. On cancellation, the unanswered user message is removed from local history
    /// before the <see cref="OperationCanceledException"/> is rethrown, so history stays
    /// consistent. The first turn after resuming a session replays the prior transcript.
    /// </summary>
    public async Task<AgentTurnResult> SendAsync(string userText, CancellationToken ct = default)
    {
        _logger.LogUser(userText);
        _messages.Add(new AgentMessage("user", userText));

        for (var attempt = 0; attempt <= 3; attempt++)
        {
            try
            {
                await _rateLimiter.ThrottleAsync(EstimateTokens(_messages), ct);

                AgentResponse response;
                if (_pendingResumeMessages.Count > 0)
                {
                    var seeded = new List<ChatMessage>(_pendingResumeMessages) { new(ChatRole.User, userText) };
                    response = await _agent.RunAsync(seeded, _agentSession, cancellationToken: ct);
                    _pendingResumeMessages.Clear();
                }
                else
                {
                    response = await _agent.RunAsync(userText, _agentSession, cancellationToken: ct);
                }

                var raw = response.Text ?? "(no response)";
                var (thinking, finalText) = ExtractThinking(raw);
                var displayText = string.IsNullOrWhiteSpace(finalText) ? "(no final answer yet)" : finalText;
                var historyText = string.IsNullOrWhiteSpace(finalText) ? raw : finalText;

                _messages.Add(new AgentMessage("assistant", historyText));
                LastFinalText = string.IsNullOrWhiteSpace(finalText) ? null : finalText;
                _logger.LogAssistant(thinking, displayText);

                return new AgentTurnResult(thinking, displayText, Error: null);
            }
            catch (OperationCanceledException)
            {
                if (_messages.Count > 0 && _messages[^1].Role == "user")
                    _messages.RemoveAt(_messages.Count - 1);
                throw;
            }
            catch (Exception ex) when (attempt < 3 && Is429(ex))
            {
                await _rateLimiter.RetryDelayAsync(ExtractRetryAfter(ex), ct);
            }
            catch (Exception ex)
            {
                return new AgentTurnResult(null, null, Error: ex.Message);
            }
        }

        return new AgentTurnResult(null, null, Error: "Rate limit retry budget exceeded (HTTP 429).");
    }

    // ── Reasoning extraction ─────────────────────────────────────────────────

    /// <summary>
    /// Reasoning models (e.g. DeepSeek-R1 style) emit a &lt;think&gt;…&lt;/think&gt;
    /// block ahead of the final answer when called through an OpenAI-compatible endpoint.
    /// Splits it out so callers only deal with the final answer, while the transcript log
    /// keeps both.
    /// </summary>
    private static (string? Thinking, string Final) ExtractThinking(string raw)
    {
        const string openTag = "<think>";
        const string closeTag = "</think>";

        var openIdx = raw.IndexOf(openTag, StringComparison.OrdinalIgnoreCase);
        if (openIdx < 0) return (null, raw.Trim());

        var closeIdx = raw.IndexOf(closeTag, openIdx, StringComparison.OrdinalIgnoreCase);
        if (closeIdx < 0)
        {
            // Truncated mid-thought (e.g. hit a token limit) — no final answer yet.
            var thinkingOnly = raw[(openIdx + openTag.Length)..].Trim();
            return (thinkingOnly, "");
        }

        var thinking = raw[(openIdx + openTag.Length)..closeIdx].Trim();
        var final    = (raw[..openIdx] + raw[(closeIdx + closeTag.Length)..]).Trim();
        return (string.IsNullOrWhiteSpace(thinking) ? null : thinking, final);
    }

    // ── Token helpers ────────────────────────────────────────────────────────

    // 1 token ≈ 4 chars for English/code; +5 per message for role/structure overhead.
    private static int EstimateTokens(IEnumerable<AgentMessage> messages)
        => messages.Sum(m => m.Content.Length / 4 + 5);

    // ── 429 detection and retry-after extraction ─────────────────────────────

    private static bool Is429(Exception ex)
    {
        // Azure OpenAI and native OpenAI both surface HTTP errors as System.ClientModel
        // exceptions; Anthropic's SDK has its own hierarchy with a dedicated rate-limit type.
        if (ex is System.ClientModel.ClientResultException cre && (int)cre.Status == 429)
            return true;

        if (ex is Anthropic.Exceptions.AnthropicRateLimitException)
            return true;

        if (ex.Message.Contains("429",            StringComparison.Ordinal) ||
            ex.Message.Contains("TooManyRequests", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("rate limit",      StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("rate_limit",      StringComparison.OrdinalIgnoreCase))
            return true;

        return ex.InnerException is not null && Is429(ex.InnerException);
    }

    private static TimeSpan? ExtractRetryAfter(Exception ex)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            ex.Message,
            @"retry.after[:\s]+(\d+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return match.Success && int.TryParse(match.Groups[1].Value, out var s)
            ? TimeSpan.FromSeconds(s)
            : null;
    }

    // ── Universal tool docs ──────────────────────────────────────────────────

    /// <summary>
    /// Documents the engine-level tools registered directly by <see cref="CreateAsync"/> (as
    /// opposed to the context's own <see cref="IAgentContext.GetTools"/>), so the model knows
    /// they exist regardless of which context is active.
    /// </summary>
    private static string BuildUniversalToolsPrompt(bool hasSpeech)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("## Engine Tools (always available, independent of context)");
        sb.AppendLine("- AI.Ask — delegate an isolated sub-question or sub-task to the same model " +
                       "(e.g. summarize a long file) without adding it to the main conversation history.");
        if (hasSpeech)
        {
            sb.AppendLine("- Speech.Speak — speak text aloud through the configured voice.");
            sb.AppendLine("- Speech.Listen — listen on the microphone for one utterance and return the recognized text.");
        }
        return sb.ToString();
    }

    /// <summary>"FileSystem_WriteFile" (a valid tool name — providers reject dots) → "FileSystem.WriteFile" for display/logging.</summary>
    private static string FriendlyToolName(string name)
    {
        var idx = name.IndexOf('_');
        return idx > 0 ? string.Concat(name.AsSpan(0, idx), ".", name.AsSpan(idx + 1)) : name;
    }

    // ── Provider selection ───────────────────────────────────────────────────

    /// <summary>
    /// Builds the <see cref="IChatClient"/> for the configured AI provider — each using that
    /// provider's own official .NET SDK, not a shared connector:
    ///
    ///   AIProvider = "foundry" (default)  → Azure.AI.OpenAI against AzureAIFoundry:Endpoint
    ///   AIProvider = "openai"             → OpenAI's own .NET SDK against the public API
    ///   AIProvider = "anthropic"          → Anthropic's own .NET SDK (Claude)
    /// </summary>
    private static (IChatClient ChatClient, string ModelId) BuildChatClient(IConfiguration config, string contextKey)
    {
        var provider = (config["AIProvider"] ?? "foundry").Trim().ToLowerInvariant();

        return provider switch
        {
            "openai"    => BuildOpenAI(config),
            "anthropic" => BuildAnthropic(config),
            _           => BuildFoundry(config, contextKey),
        };
    }

    private static (IChatClient, string) BuildOpenAI(IConfiguration config)
    {
        var apiKey = config["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException(
                "OpenAI:ApiKey missing in appsettings.json or RALFAI_OpenAI__ApiKey env var");
        var modelId = config["OpenAI:ModelId"] ?? "gpt-5.1";

        var client = new OpenAIClient(apiKey);
        return (client.GetChatClient(modelId).AsIChatClient(), modelId);
    }

    private static (IChatClient, string) BuildAnthropic(IConfiguration config)
    {
        var apiKey = config["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException(
                "Anthropic:ApiKey missing in appsettings.json or RALFAI_Anthropic__ApiKey env var");
        var modelId = config["Anthropic:ModelId"] ?? "claude-sonnet-4-5";

        var client = new AnthropicClient { ApiKey = apiKey };
        return (client.AsIChatClient(modelId), modelId);
    }

    /// <summary>
    /// Resolves which Foundry model deployment to use for a given context.
    ///
    ///   AzureAIFoundry:ContextModels:&lt;contextKey&gt;   → named model to use for this context
    ///   AzureAIFoundry:DefaultModel                     → named model when the context has none
    ///   AzureAIFoundry:Models:&lt;name&gt;:ModelId       → that named model's deployment id
    ///
    /// Falls back to the legacy flat AzureAIFoundry:ModelId (single global model, "deepseek-r1"
    /// if unset) when no Models/ContextModels section is configured, so existing
    /// appsettings.json files keep working unchanged.
    /// </summary>
    private static (IChatClient, string) BuildFoundry(IConfiguration config, string contextKey)
    {
        var endpoint = config["AzureAIFoundry:Endpoint"]
            ?? throw new InvalidOperationException(
                "AzureAIFoundry:Endpoint missing in appsettings.json or RALFAI_AzureAIFoundry__Endpoint env var");
        var apiKey = config["AzureAIFoundry:ApiKey"]
            ?? throw new InvalidOperationException(
                "AzureAIFoundry:ApiKey missing in appsettings.json or RALFAI_AzureAIFoundry__ApiKey env var");

        var modelName = config[$"AzureAIFoundry:ContextModels:{contextKey}"]
            ?? config["AzureAIFoundry:DefaultModel"];

        var modelId = config["AzureAIFoundry:ModelId"] ?? "deepseek-r1";
        if (modelName is not null)
        {
            var section = config.GetSection($"AzureAIFoundry:Models:{modelName}");
            if (section.Exists())
                modelId = section["ModelId"] ?? modelName;
        }

        // The Foundry portal often shows a project URL (…/api/projects/<name>) or a full
        // …/chat/completions URL; the Azure OpenAI client just wants the bare resource host.
        var url = endpoint.TrimEnd('/');
        if (url.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            url = url[..^"/chat/completions".Length].TrimEnd('/');
        var host = new Uri(url).GetLeftPart(UriPartial.Authority);

        var client = new AzureOpenAIClient(new Uri(host), new AzureKeyCredential(apiKey));
        return (client.GetChatClient(modelId).AsIChatClient(), modelId);
    }
}
