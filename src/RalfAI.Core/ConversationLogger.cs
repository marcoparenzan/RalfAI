namespace RalfAI;

/// <summary>
/// Appends every turn of the conversation — user messages, tool calls, and
/// agent responses (including any extracted "thinking" block) — to a markdown
/// transcript inside a `.ralfai` folder next to the generated project.
/// On startup, can reload the history so a session resumes where it left off.
/// </summary>
public sealed class ConversationLogger
{
    private readonly string _logPath;
    private bool _headerWritten;

    public ConversationLogger(string targetFolder)
    {
        var chatFolder = Path.Combine(targetFolder, ".ralfai");
        Directory.CreateDirectory(chatFolder);
        _logPath = Path.Combine(chatFolder, "session.md");
        _headerWritten = File.Exists(_logPath);
    }

    public string LogPath => _logPath;

    public void LogUser(string message) => Append("USER", message);

    public void LogAssistant(string? thinking, string message)
    {
        var body = string.IsNullOrWhiteSpace(thinking)
            ? message
            : $"<!-- thinking\n{thinking}\n-->\n\n{message}";
        Append("AGENT", body);
    }

    public void LogTool(string name, string args, string result)
        => Append($"TOOL — {name}", $"args: {args}\nresult: {result}");

    private void Append(string header, string content)
    {
        if (!_headerWritten)
        {
            File.WriteAllText(_logPath,
                $"# RalfAI Session\n\n> Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n---\n\n");
            _headerWritten = true;
        }

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var block = $"### {header} — {timestamp}\n\n{content}\n\n---\n\n";
        File.AppendAllText(_logPath, block);
    }

    /// <summary>
    /// Parses the existing log and returns (role, content) pairs in order,
    /// ready to seed a resumed ChatHistory. Tool-call blocks are skipped and
    /// any "thinking" comment inside an agent block is stripped, since neither
    /// should be replayed back to the model as prior conversation turns.
    /// Returns empty list if no log exists yet.
    /// </summary>
    public List<(string Role, string Content)> LoadExistingHistory()
    {
        if (!File.Exists(_logPath))
            return [];

        var result = new List<(string, string)>();
        var lines = File.ReadAllLines(_logPath);
        string? currentRole = null;
        var buffer = new List<string>();

        foreach (var line in lines)
        {
            if (line.StartsWith("### USER") || line.StartsWith("### AGENT") || line.StartsWith("### TOOL"))
            {
                FlushBuffer(result, currentRole, buffer);
                buffer.Clear();
                currentRole = line.StartsWith("### USER") ? "user"
                            : line.StartsWith("### AGENT") ? "assistant"
                            : "tool";
            }
            else if (line == "---" && currentRole is not null)
            {
                FlushBuffer(result, currentRole, buffer);
                buffer.Clear();
                currentRole = null;
            }
            else if (currentRole is not null)
            {
                buffer.Add(line);
            }
        }

        FlushBuffer(result, currentRole, buffer);
        return result;
    }

    private static void FlushBuffer(List<(string, string)> list, string? role, List<string> lines)
    {
        if (role is null || role == "tool") return;

        var effectiveLines = role == "assistant" ? StripThinkingBlock(lines) : lines;
        var content = string.Join("\n", effectiveLines).Trim();
        if (!string.IsNullOrWhiteSpace(content))
            list.Add((role, content));
    }

    private static List<string> StripThinkingBlock(List<string> lines)
    {
        var result = new List<string>();
        var inThinking = false;

        foreach (var line in lines)
        {
            if (!inThinking && line.StartsWith("<!-- thinking", StringComparison.Ordinal))
            {
                inThinking = true;
                continue;
            }

            if (inThinking)
            {
                if (line == "-->") inThinking = false;
                continue;
            }

            result.Add(line);
        }

        return result;
    }
}
