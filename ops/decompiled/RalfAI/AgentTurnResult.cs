using System;

namespace RalfAI;

public sealed record AgentTurnResult(string? Thinking, string? FinalText, string? Error, TurnUsage? Usage = null, TimeSpan? Duration = null);
