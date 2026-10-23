using System.Text;
using RalfAI.Plugins;
using Xunit;

namespace RalfAI.Tests;

public sealed class FileSystemPluginTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ralf-fs-{Guid.NewGuid():N}");
    private readonly FileSystemPlugin _fs;

    public FileSystemPluginTests()
    {
        Directory.CreateDirectory(_root);
        _fs = new FileSystemPlugin(_root);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string Put(string relative, string content, Encoding? encoding = null)
    {
        var path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, encoding ?? new UTF8Encoding(false));
        return path;
    }

    // ── EditFile ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task EditReplacesExactlyOneOccurrence()
    {
        var path = Put("a.cs", "int a = 1;\nint b = 2;\n");
        var result = await _fs.EditFile("a.cs", "int b = 2;", "int b = 3;");
        Assert.StartsWith("OK", result);
        Assert.Equal("int a = 1;\nint b = 3;\n", File.ReadAllText(path));
    }

    [Fact]
    public async Task EditRefusesAmbiguousMatchUnlessReplaceAll()
    {
        var path = Put("a.txt", "x\nx\nx\n");
        var refused = await _fs.EditFile("a.txt", "x", "y");
        Assert.StartsWith("ERROR", refused);
        Assert.Contains("3 times", refused);
        Assert.Equal("x\nx\nx\n", File.ReadAllText(path));

        Assert.StartsWith("OK", await _fs.EditFile("a.txt", "x", "y", replaceAll: true));
        Assert.Equal("y\ny\ny\n", File.ReadAllText(path));
    }

    [Fact]
    public async Task EditReportsMissingTextWithAHintAndLeavesTheFileAlone()
    {
        var path = Put("a.txt", "alpha\n    beta\ngamma\n");
        var result = await _fs.EditFile("a.txt", "beta\ndelta", "x");
        Assert.StartsWith("ERROR", result);
        Assert.Contains("line 2", result);
        Assert.Equal("alpha\n    beta\ngamma\n", File.ReadAllText(path));
    }

    [Fact]
    public async Task EditMatchesFilesWithWindowsLineEndingsAndKeepsThem()
    {
        var path = Put("w.txt", "one\r\ntwo\r\nthree\r\n");
        Assert.StartsWith("OK", await _fs.EditFile("w.txt", "one\ntwo", "ONE\nTWO"));
        Assert.Equal("ONE\r\nTWO\r\nthree\r\n", File.ReadAllText(path));
    }

    [Fact]
    public async Task EditKeepsAByteOrderMark()
    {
        var path = Put("bom.cs", "class A {}", new UTF8Encoding(true));
        await _fs.EditFile("bom.cs", "A", "B");
        var bytes = File.ReadAllBytes(path);
        Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
        Assert.Equal("class B {}", Encoding.UTF8.GetString(bytes[3..]));
    }

    [Fact]
    public async Task WriteFileCreatesWithoutBomButKeepsAnExistingOne()
    {
        await _fs.WriteFile("new.md", "hi");
        Assert.NotEqual(0xEF, File.ReadAllBytes(Path.Combine(_root, "new.md"))[0]);

        var path = Put("old.cs", "x", new UTF8Encoding(true));
        await _fs.WriteFile("old.cs", "y");
        Assert.Equal(0xEF, File.ReadAllBytes(path)[0]);
    }

    // ── ReplaceInFiles ───────────────────────────────────────────────────────

    [Fact]
    public async Task ReplaceInFilesRenamesAcrossFilesAndReportsCounts()
    {
        var a = Put("src/A.cs", "Compute(1); Compute(2);");
        var b = Put("src/B.cs", "int Compute(int x) => x;");
        var other = Put("notes.md", "Compute stays here");

        var result = await _fs.ReplaceInFiles("Compute", "Evaluate", "**/*.cs");

        Assert.StartsWith("OK — 3 replacements in 2 files", result);
        Assert.Equal("Evaluate(1); Evaluate(2);", File.ReadAllText(a));
        Assert.Equal("int Evaluate(int x) => x;", File.ReadAllText(b));
        Assert.Equal("Compute stays here", File.ReadAllText(other));
    }

    [Fact]
    public async Task ReplaceInFilesLiteralModeDoesNotInterpretRegexOrDollarSigns()
    {
        var path = Put("a.txt", "price (x) is $5");
        await _fs.ReplaceInFiles("(x)", "$1", "*.txt");
        Assert.Equal("price $1 is $5", File.ReadAllText(path));
    }

    [Fact]
    public async Task ReplaceInFilesRegexModeSupportsGroups()
    {
        var path = Put("a.txt", "v1.2 and v3.4");
        await _fs.ReplaceInFiles(@"v(\d)\.(\d)", "V$1_$2", useRegex: true);
        Assert.Equal("V1_2 and V3_4", File.ReadAllText(path));
    }

    [Fact]
    public async Task ReplaceInFilesKeepsBomAndSkipsBuildFolders()
    {
        var withBom = Put("a.cs", "old", new UTF8Encoding(true));
        var built = Put("bin/Debug/x.cs", "old");

        await _fs.ReplaceInFiles("old", "new");

        Assert.Equal(0xEF, File.ReadAllBytes(withBom)[0]);
        Assert.Equal("old", File.ReadAllText(built));
    }

    [Fact]
    public async Task ReplaceInFilesSaysSoWhenNothingMatches()
    {
        Put("a.txt", "hello");
        Assert.StartsWith("No matches", await _fs.ReplaceInFiles("zzz", "y"));
        Assert.StartsWith("ERROR", await _fs.ReplaceInFiles("(", "y", useRegex: true));
    }

    // ── ReadFile ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReadFileCanReturnALineRange()
    {
        Put("l.txt", string.Join('\n', Enumerable.Range(1, 10).Select(i => $"line{i}")));
        var result = await _fs.ReadFile("l.txt", 3, 5);
        Assert.StartsWith("[l.txt, lines 3-5 of 10]", result);
        Assert.Contains("line3\nline4\nline5", result);
        Assert.DoesNotContain("line6", result);
    }

    [Fact]
    public async Task ReadFileCutsVeryLongFilesAndSaysHowToContinue()
    {
        Put("big.txt", new string('x', 150_000));
        var result = await _fs.ReadFile("big.txt");
        Assert.True(result.Length < 101_000);
        Assert.Contains("startLine/endLine", result);
    }

    // ── ListFiles / FindFiles / SearchFiles ──────────────────────────────────

    [Fact]
    public async Task ListFilesSkipsBuildAndVcsFolders()
    {
        Put("src/Program.cs", "x");
        Put("bin/Debug/app.dll", "x");
        Put("obj/x.cache", "x");
        Put(".git/config", "x");
        Put("node_modules/p/index.js", "x");

        var listing = await _fs.ListFiles();
        Assert.Contains("src/Program.cs", listing);
        Assert.DoesNotContain("bin", listing);
        Assert.DoesNotContain("obj", listing);
        Assert.DoesNotContain(".git", listing);
        Assert.DoesNotContain("node_modules", listing);
    }

    [Fact]
    public async Task ListFilesSkipsCargoTargetOnlyNextToACargoManifest()
    {
        Put("rust/Cargo.toml", "x");
        Put("rust/target/debug/app", "x");
        Put("other/target/keep.txt", "x");

        var listing = await _fs.ListFiles();
        Assert.DoesNotContain("rust/target", listing);
        Assert.Contains("other/target/keep.txt", listing);
    }

    [Fact]
    public async Task ListFilesStopsAfterTheLimit()
    {
        for (var i = 0; i < 520; i++) Put($"f/{i:D4}.txt", "x");
        var listing = await _fs.ListFiles();
        Assert.Contains("listing stopped", listing);
    }

    [Theory]
    [InlineData("**/*.cs", new[] { "src/A.cs", "src/deep/B.cs", "C.cs" })]
    [InlineData("*.md", new[] { "README.md", "docs/guide.md" })]
    [InlineData("src/*.cs", new[] { "src/A.cs" })]
    [InlineData("src/**/B.cs", new[] { "src/deep/B.cs" })]
    public async Task FindFilesMatchesGlobPatterns(string pattern, string[] expected)
    {
        foreach (var f in new[] { "src/A.cs", "src/deep/B.cs", "C.cs", "README.md", "docs/guide.md", "bin/skip.cs" }) Put(f, "x");
        var found = (await _fs.FindFiles(pattern)).Split('\n').OrderBy(x => x).ToArray();
        Assert.Equal(expected.OrderBy(x => x).ToArray(), found);
    }

    [Fact]
    public async Task SearchFilesReturnsPathLineAndText()
    {
        Put("a.cs", "class A\n{\n    void Run() {}\n}\n");
        Put("b.md", "nothing here");
        var result = await _fs.SearchFiles("void run");
        Assert.Contains("a.cs:3: void Run() {}", result);
        Assert.Contains("1 match in 1 file", result);
    }

    [Fact]
    public async Task SearchFilesHonoursTheGlobAndSkipsBinaryFiles()
    {
        Put("a.cs", "needle");
        Put("b.txt", "needle");
        File.WriteAllBytes(Path.Combine(_root, "c.bin"), [0x6e, 0x65, 0x00, 0x6e, 0x65, 0x65, 0x64, 0x6c, 0x65]);

        var onlyCs = await _fs.SearchFiles("needle", "*.cs");
        Assert.Contains("a.cs:1", onlyCs);
        Assert.DoesNotContain("b.txt", onlyCs);

        var all = await _fs.SearchFiles("needle");
        Assert.DoesNotContain("c.bin", all);
    }

    [Fact]
    public async Task SearchFilesFallsBackToLiteralTextForAnInvalidRegex()
    {
        Put("a.txt", "call foo( now");
        var result = await _fs.SearchFiles("foo(");
        Assert.Contains("a.txt:1", result);
        Assert.Contains("literal text", result);
    }

    // ── Safety ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task PathsOutsideTheRootAreBlockedIncludingSiblingsWithTheSamePrefix()
    {
        var sibling = _root + "-other";
        Directory.CreateDirectory(sibling);
        try
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _fs.WriteFile("../" + Path.GetFileName(sibling) + "/x.txt", "x"));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _fs.ReadFile("../../etc/passwd"));
            Assert.False(File.Exists(Path.Combine(sibling, "x.txt")));
        }
        finally { Directory.Delete(sibling, true); }
    }

    [Fact]
    public void EveryContextGetsTheFullFileToolSet()
    {
        var names = _fs.CreateTools().Select(t => t.Name).ToList();
        Assert.Equal(
            ["FileSystem_WriteFile", "FileSystem_EditFile", "FileSystem_ReplaceInFiles", "FileSystem_ReadFile", "FileSystem_ListFiles", "FileSystem_FindFiles", "FileSystem_SearchFiles", "FileSystem_DeleteFile"],
            names);
    }
}
