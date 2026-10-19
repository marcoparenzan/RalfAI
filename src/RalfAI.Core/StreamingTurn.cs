using System.Diagnostics;
using System.Text;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

[assembly: InternalsVisibleTo("RalfAI.Tests")]

namespace RalfAI;

/// <summary>
/// Splits streamed text on <c>&lt;think&gt;…&lt;/think&gt;</c> (emitted inline by some models) even when a
/// tag is cut across two chunks: a possible partial tag at the end of a chunk is held back until
/// the next one shows whether it really is a tag.
/// </summary>
internal sealed class ThinkTagSplitter
{
    private const string Open = "<think>";
    private const string Close = "</think>";

    private bool _inThink;
    private string _pending = "";

    public IEnumerable<(bool IsThinking, string Text)> Feed(string chunk)
    {
        _pending += chunk;
        var result = new List<(bool, string)>();
        while (true)
        {
            var tag = _inThink ? Close : Open;
            var index = _pending.IndexOf(tag, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                if (index > 0) result.Add((_inThink, _pending[..index]));
                _pending = _pending[(index + tag.Length)..];
                _inThink = !_inThink;
                continue;
            }

            var keep = PartialTagSuffix(_pending, tag);
            if (_pending.Length > keep) result.Add((_inThink, _pending[..^keep]));
            _pending = _pending[^keep..];
            return result;
        }
    }

    public IEnumerable<(bool IsThinking, string Text)> Flush()
    {
        var rest = _pending;
        _pending = "";
        return rest.Length == 0 ? [] : [(_inThink, rest)];
    }

    /// <summary>Length of the longest suffix of <paramref name="text"/> that is a proper prefix of <paramref name="tag"/>.</summary>
    private static int PartialTagSuffix(string text, string tag)
    {
        for (var length = Math.Min(text.Length, tag.Length - 1); length > 0; length--)
            if (text.EndsWith(tag[..length], StringComparison.OrdinalIgnoreCase)) return length;
        return 0;
    }
}

/// <summary>
/// Accumulates one streamed model turn: the raw answer text, the provider's reasoning summary,
/// summed usage across every model call of the turn, and live token-rate figures. Output tokens
/// are estimated from streamed characters (~4 per token) until the provider reports real usage.
/// </summary>
internal sealed class StreamingTurn
{
    private const double CharsPerToken = 4.0;
    private static readonly TimeSpan RateWindow = TimeSpan.FromSeconds(2);

    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly ThinkTagSplitter _splitter = new();
    private readonly StringBuilder _raw = new();
    private readonly StringBuilder _summary = new();
    private readonly Queue<(TimeSpan At, int Chars)> _window = new();
    private TimeSpan? _firstToken;
    private long _confirmedOutput;
    private long _charsSinceUsage;
    private long _input;
    private long _output;
    private long _total;
    private bool _usageSeen;
    private string? _lastMessageId;

    public string RawText => _raw.ToString();
    public string ReasoningSummary => _summary.ToString().Trim();
    public bool SawAnyText => _firstToken is not null;

    /// <summary>Summed provider-reported usage for the turn, or null when none was reported.</summary>
    public UsageDetails? Usage => !_usageSeen ? null : new UsageDetails
    {
        InputTokenCount = _input,
        OutputTokenCount = _output,
        TotalTokenCount = _total
    };

    /// <summary>
    /// Call before adding the text of an update: when a new model message starts (e.g. after a tool
    /// call) and the previous text did not end in a line break, a blank line keeps them apart.
    /// </summary>
    public void BeginMessage(string? messageId, Action<StreamChunk> emit)
    {
        var changed = messageId != _lastMessageId;
        _lastMessageId = messageId;
        if (!changed || _raw.Length == 0 || _raw[^1] == '\n') return;
        const string separator = "\n\n";
        _raw.Append(separator);
        emit(new StreamChunk(StreamKind.Answer, separator));
    }

    public void Add(AIContent content, Action<StreamChunk> emit)
    {
        switch (content)
        {
            case TextReasoningContent { Text.Length: > 0 } reasoning:
                _summary.Append(reasoning.Text);
                Count(reasoning.Text.Length);
                emit(new StreamChunk(StreamKind.Reasoning, reasoning.Text));
                break;

            case TextContent { Text.Length: > 0 } text:
                _raw.Append(text.Text);
                Count(text.Text.Length);
                foreach (var (isThinking, part) in _splitter.Feed(text.Text))
                    emit(new StreamChunk(isThinking ? StreamKind.Reasoning : StreamKind.Answer, part));
                break;

            case UsageContent usage when usage.Details is { } details:
                _usageSeen = true;
                _input += details.InputTokenCount ?? 0;
                _output += details.OutputTokenCount ?? 0;
                _total += details.TotalTokenCount ?? (details.InputTokenCount ?? 0) + (details.OutputTokenCount ?? 0);
                // The provider's number replaces the estimate for everything streamed so far.
                _confirmedOutput += details.OutputTokenCount ?? 0;
                _charsSinceUsage = 0;
                break;
        }
    }

    public void Flush(Action<StreamChunk> emit)
    {
        foreach (var (isThinking, part) in _splitter.Flush())
            emit(new StreamChunk(isThinking ? StreamKind.Reasoning : StreamKind.Answer, part));
    }

    private void Count(int chars)
    {
        var now = _clock.Elapsed;
        _firstToken ??= now;
        _charsSinceUsage += chars;
        _window.Enqueue((now, chars));
    }

    public TokenRate Snapshot(double sessionTokensPerSecond)
    {
        var now = _clock.Elapsed;
        while (_window.Count > 0 && now - _window.Peek().At > RateWindow) _window.Dequeue();

        var tokens = _confirmedOutput + (long)(_charsSinceUsage / CharsPerToken);
        var generating = _firstToken is null ? 0 : (now - _firstToken.Value).TotalSeconds;

        // Instant rate: visible characters seen in the last couple of seconds. Hidden reasoning
        // tokens never stream, so this is a floor; the average uses the provider's real count.
        var windowSeconds = Math.Clamp(generating, 0.5, RateWindow.TotalSeconds);
        var instant = _window.Sum(w => w.Chars) / CharsPerToken / windowSeconds;
        var average = generating > 0.3 ? tokens / generating : 0;

        return new TokenRate(tokens, instant, average, sessionTokensPerSecond, Estimated: _charsSinceUsage > 0 || !_usageSeen);
    }

    /// <summary>Output tokens (real if reported, else estimated) and seconds since the first token.</summary>
    public (long OutputTokens, double GeneratingSeconds) Totals()
    {
        var tokens = _usageSeen ? _output : (long)(_charsSinceUsage / CharsPerToken);
        var seconds = _firstToken is null ? 0 : (_clock.Elapsed - _firstToken.Value).TotalSeconds;
        return (tokens, seconds);
    }
}
