using Microsoft.Extensions.Configuration;
using Xunit;

namespace RalfAI.Tests;

public sealed class TurnMetricsTests
{
    [Fact]
    public void ReadsProviderUsageAndCalculatesConfiguredEstimate()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Pricing:openai:test:InputUsdPerMillionTokens"] = "2.50",
            ["Pricing:openai:test:OutputUsdPerMillionTokens"] = "10"
        }).Build();
        var response = new FakeResponse(new FakeUsage(1_000, 500, 1_500));

        var result = TurnMetrics.Read(response, config, "openai", "test");

        Assert.NotNull(result);
        Assert.Equal(1_000, result.InputTokens);
        Assert.Equal(500, result.OutputTokens);
        Assert.Equal(0.0075m, result.EstimatedCostUsd);
    }

    [Fact]
    public void ReturnsTokensWithoutCostWhenPricingIsAbsent()
    {
        var result = TurnMetrics.Read(new FakeResponse(new FakeUsage(2, 3, 5)),
            new ConfigurationBuilder().Build(), "anthropic", "model");

        Assert.Equal(5, result?.TotalTokens);
        Assert.Null(result?.EstimatedCostUsd);
    }

    private sealed record FakeResponse(FakeUsage Usage);
    private sealed record FakeUsage(long InputTokenCount, long OutputTokenCount, long TotalTokenCount);
}
