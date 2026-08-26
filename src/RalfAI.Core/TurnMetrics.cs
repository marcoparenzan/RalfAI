using System.Collections;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace RalfAI;

public static class TurnMetrics
{
    public static TurnUsage? Read(object response, IConfiguration configuration, string provider, string modelId)
    {
        var usage = FindUsage(response, 0, new HashSet<object>(ReferenceEqualityComparer.Instance));
        if (usage is null) return null;

        var input = ReadLong(usage, "InputTokenCount", "InputTokens", "PromptTokens", "PromptTokenCount");
        var output = ReadLong(usage, "OutputTokenCount", "OutputTokens", "CompletionTokens", "CompletionTokenCount");
        var total = ReadLong(usage, "TotalTokenCount", "TotalTokens") ??
                    (input is not null && output is not null ? input + output : null);
        if (input is null && output is null && total is null) return null;

        var section = configuration.GetSection($"Pricing:{provider}:{modelId}");
        var inputRate = ReadDecimal(section["InputUsdPerMillionTokens"]);
        var outputRate = ReadDecimal(section["OutputUsdPerMillionTokens"]);
        decimal? cost = input is not null && output is not null && inputRate is not null && outputRate is not null
            ? input.Value * inputRate.Value / 1_000_000m + output.Value * outputRate.Value / 1_000_000m
            : null;
        return new TurnUsage(input, output, total, cost);
    }

    private static object? FindUsage(object? value, int depth, HashSet<object> visited)
    {
        if (value is null || depth > 4 || value is string || !visited.Add(value)) return null;
        var type = value.GetType();
        foreach (var name in new[] { "Usage", "UsageDetails", "TokenUsage" })
        {
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var candidate = property?.GetValue(value);
            if (candidate is not null) return candidate;
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0 || property.Name is "RawRepresentation") continue;
            object? child;
            try { child = property.GetValue(value); } catch { continue; }
            if (child is null) continue;
            if (child is IEnumerable sequence and not string)
            {
                foreach (var item in sequence.Cast<object?>().Take(20))
                {
                    var found = FindUsage(item, depth + 1, visited);
                    if (found is not null) return found;
                }
            }
            else if (!property.PropertyType.IsPrimitive && property.PropertyType.Namespace?.StartsWith("Microsoft", StringComparison.Ordinal) == true)
            {
                var found = FindUsage(child, depth + 1, visited);
                if (found is not null) return found;
            }
        }
        return null;
    }

    private static long? ReadLong(object usage, params string[] names)
    {
        foreach (var name in names)
        {
            var property = usage.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var value = property?.GetValue(usage);
            if (value is not null && long.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                return result;
        }
        return null;
    }

    private static decimal? ReadDecimal(string? value) => decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
}
