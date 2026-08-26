using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RalfAI;

public sealed class RateLimiter
{
	private readonly record struct TokenSlot(DateTimeOffset Time, int Tokens);

	private readonly TimeSpan _minInterval;

	private readonly int _maxTpm;

	private readonly Action<AgentNotice>? _onNotice;

	private DateTimeOffset _lastCall = DateTimeOffset.MinValue;

	private readonly Queue<TokenSlot> _tokenWindow = new Queue<TokenSlot>();

	public RateLimiter(int maxRequestsPerMinute, int maxTokensPerMinute, double safetyFactor = 1.25, Action<AgentNotice>? onNotice = null)
	{
		_minInterval = TimeSpan.FromSeconds(((maxRequestsPerMinute > 0) ? (60.0 / (double)maxRequestsPerMinute) : 0.0) * safetyFactor);
		_maxTpm = ((maxTokensPerMinute > 0) ? maxTokensPerMinute : int.MaxValue);
		_onNotice = onNotice;
	}

	public async Task ThrottleAsync(int estimatedTokens, CancellationToken ct = default(CancellationToken))
	{
		TimeSpan sinceLast = DateTimeOffset.UtcNow - _lastCall;
		if (sinceLast < _minInterval)
		{
			TimeSpan wait = _minInterval - sinceLast;
			if (wait.TotalSeconds >= 0.5)
			{
				_onNotice?.Invoke(new AgentNotice($"⏱ Pacing ({wait.TotalSeconds:F1}s / interval {_minInterval.TotalSeconds:F1}s)…", NoticeLevel.Info));
			}
			await Task.Delay(wait, ct);
		}
		PurgeTokenWindow();
		int used = TokensUsed();
		if (_tokenWindow.Count > 0 && used + estimatedTokens > _maxTpm)
		{
			DateTimeOffset freeAt = _tokenWindow.Peek().Time.AddMinutes(1.0).AddMilliseconds(200.0);
			TimeSpan tpmWait = freeAt - DateTimeOffset.UtcNow;
			if (tpmWait > TimeSpan.Zero)
			{
				_onNotice?.Invoke(new AgentNotice($"⏳ TPM limit ({used}/{_maxTpm}) — waiting {tpmWait.TotalSeconds:F1}s…", NoticeLevel.Warning));
				await Task.Delay(tpmWait, ct);
				PurgeTokenWindow();
			}
		}
		_lastCall = DateTimeOffset.UtcNow;
		_tokenWindow.Enqueue(new TokenSlot(_lastCall, estimatedTokens));
	}

	public async Task RetryDelayAsync(TimeSpan? retryAfter, CancellationToken ct = default(CancellationToken))
	{
		TimeSpan wait = retryAfter ?? TimeSpan.FromSeconds(60L);
		_onNotice?.Invoke(new AgentNotice($"⛔ 429 Too Many Requests — waiting {wait.TotalSeconds:F0}s (Retry-After)…", NoticeLevel.Error));
		await Task.Delay(wait, ct);
		_tokenWindow.Clear();
		_lastCall = DateTimeOffset.MinValue;
	}

	private int TokensUsed()
	{
		return _tokenWindow.Sum((TokenSlot s) => s.Tokens);
	}

	private void PurgeTokenWindow()
	{
		DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow.AddMinutes(-1.0);
		while (_tokenWindow.Count > 0 && _tokenWindow.Peek().Time < dateTimeOffset)
		{
			_tokenWindow.Dequeue();
		}
	}
}
