using Microsoft.Extensions.AI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using static RalfAI.Tests.ScriptedChatClient;

namespace RalfAI.Tests;

/// <summary>The turn loop end to end - streaming, tools, limits, undo, images, resume - against a scripted model.</summary>
public sealed class AgentEngineTests
{
    private static Dictionary<string, object?> Write(string path, string content)
        => new() { ["relativePath"] = path, ["content"] = content };

    // ── Streaming ────────────────────────────────────────────────────────────

    [Fact]
    public async Task AnswerStreamsInPiecesAndUsageIsReported()
    {
        using var h = await EngineHarness.CreateAsync(Of(_ => [Text("Hel"), Text("lo"), Usage(10, 5)]));

        var result = await h.Engine.SendAsync("ciao");

        Assert.Null(result.Error);
        Assert.Equal("Hello", result.FinalText);
        Assert.Equal("Hello", string.Concat(h.Chunks.Where(c => c.Kind == StreamKind.Answer).Select(c => c.Text)));
        Assert.Equal(10, result.Usage!.InputTokens);
        Assert.Equal(5, result.Usage.OutputTokens);
    }

    [Fact]
    public async Task ReasoningSummaryStreamsAsReasoningAndComesBackAsThinking()
    {
        using var h = await EngineHarness.CreateAsync(Of(_ => [Reasoning("**Plan** weigh the options"), Text("Fatto")]));

        var result = await h.Engine.SendAsync("pensa");

        Assert.Equal("Fatto", result.FinalText);
        Assert.Contains("weigh the options", result.Thinking);
        Assert.Contains(h.Chunks, c => c.Kind == StreamKind.Reasoning && c.Text.Contains("Plan"));
        Assert.DoesNotContain(h.Chunks, c => c.Kind == StreamKind.Answer && c.Text.Contains("weigh"));
    }

    [Fact]
    public async Task InlineThinkTagsAreSeparatedFromTheAnswerEvenWhenSplitAcrossChunks()
    {
        using var h = await EngineHarness.CreateAsync(Of(_ => [Text("<thi"), Text("nk>hmm</th"), Text("ink>done")]));

        var result = await h.Engine.SendAsync("x");

        Assert.Equal("hmm", result.Thinking);
        Assert.Equal("done", result.FinalText);
    }

    [Fact]
    public async Task ReasoningSummaryIsRequestedFromTheModelUnlessSwitchedOff()
    {
        using var on = await EngineHarness.CreateAsync(Of(_ => [Text("ok")]));
        await on.Engine.SendAsync("x");
        Assert.Equal(ReasoningOutput.Summary, on.Client.Calls[0].Options!.Reasoning!.Output);

        using var off = await EngineHarness.CreateAsync(Of(_ => [Text("ok")]), new() { ["Reasoning:Summary"] = "off" });
        await off.Engine.SendAsync("x");
        Assert.Null(off.Client.Calls[0].Options?.Reasoning);
    }

    [Fact]
    public async Task ReasoningEffortFromTheConfigReachesTheModel()
    {
        using var h = await EngineHarness.CreateAsync(Of(_ => [Text("ok")]), new() { ["Reasoning:Effort"] = "low" });
        await h.Engine.SendAsync("x");
        Assert.Equal(ReasoningEffort.Low, h.Client.Calls[0].Options!.Reasoning!.Effort);
    }

    [Fact]
    public async Task AModelThatRejectsTheReasoningOptionIsRetriedWithoutIt()
    {
        var client = Of(call => call.Options?.Reasoning is not null
            ? throw new InvalidOperationException("HTTP 400: reasoning summary is not supported for this model")
            : [Text("fine")]);
        using var h = await EngineHarness.CreateAsync(client);

        var result = await h.Engine.SendAsync("x");

        Assert.Equal("fine", result.FinalText);
        Assert.Contains(h.Notices, n => n.Message.Contains("Reasoning summary not supported"));
        Assert.Null(client.Calls[^1].Options?.Reasoning);
    }

    // ── Tools, changes, undo ─────────────────────────────────────────────────

    [Fact]
    public async Task ToolCallsRunAndTheirResultsGoBackToTheModel()
    {
        var client = Of(call => call.Index == 0
            ? [ToolCalls(("FileSystem_WriteFile", Write("notes/a.txt", "alfa")))]
            : [Text("scritto")]);
        using var h = await EngineHarness.CreateAsync(client);

        var result = await h.Engine.SendAsync("scrivi a.txt");

        Assert.Equal("scritto", result.FinalText);
        Assert.Equal("alfa", File.ReadAllText(h.PathOf("notes/a.txt")));
        Assert.Contains(ToolResults(client.Calls[1]), r => r.StartsWith("OK"));
        Assert.Equal(2, client.Calls.Count);
    }

    [Fact]
    public async Task ChangesAreReportedAndUndoRestoresTheFolder()
    {
        var client = Of(call => call.Index == 0
            ? [ToolCalls(("FileSystem_WriteFile", Write("a.txt", "new")), ("FileSystem_WriteFile", Write("keep.txt", "changed")))]
            : [Text("ok")]);
        using var h = await EngineHarness.CreateAsync(client);
        File.WriteAllText(h.PathOf("keep.txt"), "original");

        var result = await h.Engine.SendAsync("modifica");

        Assert.Equal(2, result.Changes!.Files.Count);
        Assert.Equal(FileChangeKind.Created, result.Changes.Files.Single(f => f.Path == "a.txt").Kind);
        Assert.Same(result.Changes.Id, h.Engine.LastTurnChanges!.Id);

        var report = h.Engine.Undo();

        Assert.False(File.Exists(h.PathOf("a.txt")));
        Assert.Equal("original", File.ReadAllText(h.PathOf("keep.txt")));
        Assert.Equal(2, report.Restored.Count + report.Removed.Count);
    }

    [Fact]
    public async Task EveryRequestIsAppendedToPromptsMdByTheEngineWithoutBeingAChange()
    {
        using var h = await EngineHarness.CreateAsync(Of(_ => [Text("ok")]));

        var first = await h.Engine.SendAsync("prima richiesta");
        await h.Engine.SendAsync("seconda richiesta");

        var log = File.ReadAllText(h.PathOf("PROMPTS.md"));
        Assert.Contains("## 1. \"prima richiesta\"", log);
        Assert.Contains("## 2. \"seconda richiesta\"", log);
        Assert.Null(first.Changes);
    }

    [Fact]
    public async Task PromptLogCanBeSwitchedOff()
    {
        using var h = await EngineHarness.CreateAsync(Of(_ => [Text("ok")]), new() { ["PromptLog:File"] = "" });
        await h.Engine.SendAsync("niente log");
        Assert.False(File.Exists(h.PathOf("PROMPTS.md")));
    }

    // ── Limits ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task PastTheToolBudgetCallsAreNotRunAndTheModelIsToldToWrapUp()
    {
        var client = Of(call => call.Index == 0
            ? [ToolCalls(("FileSystem_WriteFile", Write("one.txt", "1")), ("FileSystem_WriteFile", Write("two.txt", "2")), ("FileSystem_WriteFile", Write("three.txt", "3")))]
            : [Text("fermato, resta two e three")]);
        using var h = await EngineHarness.CreateAsync(client, new() { ["Limits:MaxToolCalls"] = "1" });

        var result = await h.Engine.SendAsync("scrivi tre file");

        Assert.Null(result.Error);
        Assert.Equal("fermato, resta two e three", result.FinalText);
        Assert.Single(Directory.GetFiles(h.Folder, "*.txt"));
        Assert.Equal(2, ToolResults(client.Calls[1]).Count(r => r.Contains("Tool call limit reached")));
        Assert.Contains(h.Notices, n => n.Level == NoticeLevel.Warning && n.Message.Contains("Tool call limit"));
    }

    [Fact]
    public async Task AModelThatKeepsCallingToolsPastTheBudgetIsStopped()
    {
        var calls = Enumerable.Range(0, TurnLimits.ToolCallGrace + 3)
            .Select(i => ("FileSystem_WriteFile", Write($"f{i}.txt", "x"))).ToArray();
        using var h = await EngineHarness.CreateAsync(Of(_ => [ToolCalls(calls)]), new() { ["Limits:MaxToolCalls"] = "1" });

        var result = await h.Engine.SendAsync("insisti");

        Assert.Contains("kept calling tools", result.Error);
        Assert.Null(result.FinalText);
    }

    [Fact]
    public async Task TooMuchOutputStopsTheTurnAndItIsNotRemembered()
    {
        var chunks = Enumerable.Range(0, 20).Select(_ => Text(new string('x', 100))).ToList();
        using var h = await EngineHarness.CreateAsync(Of(_ => chunks), new() { ["Limits:MaxOutputTokens"] = "50" });

        var result = await h.Engine.SendAsync("scrivi un romanzo");

        Assert.Contains("output tokens", result.Error);
        Assert.DoesNotContain(h.Engine.Messages, m => m.Role == "user" && m.Content.Contains("romanzo"));
        Assert.Contains("[stopped]", File.ReadAllText(Path.Combine(h.Folder, ".ralfai", "session.md")));
    }

    [Fact]
    public async Task EndlessReasoningStopsTheTurn()
    {
        var chunks = Enumerable.Range(0, 30).Select(i => Reasoning($"**Making a call {i}** " + new string('r', 100))).ToList();
        using var h = await EngineHarness.CreateAsync(Of(_ => chunks), new() { ["Limits:MaxReasoningChars"] = "500", ["Limits:MaxOutputTokens"] = "0" });

        var result = await h.Engine.SendAsync("pensa per sempre");

        Assert.Contains("characters of reasoning", result.Error);
    }

    [Fact]
    public async Task ZeroDisablesEveryLimit()
    {
        var chunks = Enumerable.Range(0, 20).Select(_ => Text(new string('x', 100))).ToList();
        using var h = await EngineHarness.CreateAsync(Of(_ => chunks),
            new() { ["Limits:MaxOutputTokens"] = "0", ["Limits:MaxReasoningChars"] = "0", ["Limits:MaxToolCalls"] = "0" });

        var result = await h.Engine.SendAsync("lungo");

        Assert.Null(result.Error);
        Assert.Equal(2_000, result.FinalText!.Length);
    }

    // ── Images, cancellation, resume ─────────────────────────────────────────

    [Fact]
    public async Task ImagesAreSentAsImageContentAndNotedInTheLog()
    {
        var client = Of(_ => [Text("vedo un quadrato")]);
        using var h = await EngineHarness.CreateAsync(client);
        var path = h.PathOf("foto.png");
        using (var image = new Image<Rgba32>(8, 8)) image.SaveAsPng(path);
        var (images, warnings) = ImageAttachments.Resolve("cosa c'e in @foto.png ?", h.Folder);
        Assert.Empty(warnings);

        await h.Engine.SendAsync("cosa c'e in @foto.png ?", images);

        var sent = client.Calls[0].Messages.Last(m => m.Role == ChatRole.User);
        Assert.Contains(sent.Contents.OfType<DataContent>(), d => d.MediaType == "image/png");
        Assert.Contains("[image attached: foto.png]", File.ReadAllText(Path.Combine(h.Folder, ".ralfai", "session.md")));
    }

    [Fact]
    public async Task CancellingKeepsWhatWasAlreadyDoneUndoableAndForgetsTheRequest()
    {
        using var cts = new CancellationTokenSource();
        var client = new ScriptedChatClient(Script);
        using var h = await EngineHarness.CreateAsync(client);

        async IAsyncEnumerable<ChatResponseUpdate> Script(ScriptedCall call, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            if (call.Index == 0)
            {
                yield return ToolCalls(("FileSystem_WriteFile", Write("partial.txt", "half done")));
                yield break;
            }
            cts.CancelAfter(50);                         // the model "thinks" until the user presses Esc
            await Task.Delay(Timeout.Infinite, ct);
            yield break;
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => h.Engine.SendAsync("lavora", cts.Token));

        Assert.NotNull(h.Engine.LastTurnChanges);
        Assert.Equal("partial.txt", h.Engine.LastTurnChanges!.Files.Single().Path);
        Assert.DoesNotContain(h.Engine.Messages, m => m.Role == "user" && m.Content == "lavora");
        h.Engine.Undo();
        Assert.False(File.Exists(h.PathOf("partial.txt")));
    }

    [Fact]
    public async Task ASessionResumedLaterSendsTheEarlierConversationOnTheFirstTurn()
    {
        using var first = await EngineHarness.CreateAsync(Of(_ => [Text("Memorizzato: 42")]));
        await first.Engine.SendAsync("ricorda il numero 42");

        var client = Of(_ => [Text("Era 42")]);
        using var second = await EngineHarness.CreateAsync(client, folder: first.Folder);
        Assert.Equal(2, second.Engine.ResumedMessageCount);

        await second.Engine.SendAsync("quale numero?");

        var texts = client.Calls[0].Messages.Select(m => m.Text).ToList();
        Assert.Contains(texts, t => t.Contains("ricorda il numero 42"));
        Assert.Contains(texts, t => t.Contains("Memorizzato: 42"));
        Assert.Contains(texts, t => t.Contains("quale numero?"));
    }

    [Fact]
    public async Task OnlyTheRecentPartOfAResumedConversationIsResent()
    {
        using var first = await EngineHarness.CreateAsync(Of(_ => [Text(new string('a', 4_000))]));
        for (var i = 0; i < 5; i++) await first.Engine.SendAsync($"domanda numero {i}");

        var client = Of(_ => [Text("ok")]);
        using var second = await EngineHarness.CreateAsync(client, new() { ["History:ResumeMaxTokens"] = "2500" }, folder: first.Folder);
        await second.Engine.SendAsync("ultima");

        var sentUserTexts = client.Calls[0].Messages.Where(m => m.Role == ChatRole.User).Select(m => m.Text).ToList();
        Assert.Contains("ultima", sentUserTexts);
        Assert.DoesNotContain(sentUserTexts, t => t.Contains("domanda numero 0"));
        Assert.Contains(sentUserTexts, t => t.Contains("domanda numero 4"));
    }
}
