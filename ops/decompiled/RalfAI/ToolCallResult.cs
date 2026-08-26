namespace RalfAI;

public readonly record struct ToolCallResult(string Name, string ArgsForLog, string Result);
