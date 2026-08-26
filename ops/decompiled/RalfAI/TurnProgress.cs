using System;

namespace RalfAI;

public readonly record struct TurnProgress(string Phase, string Message, TimeSpan Elapsed);
