using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Xunit;
using static RalfAI.Tests.ScriptedChatClient;

namespace RalfAI.Tests;

public sealed class PermissionPolicyTests
{
    private static PermissionPolicy Ask(string[]? allow = null, string[]? deny = null) => new(PermissionMode.Ask, allow, deny);

    [Theory]
    [InlineData("dotnet build src/App.sln -c Release")]
    [InlineData("dotnet test")]
    [InlineData("dotnet new console -n App --framework net11.0")]
    [InlineData("dotnet add src/App package Newtonsoft.Json")]
    [InlineData("dotnet restore")]
    [InlineData("dotnet --info")]
    [InlineData("cargo build --release")]
    [InlineData("cargo test")]
    [InlineData("cargo new src/hello --bin")]
    [InlineData("cargo clippy -- -D warnings")]
    [InlineData("rustup show")]
    [InlineData("rustup target list --installed")]
    public void TheNormalBuildTestAndScaffoldWorkflowRunsWithoutAsking(string command)
        => Assert.Equal(PermissionDecision.Allow, Ask().Evaluate(command));

    [Theory]
    [InlineData("dotnet run --file src/Apply.cs")]
    [InlineData("dotnet run")]
    [InlineData("dotnet tool install -g something")]
    [InlineData("dotnet new install Some.Templates")]
    [InlineData("dotnet nuget push pkg.nupkg")]
    [InlineData("dotnet other.dll")]
    [InlineData("cargo run")]
    [InlineData("cargo install cargo-watch")]
    [InlineData("cargo publish")]
    [InlineData("rustup target add wasm32-unknown-unknown")]
    [InlineData("rustup toolchain install nightly")]
    [InlineData("rust install-toolchain")]
    public void RunningProjectCodeInstallingSoftwareOrUnknownCommandsAsksFirst(string command)
        => Assert.Equal(PermissionDecision.Ask, Ask().Evaluate(command));

    [Fact]
    public void MatchingIgnoresCaseAndExtraSpaces()
    {
        Assert.Equal(PermissionDecision.Allow, Ask().Evaluate("  DOTNET   Build  "));
        Assert.Equal(PermissionDecision.Ask, Ask().Evaluate("Dotnet   RUN"));
    }

    [Fact]
    public void UserAllowRulesBeatTheBuiltInAskList()
    {
        var policy = Ask(allow: ["dotnet run*"]);
        Assert.Equal(PermissionDecision.Allow, policy.Evaluate("dotnet run --project src/App"));
        Assert.Equal(PermissionDecision.Ask, policy.Evaluate("cargo run"));
    }

    [Fact]
    public void UserDenyRulesBeatEverythingExceptNothing()
    {
        var policy = Ask(allow: ["dotnet build*"], deny: ["dotnet build*prod*"]);
        Assert.Equal(PermissionDecision.Deny, policy.Evaluate("dotnet build src/prod.sln"));
        Assert.Equal(PermissionDecision.Allow, policy.Evaluate("dotnet build src/dev.sln"));
        Assert.Equal(PermissionDecision.Deny, new PermissionPolicy(PermissionMode.Allow, deny: ["cargo publish*"]).Evaluate("cargo publish"));
    }

    [Fact]
    public void AllowModeNeverAsksAndDenyModeNeverPromptsEither()
    {
        var allowAll = new PermissionPolicy(PermissionMode.Allow);
        Assert.Equal(PermissionDecision.Allow, allowAll.Evaluate("dotnet run"));
        Assert.Equal(PermissionDecision.Allow, allowAll.Evaluate("some-unknown thing"));

        var denyUnlisted = new PermissionPolicy(PermissionMode.Deny, allow: ["dotnet run*"]);
        Assert.Equal(PermissionDecision.Allow, denyUnlisted.Evaluate("dotnet build"));
        Assert.Equal(PermissionDecision.Allow, denyUnlisted.Evaluate("dotnet run"));
        Assert.Equal(PermissionDecision.Deny, denyUnlisted.Evaluate("cargo install x"));
        Assert.Equal(PermissionDecision.Deny, denyUnlisted.Evaluate("unknown"));
    }

    [Fact]
    public void AlwaysAllowCoversTheSameVerbForTheRestOfTheSession()
    {
        var policy = Ask();
        Assert.Equal("dotnet run*", PermissionPolicy.PatternFor("dotnet run --project src/App"));
        policy.GrantSession(PermissionPolicy.PatternFor("dotnet run --project src/App"));

        Assert.Equal(PermissionDecision.Allow, policy.Evaluate("dotnet run --project src/Other"));
        Assert.Equal(PermissionDecision.Ask, policy.Evaluate("dotnet tool install x"));
    }

    [Fact]
    public void ARefusedCommandIsRefusedAgainWithoutAsking()
    {
        var policy = Ask();
        policy.Refuse("dotnet run --file x.cs");
        Assert.Equal(PermissionDecision.Deny, policy.Evaluate("dotnet  RUN --file x.cs"));
        Assert.Equal(PermissionDecision.Ask, policy.Evaluate("dotnet run --file y.cs"));
    }

    [Fact]
    public void ConfigIsReadFromThePermissionsSection()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Permissions:Mode"] = "deny",
            ["Permissions:Allow:0"] = "dotnet run*",
            ["Permissions:Deny:0"] = "dotnet build*secret*",
        }).Build();

        var policy = PermissionPolicy.FromConfig(config);

        Assert.Equal(PermissionMode.Deny, policy.Mode);
        Assert.Equal(PermissionDecision.Allow, policy.Evaluate("dotnet run"));
        Assert.Equal(PermissionDecision.Deny, policy.Evaluate("dotnet build secret.sln"));
        Assert.Equal(PermissionMode.Ask, PermissionPolicy.FromConfig(new ConfigurationBuilder().Build()).Mode);
    }

    [Fact]
    public void ToolCallsAreDescribedAsTheCommandTheyWouldRun()
    {
        static IEnumerable<KeyValuePair<string, object?>> Args(params (string, object?)[] a) => a.Select(x => new KeyValuePair<string, object?>(x.Item1, x.Item2));

        Assert.Equal("dotnet add package x", PermissionPolicy.Describe("DotNet_RunCommand", Args(("arguments", "add package x"))));
        Assert.Equal("dotnet new console App --framework net11.0",
            PermissionPolicy.Describe("DotNet_RunDotnetNew", Args(("template", "console"), ("name", "App"), ("additionalArgs", "--framework net11.0"))));
        Assert.Equal("dotnet build src/App.sln", PermissionPolicy.Describe("DotNet_RunBuild", Args(("projectPath", "src/App.sln"))));
        Assert.Equal("cargo test --release", PermissionPolicy.Describe("Rust_RunCargo", Args(("arguments", new[] { "test", "--release" }))));
        Assert.Equal("rustup target add wasm32-unknown-unknown",
            PermissionPolicy.Describe("Rust_RunRustup", Args(("arguments", System.Text.Json.JsonDocument.Parse("""["target","add","wasm32-unknown-unknown"]""").RootElement))));
        Assert.Equal("rust install-toolchain", PermissionPolicy.Describe("Rust_EnsureToolchain", Args()));
        Assert.Null(PermissionPolicy.Describe("FileSystem_WriteFile", Args(("relativePath", "a.txt"))));
        Assert.Null(PermissionPolicy.Describe("Web_FetchUrl", Args(("url", "https://example.com"))));
    }
}

/// <summary>The gate inside the turn loop: what the model sees, what actually runs, who gets asked.</summary>
public sealed class EnginePermissionTests
{
    private sealed class Recorder
    {
        public List<string> Ran { get; } = [];
        public AITool Tool => AIFunctionFactory.Create((string arguments) => { Ran.Add(arguments); return "ran: " + arguments; }, name: "DotNet_RunCommand");
    }

    private static Dictionary<string, object?> Cmd(string arguments) => new() { ["arguments"] = arguments };

    private static ScriptedChatClient CallsThenAnswers(params string[] commands) => Of(call => call.Index == 0
        ? [ToolCalls(commands.Select(c => ("DotNet_RunCommand", Cmd(c))).ToArray())]
        : [Text("fatto")]);

    [Fact]
    public async Task AllowedCommandsRunWithoutAnyQuestion()
    {
        var recorder = new Recorder();
        var asked = new List<PermissionRequest>();
        using var h = await EngineHarness.CreateAsync(CallsThenAnswers("build src/App.sln", "test"), context: new StubContext(recorder.Tool));
        h.Engine.Permission = (request, _) => { asked.Add(request); return Task.FromResult(PermissionAnswer.Deny); };

        await h.Engine.SendAsync("compila");

        Assert.Equal(["build src/App.sln", "test"], recorder.Ran);
        Assert.Empty(asked);
    }

    [Fact]
    public async Task ADeniedCommandDoesNotRunAndTheModelIsToldWhy()
    {
        var recorder = new Recorder();
        var client = CallsThenAnswers("run --file x.cs");
        using var h = await EngineHarness.CreateAsync(client, context: new StubContext(recorder.Tool));
        PermissionRequest? seen = null;
        h.Engine.Permission = (request, _) => { seen = request; return Task.FromResult(PermissionAnswer.Deny); };

        var result = await h.Engine.SendAsync("esegui");

        Assert.Empty(recorder.Ran);
        Assert.Equal("dotnet run --file x.cs", seen!.Command);
        Assert.Equal("dotnet run*", seen.AlwaysPattern);
        Assert.Contains("runs the project's code", seen.Why);
        Assert.Contains(ToolResults(client.Calls[1]), r => r.StartsWith("Permission denied") && r.Contains("the user said no"));
        Assert.Equal("fatto", result.FinalText);
    }

    [Fact]
    public async Task AllowOnceRunsItButAsksAgainNextTime()
    {
        var recorder = new Recorder();
        var asked = 0;
        var client = Of(call => call.Index < 2 ? [ToolCalls(("DotNet_RunCommand", Cmd("run")))] : [Text("fatto")]);
        using var h = await EngineHarness.CreateAsync(client, context: new StubContext(recorder.Tool));
        h.Engine.Permission = (_, _) => { asked++; return Task.FromResult(PermissionAnswer.AllowOnce); };

        await h.Engine.SendAsync("due volte");

        Assert.Equal(["run", "run"], recorder.Ran);
        Assert.Equal(2, asked);
    }

    [Fact]
    public async Task AlwaysAllowAsksOnlyOnceAndAlsoCoversLaterTurns()
    {
        var recorder = new Recorder();
        var asked = 0;
        var client = Of(call => call.Messages.Last().Role == ChatRole.User
            ? [ToolCalls(("DotNet_RunCommand", Cmd("run --project a")))]
            : [Text("ok")]);
        using var h = await EngineHarness.CreateAsync(client, context: new StubContext(recorder.Tool));
        h.Engine.Permission = (_, _) => { asked++; return Task.FromResult(PermissionAnswer.AllowAlways); };

        await h.Engine.SendAsync("primo");
        await h.Engine.SendAsync("secondo");

        Assert.Equal(1, asked);
        Assert.Equal(2, recorder.Ran.Count);
    }

    [Fact]
    public async Task ARefusedCommandIsNotAskedAgainEvenIfTheModelRetries()
    {
        var recorder = new Recorder();
        var asked = 0;
        var client = Of(call => call.Index < 2 ? [ToolCalls(("DotNet_RunCommand", Cmd("run")))] : [Text("rinuncio")]);
        using var h = await EngineHarness.CreateAsync(client, context: new StubContext(recorder.Tool));
        h.Engine.Permission = (_, _) => { asked++; return Task.FromResult(PermissionAnswer.Deny); };

        await h.Engine.SendAsync("insisti");

        Assert.Equal(1, asked);
        Assert.Empty(recorder.Ran);
        Assert.Contains(ToolResults(client.Calls[2]), r => r.StartsWith("Permission denied"));
    }

    [Fact]
    public async Task WithoutAWayToAskACommandThatNeedsApprovalIsRefused()
    {
        var recorder = new Recorder();
        var client = CallsThenAnswers("run");
        using var h = await EngineHarness.CreateAsync(client, context: new StubContext(recorder.Tool));

        await h.Engine.SendAsync("esegui");

        Assert.Empty(recorder.Ran);
        Assert.Contains(ToolResults(client.Calls[1]), r => r.Contains("cannot ask") && r.Contains("Permissions:Mode"));
    }

    [Fact]
    public async Task AllowModeRunsEverythingWithoutAsking()
    {
        var recorder = new Recorder();
        var asked = false;
        using var h = await EngineHarness.CreateAsync(CallsThenAnswers("run", "exec thing.dll"),
            new() { ["Permissions:Mode"] = "allow" }, new StubContext(recorder.Tool));
        h.Engine.Permission = (_, _) => { asked = true; return Task.FromResult(PermissionAnswer.Deny); };

        await h.Engine.SendAsync("tutto");

        Assert.Equal(2, recorder.Ran.Count);
        Assert.False(asked);
    }

    [Fact]
    public async Task ConfiguredAllowAndDenyRulesApply()
    {
        var recorder = new Recorder();
        var client = CallsThenAnswers("run", "build prod.sln");
        using var h = await EngineHarness.CreateAsync(client,
            new() { ["Permissions:Allow:0"] = "dotnet run*", ["Permissions:Deny:0"] = "dotnet build*prod*" }, new StubContext(recorder.Tool));

        await h.Engine.SendAsync("regole");

        Assert.Equal(["run"], recorder.Ran);
        Assert.Contains(ToolResults(client.Calls[1]), r => r.Contains("blocked by the permission rules"));
    }

    [Fact]
    public async Task FileToolsAreNeverGated()
    {
        var client = Of(call => call.Index == 0 ? [ToolCalls(("FileSystem_WriteFile", new() { ["relativePath"] = "a.txt", ["content"] = "x" }))] : [Text("ok")]);
        using var h = await EngineHarness.CreateAsync(client);
        h.Engine.Permission = (_, _) => throw new InvalidOperationException("must not be asked");

        await h.Engine.SendAsync("scrivi");

        Assert.True(File.Exists(h.PathOf("a.txt")));
    }

    [Fact]
    public async Task ADeniedCommandIsLoggedAsNotRun()
    {
        var recorder = new Recorder();
        using var h = await EngineHarness.CreateAsync(CallsThenAnswers("run"), context: new StubContext(recorder.Tool));
        h.Engine.Permission = (_, _) => Task.FromResult(PermissionAnswer.Deny);

        await h.Engine.SendAsync("esegui");

        Assert.Contains("NOT RUN: Permission denied", File.ReadAllText(Path.Combine(h.Folder, ".ralfai", "session.md")));
    }
}
