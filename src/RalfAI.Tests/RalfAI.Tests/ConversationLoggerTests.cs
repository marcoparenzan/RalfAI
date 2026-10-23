using Xunit;
using static RalfAI.Tests.ScriptedChatClient;

namespace RalfAI.Tests;

public sealed class ConversationLoggerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ralf-log-{Guid.NewGuid():N}");

    public ConversationLoggerTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string Ralfai(string name) => Path.Combine(_root, ".ralfai", name);

    // ── History in session.jsonl ─────────────────────────────────────────────

    [Fact]
    public void UserAndAgentMessagesRoundTripThroughTheJsonlHistory()
    {
        var log = new ConversationLogger(_root);
        log.LogUser("ciao");
        log.LogTool("FileSystem.ReadFile", "relativePath=a.txt", "contenuto del file");
        log.LogAssistant("pensiero privato", "risposta");

        var history = new ConversationLogger(_root).LoadExistingHistory();

        Assert.Equal([("user", "ciao"), ("assistant", "risposta")], history);
    }

    [Fact]
    public void MessagesWithNewlinesQuotesAndUnicodeSurviveTheRoundTrip()
    {
        var log = new ConversationLogger(_root);
        log.LogUser("riga uno\nriga \"due\"\ttab — è ✓");

        Assert.Equal("riga uno\nriga \"due\"\ttab — è ✓", new ConversationLogger(_root).LoadExistingHistory().Single().Content);
    }

    [Fact]
    public void AHalfWrittenLineDoesNotLoseTheRestOfTheHistory()
    {
        var log = new ConversationLogger(_root);
        log.LogUser("prima");
        File.AppendAllText(Ralfai("session.jsonl"), "{\"Role\":\"user\",\"Te");        // a crash in the middle of a write
        File.AppendAllText(Ralfai("session.jsonl"), Environment.NewLine);
        log.LogUser("dopo");

        Assert.Equal(["prima", "dopo"], new ConversationLogger(_root).LoadExistingHistory().Select(h => h.Content));
    }

    [Fact]
    public void NoHistoryYetMeansAnEmptyList() => Assert.Empty(new ConversationLogger(_root).LoadExistingHistory());

    [Fact]
    public void ResumingDoesNotNeedTheMarkdownTranscriptAnyMore()
    {
        var log = new ConversationLogger(_root);
        log.LogUser("ricordami");
        log.LogAssistant(null, "ok");
        File.Delete(Ralfai("session.md"));

        Assert.Equal(2, new ConversationLogger(_root).LoadExistingHistory().Count);
    }

    // ── Old transcripts ──────────────────────────────────────────────────────

    [Fact]
    public void AnOldMarkdownTranscriptIsConvertedTheFirstTime()
    {
        Directory.CreateDirectory(Path.Combine(_root, ".ralfai"));
        File.WriteAllText(Ralfai("session.md"),
            "# RalfAI Session\n\n> Started: 2026-01-01 10:00:00\n\n---\n\n" +
            "### USER — 10:00:01\n\nfai una cosa\n\n---\n\n" +
            "### TOOL — FileSystem.WriteFile — 10:00:02\n\nargs: x\nresult: OK\n\n---\n\n" +
            "### AGENT — 10:00:03\n\n<!-- thinking\nmmm\n-->\n\nfatto\n\n---\n\n");

        var history = new ConversationLogger(_root).LoadExistingHistory();

        Assert.Equal([("user", "fai una cosa"), ("assistant", "fatto")], history);
        Assert.True(File.Exists(Ralfai("session.jsonl")), "the converted history is stored so the next start does not parse Markdown");
    }

    [Fact]
    public void ABigOldTranscriptIsConvertedBeforeItIsRotatedAway()
    {
        Directory.CreateDirectory(Path.Combine(_root, ".ralfai"));
        File.WriteAllText(Ralfai("session.md"),
            "### USER — 10:00:01\n\nvecchia richiesta\n\n---\n\n### AGENT — 10:00:02\n\nvecchia risposta\n\n---\n\n" + new string('x', 2_000));

        var log = new ConversationLogger(_root, maxLogBytes: 500);

        Assert.Equal(2, log.LoadExistingHistory().Count);
        Assert.Single(Directory.GetFiles(Path.Combine(_root, ".ralfai"), "session.2*.md"));
    }

    // ── Transcript size ──────────────────────────────────────────────────────

    [Fact]
    public void LongToolOutputIsCutInTheTranscriptButNotInTheHistory()
    {
        var log = new ConversationLogger(_root, toolOutputChars: 100);
        log.LogTool("FileSystem.ReadFile", "relativePath=big.txt", new string('q', 5_000));

        var text = File.ReadAllText(Ralfai("session.md"));
        Assert.Contains("4,900 more characters not logged", text);
        Assert.DoesNotContain(new string('q', 101), text);
    }

    [Fact]
    public void ZeroKeepsToolOutputWhole()
    {
        var log = new ConversationLogger(_root, toolOutputChars: 0);
        log.LogTool("FileSystem.ReadFile", "a", new string('q', 5_000));
        Assert.Contains(new string('q', 5_000), File.ReadAllText(Ralfai("session.md")));
    }

    [Fact]
    public void ABigTranscriptIsRotatedAndAFreshOneStarts()
    {
        var first = new ConversationLogger(_root, maxLogBytes: 400);
        for (var i = 0; i < 20; i++) first.LogUser($"messaggio {i} " + new string('m', 40));

        var second = new ConversationLogger(_root, maxLogBytes: 400);
        second.LogUser("nuova sessione");

        Assert.Single(Directory.GetFiles(Path.Combine(_root, ".ralfai"), "session.2*.md"));
        Assert.Contains("nuova sessione", File.ReadAllText(Ralfai("session.md")));
        Assert.DoesNotContain("messaggio 0", File.ReadAllText(Ralfai("session.md")));
        Assert.Equal(21, second.LoadExistingHistory().Count);       // the history is not rotated: resuming still sees everything
    }
}

public sealed class ResumeNoteTests
{
    [Fact]
    public void NoOlderRequestsMeansNoNote() => Assert.Null(AgentEngine.EarlierRequestsNote([]));

    [Fact]
    public void OlderRequestsAreListedOldestFirstOneLineEach()
    {
        var note = AgentEngine.EarlierRequestsNote(["crea il progetto\ncon molti dettagli sotto", "aggiungi i test", "  "])!;

        Assert.Contains("1. crea il progetto | 2. aggiungi i test", note);
        Assert.DoesNotContain("molti dettagli", note);
        Assert.Contains("PROMPTS.md", note);
    }

    [Fact]
    public void OnlyTheNewestRequestsAreListedWhenThereAreMany()
    {
        var note = AgentEngine.EarlierRequestsNote(Enumerable.Range(1, 40).Select(i => $"richiesta {i}"), max: 5)!;

        Assert.Contains("the 5 most recent of 40", note);
        Assert.Contains("36. richiesta 36 | 37. richiesta 37", note);
        Assert.DoesNotContain("richiesta 35", note);
    }

    [Fact]
    public void LongRequestsAreShortened()
    {
        var note = AgentEngine.EarlierRequestsNote([new string('a', 300)])!;
        Assert.DoesNotContain(new string('a', 150), note);
        Assert.Contains("…", note);
    }
}

public sealed class EngineResumeNoteTests
{
    [Fact]
    public async Task WhenOldMessagesAreLeftOutTheModelStillGetsTheListOfEarlierRequests()
    {
        using var first = await EngineHarness.CreateAsync(Of(_ => [Text(new string('a', 4_000))]));
        for (var i = 0; i < 6; i++) await first.Engine.SendAsync($"richiesta numero {i}");

        var client = Of(_ => [Text("ok")]);
        using var second = await EngineHarness.CreateAsync(client, new() { ["History:ResumeMaxTokens"] = "2500" }, folder: first.Folder);
        await second.Engine.SendAsync("adesso");

        var firstMessage = client.Calls[0].Messages.First(m => m.Role == Microsoft.Extensions.AI.ChatRole.User).Text;
        Assert.StartsWith("[Note from RalfAI]", firstMessage);
        Assert.Contains("richiesta numero 0", firstMessage);
    }

    [Fact]
    public async Task WhenEverythingFitsThereIsNoNote()
    {
        using var first = await EngineHarness.CreateAsync(Of(_ => [Text("ok")]));
        await first.Engine.SendAsync("una sola richiesta");

        var client = Of(_ => [Text("ok")]);
        using var second = await EngineHarness.CreateAsync(client, folder: first.Folder);
        await second.Engine.SendAsync("seconda");

        Assert.DoesNotContain(client.Calls[0].Messages, m => m.Text.StartsWith("[Note from RalfAI]"));
    }

    [Fact]
    public async Task AProjectThatOnlyHasTheOldTranscriptResumesAndGainsAJsonlHistory()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"ralf-legacy-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(folder, ".ralfai"));
        File.WriteAllText(Path.Combine(folder, ".ralfai", "session.md"),
            "### USER — 09:00:00\n\nricorda che il codice e 7\n\n---\n\n### AGENT — 09:00:01\n\nmemorizzato\n\n---\n\n");
        var client = Of(_ => [Text("era 7")]);
        using var h = await EngineHarness.CreateAsync(client, folder: folder);

        Assert.Equal(2, h.Engine.ResumedMessageCount);
        await h.Engine.SendAsync("qual era il codice?");

        Assert.Contains(client.Calls[0].Messages, m => m.Text.Contains("il codice e 7"));
        Assert.True(File.Exists(Path.Combine(folder, ".ralfai", "session.jsonl")));
    }
}
