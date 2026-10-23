using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using RalfAI.Contexts;
using RalfAI.Plugins;

namespace RalfAI.Tests;

/// <summary>One model call as the fake client saw it.</summary>
public sealed record ScriptedCall(int Index, IReadOnlyList<ChatMessage> Messages, ChatOptions? Options);

/// <summary>
/// An <see cref="IChatClient"/> that answers from a script instead of a model: tests decide, per call, which updates
/// (text, reasoning, function calls, usage) come back, and can inspect what the engine sent.
/// </summary>
public sealed class ScriptedChatClient(Func<ScriptedCall, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>> script) : IChatClient
{
    public List<ScriptedCall> Calls { get; } = [];

    public static ScriptedChatClient Of(Func<ScriptedCall, IEnumerable<ChatResponseUpdate>> script)
        => new((call, _) => Async(script(call)));

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var call = new ScriptedCall(Calls.Count, messages.ToList(), options);
        Calls.Add(call);
        await foreach (var update in script(call, cancellationToken).WithCancellation(cancellationToken))
            yield return update;
    }

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in GetStreamingResponseAsync(messages, options, cancellationToken)) updates.Add(update);
        return updates.ToChatResponse();
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }

    public static async IAsyncEnumerable<ChatResponseUpdate> Async(IEnumerable<ChatResponseUpdate> updates)
    {
        foreach (var update in updates)
        {
            await Task.Yield();
            yield return update;
        }
    }

    // ── Update builders ──────────────────────────────────────────────────────

    public static ChatResponseUpdate Text(string text, string messageId = "m1")
        => new(ChatRole.Assistant, text) { MessageId = messageId };

    public static ChatResponseUpdate Reasoning(string text, string messageId = "r1")
        => new(ChatRole.Assistant, [new TextReasoningContent(text)]) { MessageId = messageId };

    public static ChatResponseUpdate Usage(long input, long output, string messageId = "m1")
        => new(ChatRole.Assistant, [new UsageContent(new UsageDetails { InputTokenCount = input, OutputTokenCount = output, TotalTokenCount = input + output })]) { MessageId = messageId };

    /// <summary>A model response that asks for tools; the engine runs them and calls the client again with the results.</summary>
    public static ChatResponseUpdate ToolCalls(params (string Tool, Dictionary<string, object?> Arguments)[] calls)
        => new(ChatRole.Assistant, calls.Select((c, i) => (AIContent)new FunctionCallContent($"call-{i}-{Guid.NewGuid():N}", c.Tool, c.Arguments)).ToList()) { MessageId = "tools" };

    /// <summary>The text of every function result in a call's messages - what the model "saw" from its tools.</summary>
    public static IEnumerable<string> ToolResults(ScriptedCall call)
        => call.Messages.SelectMany(m => m.Contents).OfType<FunctionResultContent>().Select(r => r.Result?.ToString() ?? "");
}

/// <summary>A minimal context: the standard file tools plus whatever extra tools a test adds.</summary>
public sealed class StubContext(params AITool[] extraTools) : IAgentContext
{
    public string Name => "Test";
    public string Description => "test context";
    public string BuildSystemPrompt(string targetFolder) => $"You are a test agent working in {targetFolder}.";
    public IEnumerable<AITool> GetTools(string targetFolder) => [.. new FileSystemPlugin(targetFolder).CreateTools(), .. extraTools];
}

/// <summary>An engine over a temp folder and a scripted client; disposes the folder.</summary>
public sealed class EngineHarness : IDisposable
{
    public string Folder { get; }
    public ScriptedChatClient Client { get; }
    public AgentEngine Engine { get; private set; } = null!;
    public List<StreamChunk> Chunks { get; } = [];
    public List<AgentNotice> Notices { get; } = [];

    private EngineHarness(ScriptedChatClient client, string? folder)
    {
        Client = client;
        Folder = folder ?? Path.Combine(Path.GetTempPath(), $"ralf-engine-{Guid.NewGuid():N}");
    }

    public static async Task<EngineHarness> CreateAsync(
        ScriptedChatClient client, Dictionary<string, string?>? config = null, IAgentContext? context = null, string? folder = null)
    {
        var harness = new EngineHarness(client, folder);
        Directory.CreateDirectory(harness.Folder);
        await harness.StartEngineAsync(config, context);
        return harness;
    }

    public async Task StartEngineAsync(Dictionary<string, string?>? config = null, IAgentContext? context = null)
    {
        var values = new Dictionary<string, string?>
        {
            // No pacing between turns, or every test would wait seconds for the request-per-minute spacing.
            ["AzureAIFoundry:RateLimits:RequestsPerMinute"] = "0",
            ["AzureAIFoundry:RateLimits:TokensPerMinute"] = "0",
        };
        foreach (var (key, value) in config ?? []) values[key] = value;

        Engine = await AgentEngine.CreateAsync(
            new ConfigurationBuilder().AddInMemoryCollection(values).Build(), Folder, "test", context ?? new StubContext(), Client);
        Engine.Streamed += Chunks.Add;
        Engine.Notice += Notices.Add;
    }

    public string PathOf(string relative) => Path.Combine(Folder, relative.Replace('/', Path.DirectorySeparatorChar));

    public void Dispose()
    {
        try { Directory.Delete(Folder, recursive: true); } catch (IOException) { }
    }
}
