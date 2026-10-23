using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace RalfAI;

public enum PermissionMode { Ask, Allow, Deny }

public enum PermissionDecision { Allow, Ask, Deny }

public enum PermissionAnswer { AllowOnce, AllowAlways, Deny }

/// <summary>What the agent wants to run. <see cref="AlwaysPattern"/> is the rule a "always allow" answer adds for the session.</summary>
public sealed record PermissionRequest(string ToolName, string Command, string AlwaysPattern, string Why);

/// <summary>Asks the user (or whoever drives the engine) about a command. Return <see cref="PermissionAnswer.Deny"/> when nobody can be asked.</summary>
public delegate Task<PermissionAnswer> PermissionHandler(PermissionRequest request, CancellationToken cancellationToken);

/// <summary>
/// Decides whether the agent may run a command through the process tools (dotnet, cargo, rustup).
/// File tools are sandboxed to the project folder and undoable, so they are not gated; these tools can run arbitrary code.
///
/// Order of rules: <c>Permissions:Deny</c> first, then <c>Permissions:Allow</c>, then what the user answered "always" to
/// in this session, then the built-in "ask" list (running the project's code, installing software), then the built-in
/// "allow" list (build, test, scaffold, format…), and finally the mode (<c>Permissions:Mode</c>: ask by default).
/// Rules are globs over the lower-cased command line, e.g. <c>dotnet run*</c>. Mode <c>allow</c> skips every prompt
/// (only the Deny list still applies).
/// </summary>
public sealed class PermissionPolicy
{
    private static readonly string[] BuiltInAsk =
    [
        "dotnet run*", "dotnet tool*", "dotnet new install*", "dotnet new uninstall*", "dotnet nuget*", "dotnet workload*",
        "dotnet dev-certs*", "dotnet user-secrets*", "dotnet exec*",
        "cargo run*", "cargo install*", "cargo uninstall*", "cargo publish*", "cargo login*", "cargo yank*", "cargo owner*",
        "rust install-toolchain",
    ];

    private static readonly string[] BuiltInAllow =
    [
        "dotnet build*", "dotnet test*", "dotnet restore*", "dotnet clean*", "dotnet new*", "dotnet add*", "dotnet remove*",
        "dotnet sln*", "dotnet list*", "dotnet format*", "dotnet publish*", "dotnet pack*", "dotnet msbuild*", "dotnet --*", "dotnet help*",
        "cargo build*", "cargo check*", "cargo test*", "cargo new*", "cargo init*", "cargo add*", "cargo remove*", "cargo fmt*",
        "cargo clippy*", "cargo doc*", "cargo clean*", "cargo metadata*", "cargo tree*", "cargo fetch*", "cargo update*",
        "cargo --*", "cargo version*",
        "rustup show*", "rustup --*", "rustup which*", "rustup component list*", "rustup target list*", "rustup toolchain list*",
    ];

    private static readonly List<Regex> AskRules = BuiltInAsk.Select(Glob).ToList();
    private static readonly List<Regex> AllowRules = BuiltInAllow.Select(Glob).ToList();

    private readonly List<Regex> _userAllow;
    private readonly List<Regex> _userDeny;
    private readonly List<Regex> _sessionAllow = [];
    private readonly HashSet<string> _refused = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public PermissionPolicy(PermissionMode mode, IEnumerable<string>? allow = null, IEnumerable<string>? deny = null)
    {
        Mode = mode;
        _userAllow = (allow ?? []).Select(Glob).ToList();
        _userDeny = (deny ?? []).Select(Glob).ToList();
    }

    public PermissionMode Mode { get; }

    public static PermissionPolicy FromConfig(IConfiguration config)
    {
        var mode = Enum.TryParse<PermissionMode>(config["Permissions:Mode"], ignoreCase: true, out var parsed) ? parsed : PermissionMode.Ask;
        return new PermissionPolicy(mode, ReadList(config, "Permissions:Allow"), ReadList(config, "Permissions:Deny"));
    }

    private static List<string> ReadList(IConfiguration config, string key)
        => config.GetSection(key).GetChildren().Select(c => c.Value).Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!).ToList();

    // ── What a tool call would run ───────────────────────────────────────────

    /// <summary>The command a gated tool call would run, or null when the tool is not gated.</summary>
    public static string? Describe(string toolName, IEnumerable<KeyValuePair<string, object?>> arguments)
    {
        var args = arguments.ToDictionary(a => a.Key, a => a.Value, StringComparer.OrdinalIgnoreCase);
        string Text(string name) => args.TryGetValue(name, out var v) ? ArgumentText(v) : "";

        var command = toolName switch
        {
            "DotNet_RunCommand" => "dotnet " + Text("arguments"),
            "DotNet_RunDotnetNew" => string.Join(' ', new[] { "dotnet new", Text("template"), Text("name"), Text("additionalArgs") }.Where(p => p.Length > 0)),
            "DotNet_RunBuild" => ("dotnet build " + Text("projectPath")),
            "DotNet_RunTest" => ("dotnet test " + Text("projectPath")),
            "Rust_RunCargo" => "cargo " + Text("arguments"),
            "Rust_RunRustup" => "rustup " + Text("arguments"),
            "Rust_EnsureToolchain" => "rust install-toolchain",
            _ => null,
        };
        return command is null ? null : Normalize(command);
    }

    private static string ArgumentText(object? value) => value switch
    {
        null => "",
        string s => s,
        JsonElement { ValueKind: JsonValueKind.Array } array => string.Join(' ', array.EnumerateArray().Select(e => e.ToString())),
        JsonElement element => element.ToString(),
        System.Collections.IEnumerable items => string.Join(' ', items.Cast<object?>().Select(i => i?.ToString() ?? "")),
        _ => value.ToString() ?? "",
    };

    private static string Normalize(string command) => Regex.Replace(command.Trim(), @"\s+", " ");

    /// <summary>The "always allow" rule for a command: its first two words plus a wildcard (<c>cargo install*</c>).</summary>
    public static string PatternFor(string command)
    {
        var words = Normalize(command).ToLowerInvariant().Split(' ');
        return string.Join(' ', words.Take(Math.Min(2, words.Length))) + "*";
    }

    public static string WhyAsk(string command)
    {
        var c = command.ToLowerInvariant();
        if (c.StartsWith("dotnet run") || c.StartsWith("cargo run") || c.StartsWith("dotnet exec")) return "runs the project's code";
        if (c.Contains("install") || c.StartsWith("rustup") || c.StartsWith("dotnet tool") || c.StartsWith("dotnet workload")) return "downloads or installs software";
        if (c.Contains("publish") || c.Contains("login") || c.Contains("push") || c.Contains("yank")) return "talks to a package registry";
        return "is not on the list of commands allowed without asking";
    }

    // ── Decision ─────────────────────────────────────────────────────────────

    public PermissionDecision Evaluate(string command)
    {
        var c = Normalize(command).ToLowerInvariant();
        lock (_gate)
        {
            if (_refused.Contains(c) || _userDeny.Any(r => r.IsMatch(c))) return PermissionDecision.Deny;
            if (Mode == PermissionMode.Allow) return PermissionDecision.Allow;
            if (_userAllow.Any(r => r.IsMatch(c)) || _sessionAllow.Any(r => r.IsMatch(c))) return PermissionDecision.Allow;
            if (AskRules.Any(r => r.IsMatch(c))) return Mode == PermissionMode.Deny ? PermissionDecision.Deny : PermissionDecision.Ask;
            if (AllowRules.Any(r => r.IsMatch(c))) return PermissionDecision.Allow;
            return Mode == PermissionMode.Deny ? PermissionDecision.Deny : PermissionDecision.Ask;
        }
    }

    /// <summary>"Always allow" for the rest of the session.</summary>
    public void GrantSession(string pattern)
    {
        lock (_gate) _sessionAllow.Add(Glob(pattern));
    }

    /// <summary>The user said no to exactly this command: refuse it again without asking.</summary>
    public void Refuse(string command)
    {
        lock (_gate) _refused.Add(Normalize(command).ToLowerInvariant());
    }

    private static Regex Glob(string pattern)
        => new("^" + Regex.Escape(pattern.Trim().ToLowerInvariant()).Replace("\\*", ".*") + "$", RegexOptions.CultureInvariant);
}
