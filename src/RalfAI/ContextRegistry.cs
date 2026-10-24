using RalfAI.Contexts;
using RalfAI.Profiles;

namespace RalfAI;

/// <summary>
/// Resolves an <see cref="IAgentContext"/> by name. Contexts are YAML files (<see cref="ContextStore"/>) - the ones shipped
/// with RalfAI are written to the contexts folder the first time, and can be changed like any other. The only context in code
/// is <see cref="IntegratedName"/>, which creates and edits the others.
/// </summary>
public static class ContextRegistry
{
    /// <summary>The built-in context that designs contexts.</summary>
    public const string IntegratedName = "contexts";

    public const string DefaultName = "markdown";

    /// <summary>Called with a message for each context file that failed to load.</summary>
    public static Action<string>? ProfileError { get; set; }

    public static IAgentContext Create(string? name, string? targetFolder = null)
    {
        var key = string.IsNullOrWhiteSpace(name) ? DefaultName : name.Trim();
        if (string.Equals(key, IntegratedName, StringComparison.OrdinalIgnoreCase)) return new ContextDesignerContext();

        var files = ContextStore.LoadAll(targetFolder, ProfileError);
        if (files.TryGetValue(key, out var entry)) return entry.Context;

        // Nothing chosen and the default was deleted: the context designer is always there.
        if (string.IsNullOrWhiteSpace(name)) return new ContextDesignerContext();

        throw new ArgumentException(
            $"Unknown context '{name}'. Available contexts: {string.Join(", ", All(targetFolder).Select(c => c.Key))}");
    }

    /// <summary>All contexts, the integrated one first, then the files by name.</summary>
    public static IEnumerable<(string Key, IAgentContext Context)> All(string? targetFolder = null)
    {
        yield return (IntegratedName, new ContextDesignerContext());
        foreach (var entry in ContextStore.LoadAll(targetFolder, ProfileError).Values.OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase))
            yield return (entry.Key, entry.Context);
    }
}
