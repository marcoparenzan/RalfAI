using Xunit;

namespace RalfAI.Tests;

public sealed class PromptBufferTests
{
    [Fact]
    public void InsertsAtCursorAndDeletesOnBothSides()
    {
        var buffer = new PromptBuffer();
        buffer.Insert("helo");
        buffer.MoveLeft();
        buffer.Insert('l');
        buffer.Backspace();
        buffer.Delete();

        Assert.Equal("hel", buffer.Text);
        Assert.Equal(3, buffer.Cursor);
    }

    [Fact]
    public void InsertsMultilineClipboardTextAtomicallyAndNormalizesNewlines()
    {
        var buffer = new PromptBuffer();
        buffer.Insert("prima\r\nseconda\rterza");

        Assert.Equal("prima\nseconda\nterza", buffer.Text);
        Assert.Equal(buffer.Length, buffer.Cursor);
    }

    [Fact]
    public void NavigatesLinesWhilePreservingColumn()
    {
        var buffer = new PromptBuffer();
        buffer.Insert("abcd\nx\nabcdef");
        buffer.MoveHome(document: true);
        buffer.MoveRight();
        buffer.MoveRight();
        buffer.MoveRight();
        buffer.MoveVertical(1);
        Assert.Equal(6, buffer.Cursor);
        buffer.MoveVertical(1);

        Assert.Equal(10, buffer.Cursor);
    }

    [Fact]
    public void SupportsLineDocumentAndWordNavigation()
    {
        var buffer = new PromptBuffer();
        buffer.Insert("uno due\ntre quattro");
        buffer.MoveLeft(byWord: true);
        Assert.Equal(12, buffer.Cursor);
        buffer.MoveHome();
        Assert.Equal(8, buffer.Cursor);
        buffer.MoveEnd(document: true);
        Assert.Equal(buffer.Length, buffer.Cursor);
    }
}
