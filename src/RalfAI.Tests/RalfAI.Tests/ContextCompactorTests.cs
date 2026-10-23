using Microsoft.Extensions.AI;
using Xunit;
using static RalfAI.Tests.ScriptedChatClient;

namespace RalfAI.Tests;

public sealed class ContextCompactorTests
{
    private static readonly CompactionOptions Small = new(RecentChars: 2_000, MinChars: 100);

    private static ChatMessage User(string text) => new(ChatRole.User, text);

    private static ChatMessage Call(string id, string tool, Dictionary<string, object?> args)
        => new(ChatRole.Assistant, [new FunctionCallContent(id, tool, args)]);

    private static ChatMessage Result(string id, string text) => new(ChatRole.Tool, [new FunctionResultContent(id, text)]);

    private static string ResultOf(IList<ChatMessage> messages, string id)
        => messages.SelectMany(m => m.Contents).OfType<FunctionResultContent>().Single(r => r.CallId == id).Result!.ToString()!;

    [Fact]
    public void RecentToolOutputStaysAndOlderBulkBecomesAStub()
    {
        var messages = new List<ChatMessage>
        {
            User("leggi"),
            Call("old", "FileSystem_ReadFile", new() { ["relativePath"] = "big.cs" }),
            Result("old", "OLD-FILE-HEADER\n" + new string('x', 5_000)),
            Call("new", "FileSystem_ReadFile", new() { ["relativePath"] = "other.cs" }),
            Result("new", "NEW-FILE-HEADER\n" + new string('y', 1_500)),
        };

        var (compacted, stats) = ContextCompactor.Compact(messages, Small);

        Assert.StartsWith("NEW-FILE-HEADER", ResultOf(compacted, "new"));
        var stub = ResultOf(compacted, "old");
        Assert.Contains("omitted to save context", stub);
        Assert.Contains("OLD-FILE-HEADER", stub);          // what it was, so the model knows what to re-read
        Assert.Contains("5,016", stub.Replace(" ", ""));   // how big it was
        Assert.Equal(1, stats.Results);
        Assert.True(stats.CharsSaved > 4_000);
    }

    [Fact]
    public void TheOriginalMessagesAreNeverChanged()
    {
        var original = Result("old", new string('x', 5_000));
        var messages = new List<ChatMessage> { User("a"), original, Result("new", new string('y', 1_900)) };

        ContextCompactor.Compact(messages, Small);

        Assert.Equal(5_000, ((FunctionResultContent)original.Contents[0]).Result!.ToString()!.Length);
        Assert.Same(original, messages[1]);
    }

    [Fact]
    public void SmallResultsAreNeverReplacedNoMatterHowOld()
    {
        var messages = new List<ChatMessage> { User("a"), Result("tiny", "OK — written a.txt"), Result("big", new string('z', 5_000)) };

        var (compacted, _) = ContextCompactor.Compact(messages, new CompactionOptions(RecentChars: 100, MinChars: 100));

        Assert.Equal("OK — written a.txt", ResultOf(compacted, "tiny"));
    }

    [Fact]
    public void TheBodyOfAFileTheAgentWroteIsReplacedInTheOldCallArguments()
    {
        var body = "line one\n" + new string('c', 4_000);
        var messages = new List<ChatMessage>
        {
            User("scrivi"),
            Call("w1", "FileSystem_WriteFile", new() { ["relativePath"] = "src/A.cs", ["content"] = body }),
            Result("w1", "OK — written src/A.cs"),
            Call("w2", "FileSystem_WriteFile", new() { ["relativePath"] = "src/B.cs", ["content"] = new string('d', 1_800) }),
            Result("w2", "OK — written src/B.cs"),
        };

        var (compacted, stats) = ContextCompactor.Compact(messages, Small);

        var first = compacted.SelectMany(m => m.Contents).OfType<FunctionCallContent>().First(c => c.CallId == "w1");
        Assert.Equal("src/A.cs", first.Arguments!["relativePath"]);                 // the path is kept
        Assert.Contains("characters omitted", (string)first.Arguments["content"]!);
        Assert.Equal(1_800, ((string)compacted.SelectMany(m => m.Contents).OfType<FunctionCallContent>().First(c => c.CallId == "w2").Arguments!["content"]!).Length);
        Assert.Equal(1, stats.Arguments);
    }

    [Fact]
    public void ImagesStayOnlyInTheLastUserMessages()
    {
        static ChatMessage WithImage(string text) => new(ChatRole.User, [new TextContent(text), new DataContent(new byte[20_000], "image/png")]);
        var messages = new List<ChatMessage> { WithImage("one"), new(ChatRole.Assistant, "ok"), WithImage("two"), WithImage("three"), WithImage("four") };

        var (compacted, stats) = ContextCompactor.Compact(messages, new CompactionOptions(KeepImagesInLastUserMessages: 2));

        Assert.Equal(2, stats.Images);
        Assert.DoesNotContain(compacted[0].Contents, c => c is DataContent);
        Assert.Contains(compacted[0].Contents, c => c is TextContent t && t.Text.Contains("image omitted"));
        Assert.DoesNotContain(compacted[2].Contents, c => c is DataContent);
        Assert.Contains(compacted[3].Contents, c => c is DataContent);
        Assert.Contains(compacted[4].Contents, c => c is DataContent);
    }

    [Fact]
    public void NothingToCompactReturnsTheSameList()
    {
        var messages = new List<ChatMessage> { User("ciao"), new(ChatRole.Assistant, "salve") };
        var (compacted, stats) = ContextCompactor.Compact(messages, Small);
        Assert.Same(messages, compacted);
        Assert.False(stats.Any);
    }

    [Fact]
    public void DisabledMeansUntouched()
    {
        var messages = new List<ChatMessage> { User("a"), Result("old", new string('x', 9_000)), Result("new", new string('y', 9_000)) };
        var (compacted, _) = ContextCompactor.Compact(messages, new CompactionOptions(Enabled: false, RecentChars: 10));
        Assert.Same(messages, compacted);
    }

    [Fact]
    public void StubsAreDeterministicSoTheCachedPromptPrefixStaysStable()
    {
        List<ChatMessage> Build() => [User("a"), Result("old", "HEAD\n" + new string('x', 5_000)), Result("new", new string('y', 1_900))];
        Assert.Equal(ResultOf(ContextCompactor.Compact(Build(), Small).Messages, "old"), ResultOf(ContextCompactor.Compact(Build(), Small).Messages, "old"));
    }
}

/// <summary>Compaction inside the turn loop: the model gets the stubs, the session keeps the originals.</summary>
public sealed class EngineCompactionTests
{
    private static Dictionary<string, object?> Read(string path) => new() { ["relativePath"] = path };

    [Fact]
    public async Task OldFileContentsAreStubbedInLaterRequestsButTheNewestStayWhole()
    {
        // Three reads of ~1.5 KB each; only ~2 KB of tool output may stay whole, so the oldest read must be stubbed
        // by the time the model is called again.
        var client = Of(call => call.Index switch
        {
            0 => [ToolCalls(("FileSystem_ReadFile", Read("a.txt")))],
            1 => [ToolCalls(("FileSystem_ReadFile", Read("b.txt")))],
            2 => [ToolCalls(("FileSystem_ReadFile", Read("c.txt")))],
            _ => [Text("letti")],
        });
        using var h = await EngineHarness.CreateAsync(client, new() { ["Context:RecentToolChars"] = "2000", ["Context:CompactMinChars"] = "100" });
        File.WriteAllText(h.PathOf("a.txt"), "AAA-HEADER\n" + new string('a', 1_500));
        File.WriteAllText(h.PathOf("b.txt"), "BBB-HEADER\n" + new string('b', 1_500));
        File.WriteAllText(h.PathOf("c.txt"), "CCC-HEADER\n" + new string('c', 1_500));

        var result = await h.Engine.SendAsync("leggi tutto");

        Assert.Equal("letti", result.FinalText);
        var lastCall = ToolResults(client.Calls[3]).ToList();
        Assert.Contains(lastCall, r => r.Contains("omitted to save context") && r.Contains("AAA-HEADER"));
        Assert.Contains(lastCall, r => r.StartsWith("CCC-HEADER") && r.Length > 1_500);
        // ...but the call before that still saw the first file whole: nothing is compacted until it has to be.
        Assert.Contains(ToolResults(client.Calls[1]), r => r.StartsWith("AAA-HEADER"));
        Assert.Contains(h.Notices, n => n.Level == NoticeLevel.Info && n.Message.StartsWith("Context compacted"));
    }

    [Fact]
    public async Task CompactionCanBeSwitchedOff()
    {
        var client = Of(call => call.Index < 3 ? [ToolCalls(("FileSystem_ReadFile", Read($"f{call.Index}.txt")))] : [Text("ok")]);
        using var h = await EngineHarness.CreateAsync(client,
            new() { ["Context:Compaction"] = "off", ["Context:RecentToolChars"] = "10", ["Context:CompactMinChars"] = "10" });
        for (var i = 0; i < 3; i++) File.WriteAllText(h.PathOf($"f{i}.txt"), new string('x', 3_000));

        await h.Engine.SendAsync("leggi");

        Assert.DoesNotContain(ToolResults(client.Calls[3]), r => r.Contains("omitted to save context"));
    }

    [Fact]
    public async Task TheSessionLogStillHasTheFullToolOutput()
    {
        var client = Of(call => call.Index < 2 ? [ToolCalls(("FileSystem_ReadFile", Read($"f{call.Index}.txt")))] : [Text("ok")]);
        using var h = await EngineHarness.CreateAsync(client, new() { ["Context:RecentToolChars"] = "100", ["Context:CompactMinChars"] = "10", ["Log:ToolOutputChars"] = "0" });
        File.WriteAllText(h.PathOf("f0.txt"), "ZERO\n" + new string('z', 2_000));
        File.WriteAllText(h.PathOf("f1.txt"), "ONE\n" + new string('o', 2_000));

        await h.Engine.SendAsync("leggi");

        var log = File.ReadAllText(Path.Combine(h.Folder, ".ralfai", "session.md"));
        Assert.Contains(new string('z', 2_000), log);
    }
}
