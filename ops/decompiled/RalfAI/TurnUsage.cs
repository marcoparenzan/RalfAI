namespace RalfAI;

public sealed record TurnUsage(long? InputTokens, long? OutputTokens, long? TotalTokens, decimal? EstimatedCostUsd)
{
	public bool IsReported => InputTokens.HasValue || OutputTokens.HasValue || TotalTokens.HasValue;
}
