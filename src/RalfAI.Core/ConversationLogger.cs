using System.Globalization;
using System.Text.Json;

namespace RalfAI;

/// <summary>
/// Records the conversation inside the project's <c>.ralfai</c> folder, in two files with different jobs:
/// <list type="bullet">
/// <item><c>session.md</c> - the human transcript: user messages, tool calls (their arguments and output cut to
/// <c>Log:ToolOutputChars</c>, default 2000, so a big file read does not live there forever) and agent answers with their
/// reasoning. When it passes <c>Log:MaxMegabytes</c> (default 5) it is renamed to <c>session.&lt;time&gt;.md</c> and a fresh one starts.</item>
/// <item><c>session.jsonl</c> - what resuming needs: one JSON object per user or agent message, nothing else. Small, quick to read
/// and not dependent on parsing Markdown. A project that only has the old <c>session.md</c> is converted the first time.</item>
/// </list>
/// </summary>
public sealed class ConversationLogger
{
    private const int DefaultToolOutputChars = 2000;
    private const long DefaultMaxLogBytes = 5L * 1024 * 1024;

    private readonly string _folder;
    private readonly string _logPath;
    private readonly string _jsonlPath;
    private readonly int _toolOutputChars;
    private bool _headerWritten;

    private sealed record Entry(DateTimeOffset? T, string Role, string Text);

    public ConversationLogger(string targetFolder, int toolOutputChars = DefaultToolOutputChars, long maxLogBytes = DefaultMaxLogBytes)
    {
        _folder = Path.Combine(targetFolder, ".ralfai");
        Directory.CreateDirectory(_folder);
        _logPath = Path.Combine(_folder, "session.md");
        _jsonlPath = Path.Combine(_folder, "session.jsonl");
        _toolOutputChars = toolOutputChars;

        // Convert an old transcript BEFORE it can be rotated away, or its conversation would be lost.
        if (!File.Exists(_jsonlPath) && File.Exists(_logPath)) MigrateFromMarkdown();
        if (maxLogBytes > 0 && File.Exists(_logPath) && new FileInfo(_logPath).Length > maxLogBytes) Rotate();

        _headerWritten = File.Exists(_logPath);
    }

    public string LogPath => _logPath;

    public string HistoryPath => _jsonlPath;

    public void LogUser(string message)
    {
        Append("USER", message);
        AppendHistory("user", message);
    }

    public void LogAssistant(string? thinking, string message)
    {
        var body = string.IsNullOrWhiteSpace(thinking)
            ? message
            : $"<!-- thinking\n{thinking}\n-->\n\n{message}";
        Append("AGENT", body);
        AppendHistory("assistant", message);
    }

    public void LogTool(string name, string args, string result)
        => Append($"TOOL — {name}", $"args: {Clip(args)}\nresult: {Clip(result)}");

    // ── Transcript (session.md) ──────────────────────────────────────────────

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

    private string Clip(string text)
    {
        if (_toolOutputChars <= 0 || text.Length <= _toolOutputChars) return text;
        return text[.._toolOutputChars] + $"\n… [{(text.Length - _toolOutputChars).ToString("N0", CultureInfo.InvariantCulture)} more characters not logged]";
    }

    private void Rotate()
    {
        var rotated = Path.Combine(_folder, $"session.{DateTime.Now:yyyyMMdd-HHmmss}.md");
        try { File.Move(_logPath, rotated); }
        catch (IOException) { /* in use or read-only: keep appending to the big file */ }
    }

    // ── History for resuming (session.jsonl) ─────────────────────────────────

    private void AppendHistory(string role, string text)
    {
        var line = JsonSerializer.Serialize(new Entry(DateTimeOffset.Now, role, text));
        File.AppendAllText(_jsonlPath, line + Environment.NewLine);
    }

    /// <summary>The user and agent messages of this project so far, oldest first; empty when there is no history yet.</summary>
    public List<(string Role, string Content)> LoadExistingHistory()
    {
        if (!File.Exists(_jsonlPath)) return [];

        var result = new List<(string, string)>();
        foreach (var line in File.ReadLines(_jsonlPath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var entry = JsonSerializer.Deserialize<Entry>(line);
                if (entry is { Role: "user" or "assistant", Text.Length: > 0 }) result.Add((entry.Role, entry.Text));
            }
            catch (JsonException) { /* a half-written line must not lose the rest of the history */ }
        }
        return result;
    }

    private void MigrateFromMarkdown()
    {
        var history = ParseMarkdown(File.ReadAllLines(_logPath));
        var lines = history.Select(h => JsonSerializer.Serialize(new Entry(null, h.Role, h.Content)));
        File.WriteAllLines(_jsonlPath, lines);
    }

    /// <summary>
    /// Reads a transcript written by an earlier version: (role, content) pairs in order. Tool blocks are skipped and the
    /// "thinking" comment inside an agent block is dropped, since neither is replayed to the model as conversation.
    /// </summary>
    private static List<(string Role, string Content)> ParseMarkdown(string[] lines)
    {
        var result = new List<(string, string)>();
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
