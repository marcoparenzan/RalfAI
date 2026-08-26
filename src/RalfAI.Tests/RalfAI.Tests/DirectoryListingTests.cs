using Xunit;

namespace RalfAI.Tests;

public sealed class DirectoryListingTests
{
    [Fact]
    public void LoadListsParentThenFoldersThenFilesSortedByName()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ralf-dir-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "sub"));
        Directory.CreateDirectory(Path.Combine(root, "zeta"));
        File.WriteAllText(Path.Combine(root, "b.txt"), "hi");
        File.WriteAllText(Path.Combine(root, "a.txt"), "hello");
        try
        {
            var entries = DirectoryListing.Load(root);

            Assert.Equal("..", entries[0].Name);
            Assert.True(entries[0].IsDirectory);
            Assert.Equal(["..", "sub", "zeta", "a.txt", "b.txt"], entries.Select(e => e.Name));
            Assert.Equal(5, entries.First(e => e.Name == "a.txt").Size);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void LoadOnMissingDirectoryFallsBackToJustParent()
    {
        // DirectoryInfo.Parent resolves syntactically even for a path that doesn't exist, so
        // ".." is added before GetDirectories()/GetFiles() throw DirectoryNotFoundException
        // (an IOException) and get swallowed — same "show whatever loaded so far" behavior as
        // a permission-denied folder.
        var entries = DirectoryListing.Load(Path.Combine(Path.GetTempPath(), $"ralf-missing-{Guid.NewGuid():N}"));
        Assert.Equal([".."], entries.Select(e => e.Name));
    }

    [Fact]
    public void FormatSizeUsesPlainBytesBelowOneKilobyte()
    {
        Assert.Equal("0 B", DirectoryListing.FormatSize(0));
        Assert.Equal("512 B", DirectoryListing.FormatSize(512));
    }

    [Theory]
    [InlineData(1536, 1.5, "KB")]
    [InlineData(1024 * 1024 * 3, 3, "MB")]
    public void FormatSizeScalesUnitsAboveOneKilobyte(long bytes, double expectedValue, string expectedUnit)
        // The exact decimal separator is locale-dependent (FormatSize uses the current
        // culture), so build the expectation the same way rather than hardcoding "1.5"/"1,5".
        => Assert.Equal($"{expectedValue.ToString("0.#")} {expectedUnit}", DirectoryListing.FormatSize(bytes));

    [Fact]
    public void TruncateKeepsShortTextUnchanged()
        => Assert.Equal("short.txt", DirectoryListing.Truncate("short.txt", 20));

    [Fact]
    public void TruncateShortensLongTextFromTheStart()
    {
        var result = DirectoryListing.Truncate("a-very-long-file-name-indeed.txt", 10);
        Assert.Equal(10, result.Length);
        Assert.StartsWith("…", result);
        Assert.EndsWith("txt", result);
    }
}
