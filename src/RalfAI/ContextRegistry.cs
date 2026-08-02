using RalfAI.Contexts;

namespace RalfAI;

/// <summary>
/// Resolves an <see cref="IAgentContext"/> by name instead of hardcoding a single stack.
/// Lives in the exe (the composition root) because each context implementation ships in its
/// own library — Core and Abstractions must not depend back on them.
/// Add new stacks by registering them here — nothing else in the app needs to change.
/// </summary>
public static class ContextRegistry
{
    private static readonly Dictionary<string, Func<IAgentContext>> Registry =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["dotnet"]   = () => new DotNetContext(),
            ["markdown"] = () => new MarkdownContext(),
            ["html"]     = () => new HtmlAppContext(),
        };

    public const string DefaultName = "markdown";

    public static IAgentContext Create(string? name)
    {
        var key = string.IsNullOrWhiteSpace(name) ? DefaultName : name.Trim();
        if (!Registry.TryGetValue(key, out var factory))
            throw new ArgumentException(
                $"Unknown context '{name}'. Available contexts: {string.Join(", ", Registry.Keys)}");
        return factory();
    }

    /// <summary>All registered contexts, keyed by their registry name.</summary>
    public static IEnumerable<(string Key, IAgentContext Context)> All()
        => Registry.Select(kv => (kv.Key, kv.Value()));
}
