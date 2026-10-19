namespace RalfAI;

/// <summary>Result of one agent turn (<see cref="AgentEngine.SendAsync"/>).</summary>
public sealed record AgentTurnResult(
    string? Thinking,
    string? FinalText,
    string? Error,
    TurnUsage? Usage = null,
    TimeSpan? Duration = null,
    double? TokensPerSecond = null);

/// <summary>Token usage reported by the provider and an optional client-side cost estimate.</summary>
public sealed record TurnUsage(long? InputTokens, long? OutputTokens, long? TotalTokens, decimal? EstimatedCostUsd)
{
    public bool IsReported => InputTokens is not null || OutputTokens is not null || TotalTokens is not null;
}

/// <summary>An observable phase in a turn; it never exposes hidden chain-of-thought.</summary>
public readonly record struct TurnProgress(string Phase, string Message, TimeSpan Elapsed);

/// <summary>A single message in the conversation, projected for UI binding (no framework types).</summary>
public sealed record AgentMessage(string Role, string Content);

/// <summary>Raised just before a tool/plugin function is invoked.</summary>
public readonly record struct ToolCallStart(string Name, string Description, string ArgsPreview, string ArgsForLog);

/// <summary>Raised right after a tool/plugin function returns.</summary>
public readonly record struct ToolCallResult(string Name, string ArgsForLog, string Result);

/// <summary>What a streamed fragment belongs to.</summary>
public enum StreamKind { Reasoning, Answer }

/// <summary>A fragment of model output as it arrives. Reasoning is whatever the provider chooses to expose
/// (a reasoning summary, or inline &lt;think&gt; text) - never hidden chain-of-thought.</summary>
public readonly record struct StreamChunk(StreamKind Kind, string Text);

/// <summary>Live generation speed. Tokens are estimated from streamed text until the provider reports usage.</summary>
public readonly record struct TokenRate(
    long TurnOutputTokens,
    double InstantTokensPerSecond,
    double TurnAverageTokensPerSecond,
    double SessionAverageTokensPerSecond,
    bool Estimated);
