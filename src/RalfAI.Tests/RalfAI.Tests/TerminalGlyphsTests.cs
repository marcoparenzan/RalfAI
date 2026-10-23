using System.Text;
using Xunit;

namespace RalfAI.Tests;

public sealed class TerminalGlyphsTests
{
    private static string Through(Encoding target, string text)
    {
        var sink = new StringWriter();
        var writer = new GlyphFallbackWriter(sink, target);
        writer.Write(text);
        writer.Flush();
        return sink.ToString();
    }

    [Fact]
    public void PlainAsciiPassesUntouched() => Assert.Equal("Type your message (F1 for help)", Through(Encoding.ASCII, "Type your message (F1 for help)"));

    [Fact]
    public void ArrowsBulletsAndPunctuationBecomeTheirAsciiEquivalents()
        => Assert.Equal("> Type... (@ files . / commands . ^ history) -> v <- - x", Through(Encoding.ASCII, "❯ Type… (@ files · / commands · ↑ history) → ↓ ← — ✗"));

    [Fact]
    public void EmojiAreReplacedAsAWholeNotHalfASurrogatePair()
    {
        Assert.Equal("[+] a.png", Through(Encoding.ASCII, "📎 a.png"));
        Assert.Equal("(mic) listening (spk)", Through(Encoding.ASCII, "🎤 listening 🔊"));
    }

    [Fact]
    public void ASurrogatePairSplitAcrossCharWritesIsStillOneEmoji()
    {
        var sink = new StringWriter();
        var writer = new GlyphFallbackWriter(sink, Encoding.ASCII);
        foreach (var c in "x📎y") writer.Write(c);
        Assert.Equal("x[+]y", sink.ToString());
    }

    [Fact]
    public void UnknownSymbolsBecomeAQuestionMarkRatherThanDisappearing()
        => Assert.Equal("a?b", Through(Encoding.ASCII, "a☃b"));          // a snowman

    [Fact]
    public void BoxDrawingBecomesLinesAndCorners()
        => Assert.Equal("+--+|", Through(Encoding.ASCII, "┌──┐│").Replace("++", "+"));

    [Fact]
    public void WhatTheConsoleCanShowIsKeptSoItalianAccentsSurviveOnAnOemCodePage()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var oem850 = Encoding.GetEncoding(850);

        // è, à, ù and the middle dot exist in code page 850; the arrow and the check mark do not.
        Assert.Equal("è à ù · -> v", Through(oem850, "è à ù · → ✓"));
    }

    [Fact]
    public void AskingForAsciiReplacesSymbolsEvenWhenTheConsoleCouldEncodeThemButKeepsAccentedLetters()
    {
        var sink = new StringWriter();
        var writer = new GlyphFallbackWriter(sink, new UTF8Encoding(false), replaceSymbols: true);
        writer.Write("Perché è già → ✓ ─ 📎 · ×");

        Assert.Equal("Perché è già -> v - [+] · ×", sink.ToString());
    }

    [Fact]
    public void WithoutTheFlagAUtf8ConsoleIsLeftAlone()
    {
        var sink = new StringWriter();
        new GlyphFallbackWriter(sink, new UTF8Encoding(false)).Write("→ ✓ 📎");
        Assert.Equal("→ ✓ 📎", sink.ToString());
    }

    [Fact]
    public void AnExplicitSettingDecidesRegardlessOfTheTerminal()
    {
        Assert.Equal(GlyphMode.Ascii, TerminalGlyphs.Decide("ascii", false, outputRedirected: false, _ => "x"));
        Assert.Equal(GlyphMode.Unicode, TerminalGlyphs.Decide("unicode", false, outputRedirected: false, _ => null));
        Assert.Equal(GlyphMode.Ascii, TerminalGlyphs.Decide("  ASCII ", false, outputRedirected: false, _ => null));
    }

    [Fact]
    public void RedirectedOutputIsAlwaysUtf8InAutoModeBecauseProgramsReadIt()
        => Assert.Equal(GlyphMode.Unicode, TerminalGlyphs.Decide("auto", false, outputRedirected: true, _ => null));

    [Fact]
    public void ForceAsciiBeatsAnExplicitUnicodeSetting()
        => Assert.Equal(GlyphMode.Ascii, TerminalGlyphs.Decide("unicode", forceAscii: true, outputRedirected: false, _ => null));

    [Fact]
    public void AutoRecognisesModernTerminalsAndUtf8Locales()
    {
        // Windows Terminal / VS Code on Windows, a UTF-8 locale elsewhere.
        string? modern(string name) => OperatingSystem.IsWindows() ? (name == "WT_SESSION" ? "abc" : null) : (name == "LANG" ? "it_IT.UTF-8" : null);
        Assert.Equal(GlyphMode.Unicode, TerminalGlyphs.Decide("auto", false, false, modern));

        // A legacy console (Windows) or the plain C locale (elsewhere) gets the safe fallback.
        string? legacy(string name) => OperatingSystem.IsWindows() ? null : (name == "LANG" ? "C" : null);
        Assert.Equal(GlyphMode.Ascii, TerminalGlyphs.Decide("auto", false, false, legacy));
    }
}
