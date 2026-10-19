using Microsoft.Extensions.AI;
using Xunit;

namespace RalfAI.Tests;

public sealed class StreamingTurnTests
{
    private static (string Thinking, string Answer) Split(params string[] chunks)
    {
        var splitter = new ThinkTagSplitter();
        string thinking = "", answer = "";
        void Take(IEnumerable<(bool IsThinking, string Text)> parts)
        {
            foreach (var (isThinking, text) in parts)
                if (isThinking) thinking += text; else answer += text;
        }
        foreach (var chunk in chunks) Take(splitter.Feed(chunk));
        Take(splitter.Flush());
        return (thinking, answer);
    }

    [Fact]
    public void SplitsThinkingFromAnswer() =>
        Assert.Equal(("plan it", "done"), Split("<think>plan it</think>done"));

    [Fact]
    public void HandlesTagsCutAcrossChunks() =>
        Assert.Equal(("plan it", "done"), Split("<thi", "nk>plan it</th", "ink>do", "ne"));

    [Fact]
    public void PlainTextWithLessThanIsNotSwallowed() =>
        Assert.Equal(("", "if a < b then <end>"), Split("if a < b ", "then <end>"));

    [Fact]
    public void UnterminatedThinkingIsFlushedAsThinking() =>
        Assert.Equal(("still going <", ""), Split("<think>still going <"));

    [Fact]
    public void SeparatesMessagesThatDoNotEndInANewline()
    {
        var turn = new StreamingTurn();
        var chunks = new List<StreamChunk>();
        turn.BeginMessage("m1", chunks.Add);
        turn.Add(new TextContent("before tool."), chunks.Add);
        turn.BeginMessage("m2", chunks.Add);
        turn.Add(new TextContent("after tool."), chunks.Add);
        Assert.Equal("before tool.\n\nafter tool.", turn.RawText);
    }

    [Fact]
    public void SumsUsageAcrossModelCallsAndKeepsReasoningApart()
    {
        var turn = new StreamingTurn();
        var chunks = new List<StreamChunk>();
        turn.Add(new TextReasoningContent("weighing options"), chunks.Add);
        turn.Add(new UsageContent(new UsageDetails { InputTokenCount = 100, OutputTokenCount = 10 }), chunks.Add);
        turn.Add(new TextContent("answer"), chunks.Add);
        turn.Add(new UsageContent(new UsageDetails { InputTokenCount = 120, OutputTokenCount = 5 }), chunks.Add);

        Assert.Equal("weighing options", turn.ReasoningSummary);
        Assert.Equal("answer", turn.RawText);
        Assert.Equal(220, turn.Usage!.InputTokenCount);
        Assert.Equal(15, turn.Usage.OutputTokenCount);
        Assert.Equal(StreamKind.Reasoning, chunks[0].Kind);
        Assert.Equal(StreamKind.Answer, chunks[1].Kind);
    }
}

public sealed class ReasoningSummaryFormattingTests
{
    [Fact]
    public void GluedHeadingsStartNewParagraphs()
    {
        var turn = new StreamingTurn();
        var chunks = new List<StreamChunk>();
        foreach (var piece in new[] { "**Making a call**", "Using a tool", "**Ending the process**", "No further action" })
            turn.Add(new Microsoft.Extensions.AI.TextReasoningContent(piece), chunks.Add);

        Assert.Equal("**Making a call**Using a tool\n\n**Ending the process**No further action", turn.ReasoningSummary);
    }

    [Fact]
    public void ClosingMarkerOfAHeadingSplitAcrossChunksIsNotABreak()
    {
        var turn = new StreamingTurn();
        var chunks = new List<StreamChunk>();
        foreach (var piece in new[] { "**Making", " a call", "**", "text" })
            turn.Add(new Microsoft.Extensions.AI.TextReasoningContent(piece), chunks.Add);

        Assert.Equal("**Making a call**text", turn.ReasoningSummary);
    }
}
