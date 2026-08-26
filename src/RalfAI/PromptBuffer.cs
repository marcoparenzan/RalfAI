using System.Text;

namespace RalfAI;

/// <summary>A cursor-aware, multi-line text buffer used by the interactive prompt editor.</summary>
public sealed class PromptBuffer
{
    private readonly StringBuilder _text = new();
    private int? _preferredColumn;

    public int Cursor { get; private set; }
    public int Length => _text.Length;
    public bool IsEmpty => _text.Length == 0;
    public string Text => _text.ToString();

    public void Insert(char value) => Insert(value.ToString());

    public void Insert(string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        value = NormalizeNewLines(value);
        _text.Insert(Cursor, value);
        Cursor += value.Length;
        _preferredColumn = null;
    }

    public void Backspace()
    {
        if (Cursor == 0) return;
        _text.Remove(--Cursor, 1);
        _preferredColumn = null;
    }

    public void Delete()
    {
        if (Cursor < _text.Length) _text.Remove(Cursor, 1);
        _preferredColumn = null;
    }

    public void MoveLeft(bool byWord = false)
    {
        Cursor = byWord ? PreviousWord(Cursor) : Math.Max(0, Cursor - 1);
        _preferredColumn = null;
    }

    public void MoveRight(bool byWord = false)
    {
        Cursor = byWord ? NextWord(Cursor) : Math.Min(_text.Length, Cursor + 1);
        _preferredColumn = null;
    }

    public void MoveHome(bool document = false)
    {
        Cursor = document ? 0 : LineStart(Cursor);
        _preferredColumn = null;
    }

    public void MoveEnd(bool document = false)
    {
        Cursor = document ? _text.Length : LineEnd(Cursor);
        _preferredColumn = null;
    }

    public void MoveVertical(int direction)
    {
        if (direction is not (-1 or 1)) throw new ArgumentOutOfRangeException(nameof(direction));
        var start = LineStart(Cursor);
        _preferredColumn ??= Cursor - start;
        if (direction < 0)
        {
            if (start == 0) return;
            var previousEnd = start - 1;
            var previousStart = LineStart(previousEnd);
            Cursor = Math.Min(previousStart + _preferredColumn.Value, previousEnd);
            return;
        }

        var end = LineEnd(Cursor);
        if (end == _text.Length) return;
        var nextStart = end + 1;
        Cursor = Math.Min(nextStart + _preferredColumn.Value, LineEnd(nextStart));
    }

    public void Clear()
    {
        _text.Clear();
        Cursor = 0;
        _preferredColumn = null;
    }

    private int LineStart(int position)
    {
        while (position > 0 && _text[position - 1] != '\n') position--;
        return position;
    }

    private int LineEnd(int position)
    {
        while (position < _text.Length && _text[position] != '\n') position++;
        return position;
    }

    private int PreviousWord(int position)
    {
        while (position > 0 && char.IsWhiteSpace(_text[position - 1])) position--;
        while (position > 0 && !char.IsWhiteSpace(_text[position - 1])) position--;
        return position;
    }

    private int NextWord(int position)
    {
        while (position < _text.Length && !char.IsWhiteSpace(_text[position])) position++;
        while (position < _text.Length && char.IsWhiteSpace(_text[position])) position++;
        return position;
    }

    private static string NormalizeNewLines(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}
