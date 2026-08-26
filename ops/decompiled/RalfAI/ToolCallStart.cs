namespace RalfAI;

public readonly record struct ToolCallStart(string Name, string Description, string ArgsPreview, string ArgsForLog);
