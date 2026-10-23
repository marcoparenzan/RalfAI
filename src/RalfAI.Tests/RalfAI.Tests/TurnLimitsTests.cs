using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace RalfAI.Tests;

public sealed class TurnLimitsTests
{
    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(v => v.Key, v => (string?)v.Value)).Build();

    [Fact]
    public void DefaultsApplyWhenNothingIsConfigured()
    {
        var limits = TurnLimits.FromConfig(Config());
        Assert.Equal(TurnLimits.DefaultMaxToolCalls, limits.MaxToolCalls);
        Assert.Equal(TurnLimits.DefaultMaxOutputTokens, limits.MaxOutputTokens);
        Assert.Equal(TurnLimits.DefaultMaxReasoningChars, limits.MaxReasoningChars);
    }

    [Fact]
    public void ConfigOverridesAndZeroTurnsALimitOff()
    {
        var limits = TurnLimits.FromConfig(Config(("Limits:MaxToolCalls", "5"), ("Limits:MaxOutputTokens", "0"), ("Limits:MaxReasoningChars", "1000")));
        Assert.Equal(5, limits.MaxToolCalls);
        Assert.Null(limits.CheckStream(10_000_000, 500));
        Assert.NotNull(limits.CheckStream(10, 1_001));
    }

    [Fact]
    public void NonsenseConfigFallsBackToTheDefault()
    {
        var limits = TurnLimits.FromConfig(Config(("Limits:MaxToolCalls", "many"), ("Limits:MaxOutputTokens", "-5")));
        Assert.Equal(TurnLimits.DefaultMaxToolCalls, limits.MaxToolCalls);
        Assert.Equal(TurnLimits.DefaultMaxOutputTokens, limits.MaxOutputTokens);
    }

    [Fact]
    public void StreamIsWithinLimitsUntilATokenOrReasoningThresholdIsPassed()
    {
        var limits = new TurnLimits(10, 1_000, 2_000);
        Assert.Null(limits.CheckStream(1_000, 2_000));
        Assert.Contains("output tokens", limits.CheckStream(1_001, 0));
        Assert.Contains("characters of reasoning", limits.CheckStream(0, 2_001));
    }

    [Fact]
    public void ToolCallsAreCountedPerTurnAndTheBudgetIsSoftThenHard()
    {
        var limits = new TurnLimits(3, 0, 0);
        var numbers = Enumerable.Range(0, 3).Select(_ => limits.CountToolCall()).ToList();
        Assert.Equal([1, 2, 3], numbers);
        Assert.All(numbers, n => Assert.False(limits.ToolBudgetExceeded(n)));

        var fourth = limits.CountToolCall();
        Assert.True(limits.ToolBudgetExceeded(fourth));
        Assert.False(limits.ToolBudgetIgnored(fourth));
        Assert.Contains("Do not call any more tools", limits.ToolBudgetMessage);

        Assert.True(limits.ToolBudgetIgnored(3 + TurnLimits.ToolCallGrace + 1));

        limits.StartTurn();
        Assert.Equal(0, limits.ToolCalls);
        Assert.Equal(1, limits.CountToolCall());
    }

    [Fact]
    public void ZeroToolBudgetMeansUnlimited()
    {
        var limits = new TurnLimits(0, 0, 0);
        Assert.False(limits.ToolBudgetExceeded(1_000_000));
        Assert.False(limits.ToolBudgetIgnored(1_000_000));
    }

    [Fact]
    public void ParallelToolCallsGetDistinctNumbers()
    {
        var limits = new TurnLimits(1_000, 0, 0);
        var numbers = Enumerable.Range(0, 200).AsParallel().Select(_ => limits.CountToolCall()).ToList();
        Assert.Equal(200, numbers.Distinct().Count());
    }

    [Fact]
    public void StreamingTurnTracksReasoningCharsAndOutputTokens()
    {
        var turn = new StreamingTurn();
        var chunks = new List<StreamChunk>();
        turn.Add(new TextReasoningContent(new string('r', 400)), chunks.Add);
        turn.Add(new TextContent("<think>" + new string('t', 100) + "</think>" + new string('a', 400)), chunks.Add);

        Assert.Equal(500, turn.ReasoningChars);
        Assert.Equal((400 + 7 + 100 + 8 + 400) / 4, turn.OutputTokensEstimate);

        turn.Add(new UsageContent(new UsageDetails { OutputTokenCount = 90 }), chunks.Add);
        Assert.Equal(90, turn.OutputTokensEstimate);
    }
}
