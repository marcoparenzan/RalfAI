namespace RalfAI;

/// <summary>Result of one agent turn (<see cref="AgentEngine.SendAsync"/>).</summary>
public sealed record AgentTurnResult(string? Thinking, string? FinalText, string? Error);

/// <summary>A single message in the conversation, projected for UI binding (no framework types).</summary>
public sealed record AgentMessage(string Role, string Content);

/// <summary>Raised just before a tool/plugin function is invoked.</summary>
public readonly record struct ToolCallStart(string Name, string Description, string ArgsPreview, string ArgsForLog);

/// <summary>Raised right after a tool/plugin function returns.</summary>
public readonly record struct ToolCallResult(string Name, string ArgsForLog, string Result);
