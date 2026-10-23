using Xunit;

namespace RalfAI.Tests;

public sealed class PromptLogTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ralf-plog-{Guid.NewGuid():N}");
    private static readonly DateTimeOffset At = new(2026, 10, 23, 16, 30, 0, TimeSpan.Zero);

    public PromptLogTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string Read() => File.ReadAllText(Path.Combine(_root, "PROMPTS.md")).Replace("\r\n", "\n");

    [Fact]
    public void FirstRequestCreatesTheFileWithAHeaderAndEntryOne()
    {
        Assert.Equal(1, PromptLog.Append(_root, "Crea una console app", now: At));

        var text = Read();
        Assert.StartsWith("# Prompts", text);
        Assert.Contains("## 1. \"Crea una console app\"", text);
        Assert.Contains("_2026-10-23 16:30_", text);
    }

    [Fact]
    public void NumberingContinuesAcrossRequests()
    {
        PromptLog.Append(_root, "uno", now: At);
        PromptLog.Append(_root, "due", now: At);
        Assert.Equal(3, PromptLog.Append(_root, "tre", now: At));

        var text = Read();
        Assert.Contains("## 2. \"due\"", text);
        Assert.Contains("## 3. \"tre\"", text);
    }

    [Fact]
    public void NumberingContinuesFromEntriesWrittenInEitherOlderFormat()
    {
        File.WriteAllText(Path.Combine(_root, "PROMPTS.md"),
            "# Prompts\n\n1. Realizzare un agente.\n2. \"Aggiungi i test\"\n\n## 30. \"Aggiungi lo scenario RUST\"\n\nUn paragrafo.\n");

        Assert.Equal(31, PromptLog.Append(_root, "prossima richiesta", now: At));
        Assert.Contains("## 31. \"prossima richiesta\"", Read());
    }

    [Fact]
    public void LongOrMultiLineRequestsKeepTheirFullTextBelowAShortTitle()
    {
        var request = "Prima riga della richiesta\nseconda riga con altri dettagli\nterza riga";
        PromptLog.Append(_root, request, now: At);

        var text = Read();
        Assert.Contains("## 1. \"Prima riga della richiesta\"", text);
        Assert.Contains("seconda riga con altri dettagli", text);
        Assert.Contains("terza riga", text);
    }

    [Fact]
    public void VeryLongRequestsAreTruncatedWithANote()
    {
        PromptLog.Append(_root, new string('x', 6_000), now: At);

        var text = Read();
        Assert.Contains("richiesta troncata", text);
        Assert.True(text.Length < 6_000);
    }

    [Fact]
    public void ExistingContentIsKeptAndTheEntryStartsOnAFreshLine()
    {
        File.WriteAllText(Path.Combine(_root, "PROMPTS.md"), "# Prompts\n\n1. prima"); // no trailing newline
        PromptLog.Append(_root, "seconda", now: At);

        var text = Read();
        Assert.Contains("1. prima\n", text);
        Assert.Contains("## 2. \"seconda\"", text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n  ")]
    public void EmptyRequestsAreNotLogged(string request)
    {
        Assert.Null(PromptLog.Append(_root, request, now: At));
        Assert.False(File.Exists(Path.Combine(_root, "PROMPTS.md")));
    }

    [Fact]
    public void AnEmptyFileNameDisablesTheLog()
    {
        Assert.Null(PromptLog.Append(_root, "ciao", fileName: "", now: At));
        Assert.Empty(Directory.GetFiles(_root));
    }

    [Fact]
    public void ACustomFileNameIsHonoured()
    {
        PromptLog.Append(_root, "ciao", fileName: "LOG.md", now: At);
        Assert.True(File.Exists(Path.Combine(_root, "LOG.md")));
    }
}
