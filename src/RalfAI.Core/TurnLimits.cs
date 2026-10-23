using Microsoft.Extensions.Configuration;

namespace RalfAI;

/// <summary>Raised to abort a turn that ran past a hard limit (see <see cref="TurnLimits"/>).</summary>
public sealed class TurnStoppedException(string message) : Exception(message);

/// <summary>
/// Per-turn guards against a model that loops: too many tool calls, or far too much output.
/// Configured under <c>Limits</c> (0 turns a limit off):
/// <list type="bullet">
/// <item><c>MaxToolCalls</c> (default 150) - soft: past it, tool calls are answered with "limit reached, summarize and stop",
/// so the turn ends with a report; a model that ignores that is stopped after <see cref="ToolCallGrace"/> more calls.</item>
/// <item><c>MaxOutputTokens</c> (default 50000) - hard: the turn is aborted.</item>
/// <item><c>MaxReasoningChars</c> (default 30000) - hard: the visible reasoning of one turn, which is a few thousand
/// characters when healthy and runs away when the model repeats itself.</item>
/// </list>
/// </summary>
public sealed class TurnLimits(int maxToolCalls, long maxOutputTokens, int maxReasoningChars)
{
    public const int ToolCallGrace = 10;
    public const int DefaultMaxToolCalls = 150;
    public const long DefaultMaxOutputTokens = 50_000;
    public const int DefaultMaxReasoningChars = 30_000;

    private int _toolCalls;

    public int MaxToolCalls { get; } = maxToolCalls;
    public long MaxOutputTokens { get; } = maxOutputTokens;
    public int MaxReasoningChars { get; } = maxReasoningChars;
    public int ToolCalls => Volatile.Read(ref _toolCalls);

    public static TurnLimits FromConfig(IConfiguration config) => new(
        int.TryParse(config["Limits:MaxToolCalls"], out var calls) && calls >= 0 ? calls : DefaultMaxToolCalls,
        long.TryParse(config["Limits:MaxOutputTokens"], out var tokens) && tokens >= 0 ? tokens : DefaultMaxOutputTokens,
        int.TryParse(config["Limits:MaxReasoningChars"], out var chars) && chars >= 0 ? chars : DefaultMaxReasoningChars);

    public void StartTurn() => Interlocked.Exchange(ref _toolCalls, 0);

    /// <summary>Counts one tool call and returns its number within the turn (1-based). Safe for parallel calls.</summary>
    public int CountToolCall() => Interlocked.Increment(ref _toolCalls);

    public bool ToolBudgetExceeded(int callNumber) => MaxToolCalls > 0 && callNumber > MaxToolCalls;

    public bool ToolBudgetIgnored(int callNumber) => MaxToolCalls > 0 && callNumber > MaxToolCalls + ToolCallGrace;

    public string ToolBudgetMessage => $"Tool call limit reached ({MaxToolCalls} per turn). Do not call any more tools: " +
                                       "summarize what has been done and what is still left, so the user can ask you to continue.";

    /// <summary>The reason to abort the turn now, or null while it is within limits.</summary>
    public string? CheckStream(long outputTokens, int reasoningChars)
    {
        if (MaxOutputTokens > 0 && outputTokens > MaxOutputTokens)
            return $"Stopped: about {outputTokens:N0} output tokens in one turn (limit {MaxOutputTokens:N0}) - the model is probably stuck repeating itself. " +
                   "Ask again more narrowly, or raise Limits:MaxOutputTokens.";
        if (MaxReasoningChars > 0 && reasoningChars > MaxReasoningChars)
            return $"Stopped: {reasoningChars:N0} characters of reasoning in one turn (limit {MaxReasoningChars:N0}) - the model is probably looping. " +
                   "Ask again more narrowly, or raise Limits:MaxReasoningChars.";
        return null;
    }

    public string ToolBudgetIgnoredMessage => $"Stopped: the model kept calling tools after the limit of {MaxToolCalls} calls per turn. " +
                                              "Ask it to continue, or raise Limits:MaxToolCalls.";
}
