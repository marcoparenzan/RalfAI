namespace RalfAI;

public enum NoticeLevel { Info, Warning, Error }

/// <summary>A human-readable pacing/backoff notice, for whichever frontend wants to display it.</summary>
public readonly record struct AgentNotice(string Message, NoticeLevel Level);

/// <summary>
/// Proactive rate limiter for LLM API calls.
///
/// RPM strategy — preventive interval pacing:
///   Wait at least (60s / RPM) * safetyFactor between requests.
///   Example: 20 RPM, safetyFactor 1.25 → minimum 3.75 s between calls.
///   This smooths out traffic instead of letting requests pile up and then hard-blocking.
///   Small waits (< 0.5 s) are silent; larger ones raise a notice.
///
/// TPM strategy — sliding window hard stop:
///   If the 60-second rolling token count would exceed the limit, wait until
///   the oldest slot expires. This is a fallback for very large contexts.
///
/// 429 fallback:
///   RetryDelayAsync() backs off for the Retry-After duration (default 60 s),
///   then resets both pacing state and the token window.
/// </summary>
public sealed class RateLimiter
{
    private readonly TimeSpan _minInterval;
    private readonly int _maxTpm;
    private readonly Action<AgentNotice>? _onNotice;

    private DateTimeOffset _lastCall = DateTimeOffset.MinValue;

    // Sliding window used only for TPM tracking.
    private readonly record struct TokenSlot(DateTimeOffset Time, int Tokens);
    private readonly Queue<TokenSlot> _tokenWindow = new();

    /// <param name="maxRequestsPerMinute">API RPM limit (0 = unlimited).</param>
    /// <param name="maxTokensPerMinute">API TPM limit (0 = unlimited).</param>
    /// <param name="safetyFactor">
    ///   Multiplier applied to the base interval.
    ///   1.25 means "space requests as if the limit were 25 % stricter",
    ///   so the minimum gap is (60 / RPM) * 1.25.
    ///   Use 1.0 to sit exactly at the theoretical limit.
    /// </param>
    /// <param name="onNotice">
    ///   Optional callback for pacing/TPM/429 notices — the frontend decides how (or whether) to
    ///   render them; RateLimiter has no UI dependency of its own.
    /// </param>
    public RateLimiter(int maxRequestsPerMinute, int maxTokensPerMinute, double safetyFactor = 1.25,
        Action<AgentNotice>? onNotice = null)
    {
        var baseSeconds = maxRequestsPerMinute > 0
            ? 60.0 / maxRequestsPerMinute
            : 0.0;

        _minInterval = TimeSpan.FromSeconds(baseSeconds * safetyFactor);
        _maxTpm      = maxTokensPerMinute > 0 ? maxTokensPerMinute : int.MaxValue;
        _onNotice    = onNotice;
    }

    /// <summary>
    /// Waits the minimum pacing interval since the last call, then checks the TPM window.
    /// Call this immediately before every top-level LLM API call.
    /// </summary>
    public async Task ThrottleAsync(int estimatedTokens, CancellationToken ct = default)
    {
        // ── 1. RPM pacing ────────────────────────────────────────────────────
        var sinceLast = DateTimeOffset.UtcNow - _lastCall;
        if (sinceLast < _minInterval)
        {
            var wait = _minInterval - sinceLast;
            if (wait.TotalSeconds >= 0.5)
                _onNotice?.Invoke(new AgentNotice(
                    $"⏱ Pacing ({wait.TotalSeconds:F1}s / interval {_minInterval.TotalSeconds:F1}s)…",
                    NoticeLevel.Info));
            await Task.Delay(wait, ct);
        }

        // ── 2. TPM hard stop ─────────────────────────────────────────────────
        PurgeTokenWindow();
        var used = TokensUsed();

        if (_tokenWindow.Count > 0 && used + estimatedTokens > _maxTpm)
        {
            var freeAt = _tokenWindow.Peek().Time.AddMinutes(1).AddMilliseconds(200);
            var tpmWait = freeAt - DateTimeOffset.UtcNow;
            if (tpmWait > TimeSpan.Zero)
            {
                _onNotice?.Invoke(new AgentNotice(
                    $"⏳ TPM limit ({used}/{_maxTpm}) — waiting {tpmWait.TotalSeconds:F1}s…",
                    NoticeLevel.Warning));
                await Task.Delay(tpmWait, ct);
                PurgeTokenWindow();
            }
        }

        // Record the call start time.
        _lastCall = DateTimeOffset.UtcNow;
        _tokenWindow.Enqueue(new(_lastCall, estimatedTokens));
    }

    /// <summary>
    /// Backs off after a 429 response, then resets pacing so the retry can proceed.
    /// </summary>
    public async Task RetryDelayAsync(TimeSpan? retryAfter, CancellationToken ct = default)
    {
        var wait = retryAfter ?? TimeSpan.FromSeconds(60);
        _onNotice?.Invoke(new AgentNotice(
            $"⛔ 429 Too Many Requests — waiting {wait.TotalSeconds:F0}s (Retry-After)…",
            NoticeLevel.Error));

        await Task.Delay(wait, ct);

        // Reset both pacing and token window so the next call goes through immediately.
        _tokenWindow.Clear();
        _lastCall = DateTimeOffset.MinValue;
    }

    private int TokensUsed() => _tokenWindow.Sum(s => s.Tokens);

    private void PurgeTokenWindow()
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-1);
        while (_tokenWindow.Count > 0 && _tokenWindow.Peek().Time < cutoff)
            _tokenWindow.Dequeue();
    }
}
