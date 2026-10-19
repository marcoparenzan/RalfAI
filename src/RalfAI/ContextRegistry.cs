using RalfAI.Contexts;
using RalfAI.Profiles;

namespace RalfAI;

/// <summary>
/// Resolves an <see cref="IAgentContext"/> by name instead of hardcoding a single stack.
/// Lives in the exe (the composition root) because each context implementation ships in its
/// own library — Core and Abstractions must not depend back on them.
/// Built-in contexts are registered here; additional ones are loaded as profile files
/// (<see cref="ProfileLoader"/>) from <c>~/.ralfai/profiles</c> and
/// <c>&lt;target&gt;/.ralfai/profiles</c>, overriding built-ins with the same name.
/// </summary>
public static class ContextRegistry
{
    private static readonly Dictionary<string, Func<IAgentContext>> BuiltIn =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["dotnet"]   = () => new DotNetContext(),
            ["markdown"] = () => new MarkdownContext(),
            ["html"]     = () => new HtmlAppContext(),
        };

    public const string DefaultName = "markdown";

    /// <summary>Called with a message for each profile file that failed to load.</summary>
    public static Action<string>? ProfileError { get; set; }

    public static IAgentContext Create(string? name, string? targetFolder = null)
    {
        var key = string.IsNullOrWhiteSpace(name) ? DefaultName : name.Trim();

        var profiles = ProfileLoader.LoadAll(targetFolder, ProfileError);
        if (profiles.TryGetValue(key, out var profile)) return profile;
        if (BuiltIn.TryGetValue(key, out var factory)) return factory();

        throw new ArgumentException(
            $"Unknown context '{name}'. Available contexts: {string.Join(", ", Keys(profiles))}");
    }

    /// <summary>All contexts (built-in and profiles), keyed by registry name.</summary>
    public static IEnumerable<(string Key, IAgentContext Context)> All(string? targetFolder = null)
    {
        var profiles = ProfileLoader.LoadAll(targetFolder, ProfileError);
        foreach (var key in Keys(profiles))
            yield return profiles.TryGetValue(key, out var p) ? (key, p) : (key, BuiltIn[key]());
    }

    /// <summary>The built-in context with this name, ignoring profile overrides.</summary>
    public static IAgentContext? CreateBuiltIn(string name)
        => BuiltIn.TryGetValue(name, out var factory) ? factory() : null;

    private static IEnumerable<string> Keys(Dictionary<string, ProfileContext> profiles)
        => BuiltIn.Keys.Concat(profiles.Keys.Where(k => !BuiltIn.ContainsKey(k)));
}
