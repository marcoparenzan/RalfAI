using Xunit;

namespace RalfAI.Tests;

public sealed class CostHistoryStoreTests
{
    [Fact]
    public async Task AppendsAndReadsUsageEntries()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ralf-cost-{Guid.NewGuid():N}");
        try
        {
            var store = new CostHistoryStore(root);
            await store.AppendAsync(new(DateTimeOffset.UtcNow, "openai", "model", 10, 5, 15, 0.001m, 1.2), TestContext.Current.CancellationToken);
            var entries = store.ReadAll();
            Assert.Single(entries);
            Assert.Equal(0.001m, entries[0].EstimatedCostUsd);
            Assert.True(File.Exists(store.Path));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
