using System.Text.Json;

namespace RalfAI;

/// <summary>
/// Previously submitted prompts for one target folder, persisted one JSON string per line in
/// &lt;target&gt;/.ralfai/history.jsonl so multi-line prompts survive a restart.
/// </summary>
public sealed class PromptHistory
{
    private const int MaxEntries = 200;
    private static string PathFor(string folder) => Path.Combine(folder, ".ralfai", "history.jsonl");

    private readonly List<string> _entries = [];
    private string? _folder;

    public IReadOnlyList<string> Entries => _entries;

    /// <summary>Loads the history for <paramref name="folder"/> unless it is already loaded.</summary>
    public void Load(string folder)
    {
        if (string.Equals(_folder, folder, StringComparison.OrdinalIgnoreCase)) return;
        _folder = folder;
        _entries.Clear();
        try
        {
            if (!File.Exists(PathFor(folder))) return;
            foreach (var line in File.ReadAllLines(PathFor(folder)))
            {
                try
                {
                    if (JsonSerializer.Deserialize<string>(line) is { Length: > 0 } entry) _entries.Add(entry);
                }
                catch (JsonException) { }
            }
            if (_entries.Count > MaxEntries) _entries.RemoveRange(0, _entries.Count - MaxEntries);
        }
        catch (IOException) { }
    }

    public void Add(string entry)
    {
        if (_folder is null || entry.Length == 0) return;
        if (_entries.Count > 0 && _entries[^1] == entry) return;
        _entries.Add(entry);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PathFor(_folder))!);
            if (_entries.Count > MaxEntries)
            {
                _entries.RemoveRange(0, _entries.Count - MaxEntries);
                File.WriteAllLines(PathFor(_folder), _entries.Select(e => JsonSerializer.Serialize(e)));
            }
            else
            {
                File.AppendAllText(PathFor(_folder), JsonSerializer.Serialize(entry) + Environment.NewLine);
            }
        }
        catch (IOException) { /* best-effort, like the other .ralfai stores */ }
    }
}
