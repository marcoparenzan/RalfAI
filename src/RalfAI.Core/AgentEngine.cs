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
    private readonly TurnLimits _limits;
    private readonly UndoJournal _undo;
    private readonly PermissionPolicy _permissions;
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

    /// <summary>
    /// Asked before a command that is not allowed outright runs (see <see cref="PermissionPolicy"/>). When nothing is set the
    /// command is refused, so a frontend without a way to ask never runs more than the allowed list.
    /// </summary>
    public PermissionHandler? Permission { get; set; }

    /// <summary>The rules for process tools; read-only view for frontends that want to show them.</summary>
    public PermissionPolicy Permissions => _permissions;
    /// <summary>Raised for each fragment of the answer or the provider-exposed reasoning as it streams in.</summary>
    public event Action<StreamChunk>? Streamed;
    /// <summary>Raised a few times per second while generating, and once more when the turn ends.</summary>
    public event Action<TokenRate>? RateChanged;

    private long _sessionOutputTokens;
    private double _sessionGeneratingSeconds;
    private bool _requestReasoningSummary;
    private readonly ReasoningEffort? _reasoningEffort;

    /// <summary>Average output tokens/second over the completed turns of this session (0 until one completes).</summary>
    public double SessionTokensPerSecond => _sessionGeneratingSeconds > 0 ? _sessionOutputTokens / _sessionGeneratingSeconds : 0;

    private AgentEngine(AIAgent agent, AgentSession session, string modelId, IAgentContext context, string targetFolder,
        ConversationLogger logger, RateLimiter limiter, SpeechService? speech, IConfiguration configuration,
        string provider, List<ChatMessage> pending, List<AgentMessage> messages, TurnLimits limits, UndoJournal undo, PermissionPolicy permissions)
    {
        _agent = agent; _agentSession = session; ModelId = modelId; Context = context; TargetFolder = targetFolder;
        _logger = logger; _rateLimiter = limiter; Speech = speech; _configuration = configuration; _provider = provider;
        _pendingResumeMessages = pending; _messages = messages; ResumedMessageCount = messages.Count;
        _costHistory = new CostHistoryStore(targetFolder);
        _limits = limits;
        _undo = undo;
        _permissions = permissions;
        // Ask the provider to expose a reasoning summary; Reasoning:Summary = "off" disables it.
        _requestReasoningSummary = !string.Equals(configuration["Reasoning:Summary"], "off", StringComparison.OrdinalIgnoreCase);
        // Optional: Reasoning:Effort = none | low | medium | high | xhigh (unset = provider default).
        _reasoningEffort = Enum.TryParse<ReasoningEffort>(configuration["Reasoning:Effort"], ignoreCase: true, out var effort) ? effort : null;
    }

    public static Task<AgentEngine> CreateAsync(IConfiguration config, string targetFolder, string contextKey,
        IAgentContext context, CancellationToken ct = default)
        => CreateCoreAsync(config, targetFolder, contextKey, context, chatClientOverride: null);

    /// <summary>Creates an engine around a given chat client instead of the configured provider - for tests and embedding.</summary>
    public static Task<AgentEngine> CreateAsync(IConfiguration config, string targetFolder, string contextKey,
        IAgentContext context, IChatClient chatClient, string modelId = "test-model")
        => CreateCoreAsync(config, targetFolder, contextKey, context, (chatClient, modelId));

    private static async Task<AgentEngine> CreateCoreAsync(IConfiguration config, string targetFolder, string contextKey,
        IAgentContext context, (IChatClient Client, string ModelId)? chatClientOverride)
    {
        var toolChars = int.TryParse(config["Log:ToolOutputChars"], out var tc) && tc >= 0 ? tc : 2000;
        var maxLogMb = double.TryParse(config["Log:MaxMegabytes"], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var mb) && mb >= 0 ? mb : 5;
        var logger = new ConversationLogger(targetFolder, toolChars, (long)(maxLogMb * 1024 * 1024));
        var rpm = int.TryParse(config["AzureAIFoundry:RateLimits:RequestsPerMinute"], out var r) ? r : 20;
        var tpm = int.TryParse(config["AzureAIFoundry:RateLimits:TokensPerMinute"], out var t) ? t : 20000;
        var safety = double.TryParse(config["AzureAIFoundry:RateLimits:SafetyFactor"], System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var s) ? s : 1.25;
        Action<AgentNotice>? noticeHandler = null;
        var limiter = new RateLimiter(rpm, tpm, safety, n => noticeHandler?.Invoke(n));
        var speech = SpeechService.FromConfig(config);
        var imageGen = ImageGeneration.ImageGenerationService.FromConfig(config);
        var provider = (config["AIProvider"] ?? "foundry").Trim().ToLowerInvariant();
        var (chatClient, modelId) = chatClientOverride ?? ChatClientFactory.Create(config, contextKey);
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
        // Images:View false for a model that cannot take images.
        var imageView = !string.Equals(config["Images:View"], "false", StringComparison.OrdinalIgnoreCase);
        if (imageView) tools.Add(AIFunctionFactory.Create(new ImageViewPlugin(targetFolder).View, name: "Image_View"));
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

        var limits = TurnLimits.FromConfig(config);
        var permissions = PermissionPolicy.FromConfig(config);
        Func<PermissionHandler?> permissionHandler = () => null;
        Action<ToolCallStart>? started = null;
        Action<ToolCallResult>? completed = null;
        // Old tool output is stubbed in what is sent to the model (the session keeps everything) - see ContextCompactor.
        Action<CompactionStats>? compacted = null;
        // Image.View pictures are attached below the compactor, so they are never counted or stubbed as old images.
        IChatClient viewingClient = imageView ? new ImageViewingChatClient(chatClient, targetFolder) : chatClient;
        var agentClient = new CompactingChatClient(viewingClient, CompactionOptions.FromConfig(config), stats => compacted?.Invoke(stats));
        var agent = agentClient.AsAIAgent(context.BuildSystemPrompt(targetFolder) + UniversalPrompt(speech is not null, imageGen is not null, imageView), "RalfAI", tools: tools)
            .AsBuilder().Use(async (AIAgent _, FunctionInvocationContext fctx,
                Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken token) =>
            {
                var name = FriendlyToolName(fctx.Function.Name);
                var callNumber = limits.CountToolCall();
                if (limits.ToolBudgetIgnored(callNumber)) throw new TurnStoppedException(limits.ToolBudgetIgnoredMessage);
                if (limits.ToolBudgetExceeded(callNumber))
                {
                    if (callNumber == limits.MaxToolCalls + 1)
                        noticeHandler?.Invoke(new($"Tool call limit reached ({limits.MaxToolCalls} per turn) - asking the model to wrap up.", NoticeLevel.Warning));
                    return limits.ToolBudgetMessage;
                }
                var description = string.IsNullOrWhiteSpace(fctx.Function.Description) ? name : fctx.Function.Description;
                var args = string.Join(", ", fctx.Arguments.Where(x => x.Value is not null).Select(x => $"{x.Key}={x.Value}"));

                // Commands that can run arbitrary code or install software need a yes first.
                if (PermissionPolicy.Describe(fctx.Function.Name, fctx.Arguments) is { } command)
                {
                    var refusal = await CheckPermissionAsync(permissions, permissionHandler(), fctx.Function.Name, command, token);
                    if (refusal is not null)
                    {
                        logger.LogTool(name, args, "NOT RUN: " + refusal);
                        return refusal;
                    }
                }

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
        // Everything ever logged would be re-sent with every model call; keep only the most recent
        // part (History:ResumeMaxTokens, default 30000 estimated tokens) as model context.
        var resumeBudget = int.TryParse(config["History:ResumeMaxTokens"], out var rb) ? rb : 30_000;
        var resumeForModel = TrimToBudget(history, resumeBudget);
        var resumeMessages = resumeForModel.Select(x => new ChatMessage(x.Role == "user" ? ChatRole.User : ChatRole.Assistant, x.Content)).ToList();
        if (EarlierRequestsNote(history.Take(history.Count - resumeForModel.Count).Where(h => h.Role == "user").Select(h => h.Content)) is { } note)
            resumeMessages.Insert(0, new ChatMessage(ChatRole.User, note));
        var engine = new AgentEngine(agent, session, modelId, context, targetFolder, logger, limiter, speech, config, provider,
            resumeMessages,
            history.Select(x => new AgentMessage(x.Role, x.Content)).ToList(), limits,
            new UndoJournal(targetFolder, int.TryParse(config["Undo:Keep"], out var keep) && keep >= 0 ? keep : 10), permissions);
        permissionHandler = () => engine.Permission;
        var reportedItems = 0;
        compacted = stats =>
        {
            var items = stats.Results + stats.Arguments + stats.Images;
            if (items <= reportedItems) return;           // only say so when more gets compacted
            reportedItems = items;
            engine.Notice?.Invoke(new($"Context compacted: {stats.Results} old tool results, {stats.Arguments} large arguments, {stats.Images} images " +
                                      $"summarised (~{stats.CharsSaved / 4:N0} tokens saved per call).", NoticeLevel.Info));
        };
        started = x => engine.ToolCallStarted?.Invoke(x);
        completed = x => engine.ToolCallCompleted?.Invoke(x);
        noticeHandler = x => engine.Notice?.Invoke(x);
        return engine;
    }

    public Task<AgentTurnResult> SendAsync(string userText, CancellationToken ct = default)
        => SendAsync(userText, [], ct);

    /// <summary>What the last turn changed on disk (null if nothing, or if it was a no-op). Set even when the turn failed or was cancelled.</summary>
    public TurnChanges? LastTurnChanges { get; private set; }

    /// <summary>How many past turns can still be reverted with <see cref="Undo"/>.</summary>
    public int UndoableTurns => _undo.Count;

    /// <summary>The changes <see cref="Undo"/> would revert, or null when there is nothing to undo.</summary>
    public TurnChanges? PreviewUndo() => _undo.Preview();

    /// <summary>Reverts the most recent turn that changed files.</summary>
    public UndoReport Undo() => _undo.Undo();

    /// <summary>Sends a user message with images attached (sent to the model as image content).</summary>
    public async Task<AgentTurnResult> SendAsync(string userText, IReadOnlyList<ImageAttachment> images, CancellationToken ct = default)
    {
        // Written before the undo journal opens the turn, so the log entry is not a "change" to revert.
        PromptLog.Append(TargetFolder, userText, _configuration["PromptLog:File"] ?? PromptLog.DefaultFileName);
        _undo.BeginTurn(userText);
        AgentTurnResult result;
        try { result = await SendCoreAsync(userText, images, ct); }
        finally { LastTurnChanges = _undo.EndTurn(); }
        return result with { Changes = LastTurnChanges };
    }

    private async Task<AgentTurnResult> SendCoreAsync(string userText, IReadOnlyList<ImageAttachment> images, CancellationToken ct)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        Progress?.Invoke(new("prepare", $"Preparing request for {_provider}/{ModelId}", timer.Elapsed));
        var attachedNote = images.Count == 0 ? "" : "\n\n" + string.Join("\n", images.Select(i => $"[image attached: {i.FileName}]"));
        _logger.LogUser(userText + attachedNote); _messages.Add(new("user", userText + attachedNote));
        var userMessage = images.Count == 0
            ? new ChatMessage(ChatRole.User, userText)
            : new ChatMessage(ChatRole.User, [new TextContent(userText), .. images.Select(i => (AIContent)new DataContent(i.Content.ToMemory(), i.MediaType))]);
        for (var attempt = 0; attempt <= 3; attempt++)
        {
            try
            {
                Progress?.Invoke(new("rate-limit", "Checking request and token limits", timer.Elapsed));
                await _rateLimiter.ThrottleAsync(_messages.Sum(m => m.Content.Length / 4 + 5), ct);
                Progress?.Invoke(new("model", "Waiting for model response", timer.Elapsed));
                var turn = new StreamingTurn();
                _limits.StartTurn();
                var options = _requestReasoningSummary || _reasoningEffort is not null
                    ? new ChatClientAgentRunOptions(new ChatOptions
                    {
                        Reasoning = new ReasoningOptions
                        {
                            Effort = _reasoningEffort,
                            Output = _requestReasoningSummary ? ReasoningOutput.Summary : null
                        }
                    })
                    : null;
                var resumed = _pendingResumeMessages.Count > 0;
                var stream = resumed
                    ? _agent.RunStreamingAsync(new List<ChatMessage>(_pendingResumeMessages) { userMessage }, _agentSession, options, ct)
                    : _agent.RunStreamingAsync(userMessage, _agentSession, options, ct);

                var lastRate = TimeSpan.Zero;
                await foreach (var update in stream.WithCancellation(ct))
                {
                    if (update.Contents.OfType<TextContent>().Any(t => !string.IsNullOrEmpty(t.Text)))
                        turn.BeginMessage(update.MessageId, chunk => Streamed?.Invoke(chunk));
                    foreach (var content in update.Contents)
                        turn.Add(content, chunk => Streamed?.Invoke(chunk));

                    if (_limits.CheckStream(turn.OutputTokensEstimate, turn.ReasoningChars) is { } stopReason)
                        throw new TurnStoppedException(stopReason);

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
            catch (Exception ex) when (FindStop(ex) is { } stop)
            {
                // The model looped past a limit: abandon the turn (it is not remembered) and say why.
                timer.Stop();
                if (_messages.LastOrDefault()?.Role == "user") _messages.RemoveAt(_messages.Count - 1);
                _logger.LogAssistant(null, "[stopped] " + stop.Message);
                return new(null, null, stop.Message, Duration: timer.Elapsed);
            }
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

    /// <summary>
    /// What a resumed session tells the model about the requests that no longer fit its context: just the requests, one line
    /// each (the newest <paramref name="max"/>), so it knows the project has a history without paying for it. Null when none.
    /// </summary>
    internal static string? EarlierRequestsNote(IEnumerable<string> olderUserRequests, int max = 25)
    {
        var requests = olderUserRequests.Select(r => r.Replace("\r\n", "\n").Split('\n')[0].Trim()).Where(r => r.Length > 0).ToList();
        if (requests.Count == 0) return null;

        var shown = requests.Skip(Math.Max(0, requests.Count - max)).Select(r => r.Length > 100 ? r[..100].TrimEnd() + "…" : r);
        var omitted = requests.Count - Math.Min(requests.Count, max);
        return "[Note from RalfAI] This project has an earlier history that is no longer in your context. The user's older requests, oldest first" +
               (omitted > 0 ? $" (the {requests.Count - omitted} most recent of {requests.Count})" : "") + ": " +
               string.Join(" | ", shown.Select((r, i) => $"{omitted + i + 1}. {r}")) +
               ". Their details are gone; check the files (and PROMPTS.md, if present) rather than guessing what was done.";
    }

    /// <summary>Newest-first selection of messages that fits an estimated token budget (~4 chars/token), in original order.</summary>
    private static List<(string Role, string Content)> TrimToBudget(List<(string Role, string Content)> history, int budgetTokens)
    {
        if (budgetTokens <= 0) return history;
        var kept = new List<(string, string)>();
        long used = 0;
        for (var i = history.Count - 1; i >= 0; i--)
        {
            used += history[i].Content.Length / 4 + 5;
            if (used > budgetTokens && kept.Count > 0) break;
            kept.Add(history[i]);
        }
        kept.Reverse();
        // A resumed conversation should open on a user turn.
        while (kept.Count > 0 && kept[0].Item1 != "user") kept.RemoveAt(0);
        return kept;
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

    /// <summary>Returns null when the command may run, otherwise the message to give the model instead of running it.</summary>
    private static async Task<string?> CheckPermissionAsync(PermissionPolicy policy, PermissionHandler? handler, string toolName, string command, CancellationToken ct)
    {
        string Refusal(string reason) => $"Permission denied: `{command}` was not run ({reason}). Do not try it again; use another approach, or ask the user to run it themselves.";

        switch (policy.Evaluate(command))
        {
            case PermissionDecision.Allow: return null;
            case PermissionDecision.Deny: return Refusal("blocked by the permission rules or already refused by the user");
        }

        if (handler is null)
            return Refusal("it needs the user's approval and this session cannot ask; allow it in Permissions:Allow, or set Permissions:Mode to allow");

        var request = new PermissionRequest(toolName, command, PermissionPolicy.PatternFor(command), PermissionPolicy.WhyAsk(command));
        switch (await handler(request, ct))
        {
            case PermissionAnswer.AllowOnce: return null;
            case PermissionAnswer.AllowAlways:
                policy.GrantSession(request.AlwaysPattern);
                return null;
            default:
                policy.Refuse(command);
                return Refusal("the user said no");
        }
    }

    private static TurnStoppedException? FindStop(Exception? ex)
    {
        for (; ex is not null; ex = ex.InnerException)
            if (ex is TurnStoppedException stop) return stop;
        return null;
    }

    private static bool IsReasoningOptionError(Exception ex) =>
        (ex.Message.Contains("reasoning", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("summary", StringComparison.OrdinalIgnoreCase))
        && (ex.Message.Contains("400", StringComparison.Ordinal) || ex.Message.Contains("unsupported", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("not supported", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase));

    private static bool Is429(Exception ex) => ex.Message.Contains("429", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) || (ex.InnerException is not null && Is429(ex.InnerException));
    private static string FriendlyToolName(string name) { var i = name.IndexOf('_'); return i > 0 ? name[..i] + "." + name[(i + 1)..] : name; }
    private static string UniversalPrompt(bool speech, bool imageGen, bool imageView) => "\n\n## Engine tools\nAI.Ask and JPG/PNG manipulation are available."
        + (imageView ? " Image.View shows you image files of the project (PNG, JPEG, GIF, WebP): when the content of a picture matters, look at it instead of guessing from its name." : "")
        + (imageGen ? " Image.GenerateAI is available." : "") + (speech ? " Speech.Speak and Speech.Listen are available." : "")
        + "\n\n## Efficiency\nEvery model round-trip is slow and re-sends the whole conversation. When several tool calls do not depend on each other "
        + "(writing multiple files, reading several files, listing folders), issue them together in the same response instead of one per turn, "
        + "and skip re-reading files you just wrote."
        + "\n\n## Editing and exploring files\nChange part of an existing file with FileSystem.EditFile (exact oldText, unique, then newText); use WriteFile only for new files "
        + "or a full rewrite. For the same change in many files (renames, constants) use FileSystem.ReplaceInFiles once instead of editing file by file. To find something use FileSystem.SearchFiles (text/regex) or FileSystem.FindFiles (names) instead of listing or reading everything, "
        + "and read just the part you need with FileSystem.ReadFile startLine/endLine.";
}
