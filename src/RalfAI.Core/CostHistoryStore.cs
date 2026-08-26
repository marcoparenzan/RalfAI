using System.Text.Json;

namespace RalfAI;

/// <summary>Append-only per-project usage/cost history. Cost is an estimate based on configured pricing.</summary>
public sealed class CostHistoryStore
{
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public CostHistoryStore(string targetFolder) => _path = System.IO.Path.Combine(targetFolder, ".ralfai", "cost-history.jsonl");
    public string Path => _path;

    public async Task AppendAsync(CostHistoryEntry entry, CancellationToken ct = default)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!);
        var json = JsonSerializer.Serialize(entry);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try { await File.AppendAllTextAsync(_path, json + Environment.NewLine, ct).ConfigureAwait(false); }
        finally { _gate.Release(); }
    }

    public IReadOnlyList<CostHistoryEntry> ReadAll()
    {
        if (!File.Exists(_path)) return [];
        return File.ReadLines(_path)
            .Select(line => { try { return JsonSerializer.Deserialize<CostHistoryEntry>(line); } catch { return null; } })
            .Where(entry => entry is not null).Cast<CostHistoryEntry>().ToList();
    }

    /// <summary>Cumulative spend for this project so far. Turns whose model had no configured
    /// pricing contribute tokens/count but not cost — <see cref="TurnsWithCost"/> tracks how
    /// many turns actually had a cost, so the caller can tell a real $0 from "some turns are
    /// missing pricing" instead of silently under-reporting the total.</summary>
    public CostSummary Summarize()
    {
        var all = ReadAll();
        var withCost = all.Where(e => e.EstimatedCostUsd is not null).ToList();
        return new CostSummary(
            Turns: all.Count,
            TurnsWithCost: withCost.Count,
            TotalCostUsd: withCost.Sum(e => e.EstimatedCostUsd!.Value),
            TotalInputTokens: all.Sum(e => e.InputTokens ?? 0),
            TotalOutputTokens: all.Sum(e => e.OutputTokens ?? 0));
    }
}

public sealed record CostHistoryEntry(DateTimeOffset Timestamp, string Provider, string ModelId, long? InputTokens,
    long? OutputTokens, long? TotalTokens, decimal? EstimatedCostUsd, double DurationSeconds);

/// <summary>Cumulative usage/spend for a project, aggregated from its <see cref="CostHistoryEntry"/> log.</summary>
public sealed record CostSummary(int Turns, int TurnsWithCost, decimal TotalCostUsd, long TotalInputTokens, long TotalOutputTokens);
