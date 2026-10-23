using RalfAI.Plugins;
using Xunit;

namespace RalfAI.Tests;

public sealed class UndoJournalTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ralf-undo-{Guid.NewGuid():N}");
    private readonly UndoJournal _journal;
    private readonly FileSystemPlugin _fs;

    public UndoJournalTests()
    {
        Directory.CreateDirectory(_root);
        _journal = new UndoJournal(_root, keep: 3);
        _fs = new FileSystemPlugin(_root);
    }

    public void Dispose()
    {
        _journal.Dispose();
        Directory.Delete(_root, recursive: true);
    }

    private string P(string rel) => Path.Combine(_root, rel.Replace('/', Path.DirectorySeparatorChar));

    private void Put(string rel, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(P(rel))!);
        File.WriteAllText(P(rel), content);
    }

    // ── Tools ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UndoRestoresModifiedDeletedAndRemovesCreatedFiles()
    {
        Put("keep.txt", "one\ntwo\nthree\n");
        Put("gone.txt", "bye\n");

        _journal.BeginTurn("change things");
        await _fs.EditFile("keep.txt", "two", "TWO");
        await _fs.DeleteFile("gone.txt");
        await _fs.WriteFile("src/new.txt", "fresh\n");
        var changes = _journal.EndTurn();

        Assert.NotNull(changes);
        Assert.Equal(3, changes!.Files.Count);
        Assert.Equal(FileChangeKind.Modified, changes.Files.Single(f => f.Path == "keep.txt").Kind);
        Assert.Equal(FileChangeKind.Deleted, changes.Files.Single(f => f.Path == "gone.txt").Kind);
        Assert.Equal(FileChangeKind.Created, changes.Files.Single(f => f.Path == "src/new.txt").Kind);

        var report = _journal.Undo();

        Assert.Equal("one\ntwo\nthree\n", File.ReadAllText(P("keep.txt")));
        Assert.Equal("bye\n", File.ReadAllText(P("gone.txt")));
        Assert.False(File.Exists(P("src/new.txt")));
        Assert.False(Directory.Exists(P("src")), "the empty folder the agent created is removed too");
        Assert.Empty(report.Skipped);
        Assert.Equal(0, _journal.Count);
    }

    [Fact]
    public async Task OnlyTheFirstChangeToAFileIsSavedSoUndoGoesBackToTheStartOfTheTurn()
    {
        Put("a.txt", "original");

        _journal.BeginTurn("two edits");
        await _fs.WriteFile("a.txt", "first");
        await _fs.WriteFile("a.txt", "second");
        _journal.EndTurn();
        _journal.Undo();

        Assert.Equal("original", File.ReadAllText(P("a.txt")));
    }

    [Fact]
    public async Task ReplaceInFilesChangesAreJournalled()
    {
        Put("a.cs", "Compute()");
        Put("b.cs", "Compute()");

        _journal.BeginTurn("rename");
        await _fs.ReplaceInFiles("Compute", "Evaluate");
        var changes = _journal.EndTurn();

        Assert.Equal(2, changes!.Files.Count);
        _journal.Undo();
        Assert.Equal("Compute()", File.ReadAllText(P("a.cs")));
        Assert.Equal("Compute()", File.ReadAllText(P("b.cs")));
    }

    [Fact]
    public async Task ATurnThatChangesNothingLeavesNoCheckpoint()
    {
        Put("a.txt", "same");

        _journal.BeginTurn("looked around");
        await _fs.ReadFile("a.txt");
        await _fs.WriteFile("a.txt", "same");
        var changes = _journal.EndTurn();

        Assert.Null(changes);
        Assert.Equal(0, _journal.Count);
        Assert.Null(_journal.Preview());
    }

    [Fact]
    public async Task ReportsAddedAndRemovedLines()
    {
        Put("a.txt", "1\n2\n3\n4\n");

        _journal.BeginTurn("edit");
        await _fs.EditFile("a.txt", "2\n3", "two\nthree\nthree-and-a-half");
        var file = _journal.EndTurn()!.Files.Single();

        Assert.Equal(3, file.Added);
        Assert.Equal(2, file.Removed);
    }

    // ── External commands ────────────────────────────────────────────────────

    [Fact]
    public void FilesCreatedByACommandCanBeRemovedButModifiedOnesAreOnlyReported()
    {
        Put("existing.csproj", "<Project />");

        _journal.BeginTurn("scaffold");
        Put("src/App/Program.cs", "class P {}");           // a command created it
        File.WriteAllText(P("existing.csproj"), "<Project Sdk=\"x\" />"); // a command edited it
        File.SetLastWriteTimeUtc(P("existing.csproj"), DateTime.UtcNow.AddMinutes(5));
        var changes = _journal.EndTurn()!;

        var created = changes.Files.Single(f => f.Path == "src/App/Program.cs");
        var modified = changes.Files.Single(f => f.Path == "existing.csproj");
        Assert.True(created.Restorable);
        Assert.False(modified.Restorable);

        var report = _journal.Undo();

        Assert.False(File.Exists(P("src/App/Program.cs")));
        Assert.Equal("<Project Sdk=\"x\" />", File.ReadAllText(P("existing.csproj")));
        Assert.Contains(report.Skipped, s => s.StartsWith("existing.csproj"));
    }

    [Fact]
    public async Task AFileACommandCreatedAndAToolDeletedInTheSameTurnIsNotResurrectedByUndo()
    {
        _journal.BeginTurn("scaffold then clean up");
        Put("src/App/App/Program.cs", "generated");        // `dotnet new` created it...
        await _fs.DeleteFile("src/App/App/Program.cs");      // ...and the agent removed it again
        await _fs.WriteFile("src/App/Program.cs", "final");
        var changes = _journal.EndTurn()!;

        Assert.DoesNotContain(changes.Files, f => f.Path.StartsWith("src/App/App/"));
        _journal.Undo();

        Assert.False(File.Exists(P("src/App/App/Program.cs")));
        Assert.False(File.Exists(P("src/App/Program.cs")));
        Assert.False(Directory.Exists(P("src")), "folders created during the turn are removed once empty");
    }

    [Fact]
    public async Task AToolEditOfACommandCreatedFileStillCountsAsCreated()
    {
        _journal.BeginTurn("scaffold and tweak");
        Put("src/new.csproj", "<Project />");                       // created by a command
        await _fs.EditFile("src/new.csproj", "<Project />", "<Project Sdk=\"x\" />");
        var changes = _journal.EndTurn()!;

        Assert.Equal(FileChangeKind.Created, changes.Files.Single().Kind);
        _journal.Undo();
        Assert.False(File.Exists(P("src/new.csproj")));
    }

    // ── Safety ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task FilesEditedAfterTheTurnAreNotOverwrittenByUndo()
    {
        Put("a.txt", "before");

        _journal.BeginTurn("edit");
        await _fs.WriteFile("a.txt", "by the agent");
        _journal.EndTurn();

        File.WriteAllText(P("a.txt"), "by the user afterwards");
        var report = _journal.Undo();

        Assert.Equal("by the user afterwards", File.ReadAllText(P("a.txt")));
        Assert.Contains(report.Skipped, s => s.StartsWith("a.txt") && s.Contains("edited since"));
    }

    [Fact]
    public async Task UndoStepsBackOneTurnAtATimeAndOnlyTheLastKeepAreRetained()
    {
        Put("n.txt", "0");
        for (var i = 1; i <= 5; i++)
        {
            _journal.BeginTurn($"turn {i}");
            await _fs.WriteFile("n.txt", i.ToString());
            _journal.EndTurn();
            await Task.Delay(5); // distinct checkpoint ids
        }

        Assert.Equal(3, _journal.Count);
        Assert.Equal("turn 5", _journal.Preview()!.Label);

        _journal.Undo();
        Assert.Equal("4", File.ReadAllText(P("n.txt")));
        _journal.Undo();
        Assert.Equal("3", File.ReadAllText(P("n.txt")));
        _journal.Undo();
        Assert.Equal("2", File.ReadAllText(P("n.txt")));
        Assert.Equal(0, _journal.Count);
        Assert.Empty(_journal.Undo().Restored);
    }

    [Fact]
    public void ZeroKeepDisablesTheJournal()
    {
        using var off = new UndoJournal(_root, keep: 0);
        off.BeginTurn("nothing");
        Put("x.txt", "x");
        Assert.Null(off.EndTurn());
    }

    // ── LineDiff ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(new string[0], new[] { "a", "b" }, 2, 0)]
    [InlineData(new[] { "a", "b" }, new string[0], 0, 2)]
    [InlineData(new[] { "a", "b", "c" }, new[] { "a", "b", "c" }, 0, 0)]
    [InlineData(new[] { "a", "b", "c" }, new[] { "a", "X", "c" }, 1, 1)]
    [InlineData(new[] { "a", "b", "c", "d" }, new[] { "b", "c", "d", "e", "f" }, 2, 1)]
    public void LineDiffCountsAddedAndRemovedLines(string[] before, string[] after, int added, int removed)
        => Assert.Equal((added, removed), LineDiff.Count(before, after));
}
