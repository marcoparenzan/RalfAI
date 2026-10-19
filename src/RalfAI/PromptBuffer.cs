using System.Text;

namespace RalfAI;

/// <summary>A cursor-aware, multi-line text buffer used by the interactive prompt editor.</summary>
public sealed class PromptBuffer
{
    private readonly StringBuilder _text = new();
    private int? _preferredColumn;
    private readonly Stack<(string Text, int Cursor)> _undo = new();
    private string? _lastEdit;
    private const int MaxUndo = 100;

    /// <summary>
    /// Records the state before an edit so <see cref="Undo"/> can restore it. Consecutive
    /// typing of word characters shares one entry, so undo steps back a word at a time.
    /// </summary>
    private void Checkpoint(string kind)
    {
        if (kind == _lastEdit && kind is "type" or "delete") return;
        _lastEdit = kind;
        if (_undo.Count >= MaxUndo) return;
        _undo.Push((_text.ToString(), Cursor));
    }

    public bool Undo()
    {
        if (_undo.Count == 0) return false;
        var (text, cursor) = _undo.Pop();
        _text.Clear().Append(text);
        Cursor = cursor;
        _preferredColumn = null;
        _lastEdit = null;
        return true;
    }

    public int Cursor { get; private set; }
    public int Length => _text.Length;
    public bool IsEmpty => _text.Length == 0;
    public string Text => _text.ToString();

    public void Insert(char value) => Insert(value.ToString());

    public void Insert(string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        value = NormalizeNewLines(value);
        Checkpoint(value.Length == 1 && !char.IsWhiteSpace(value[0]) ? "type" : "insert");
        InsertCore(value);
    }

    private void InsertCore(string value)
    {
        _text.Insert(Cursor, value);
        Cursor += value.Length;
        _preferredColumn = null;
    }

    public void Backspace()
    {
        if (Cursor == 0) return;
        Checkpoint("delete");
        _text.Remove(--Cursor, 1);
        _preferredColumn = null;
    }

    public void Delete()
    {
        if (Cursor < _text.Length)
        {
            Checkpoint("delete");
            _text.Remove(Cursor, 1);
        }
        _preferredColumn = null;
    }

    public void MoveLeft(bool byWord = false)
    {
        _lastEdit = null;
        Cursor = byWord ? PreviousWord(Cursor) : Math.Max(0, Cursor - 1);
        _preferredColumn = null;
    }

    public void MoveRight(bool byWord = false)
    {
        _lastEdit = null;
        Cursor = byWord ? NextWord(Cursor) : Math.Min(_text.Length, Cursor + 1);
        _preferredColumn = null;
    }

    public void MoveHome(bool document = false)
    {
        _lastEdit = null;
        Cursor = document ? 0 : LineStart(Cursor);
        _preferredColumn = null;
    }

    public void MoveEnd(bool document = false)
    {
        _lastEdit = null;
        Cursor = document ? _text.Length : LineEnd(Cursor);
        _preferredColumn = null;
    }

    public void MoveVertical(int direction)
    {
        _lastEdit = null;
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

    /// <summary>Ctrl+U: deletes from the start of the line to the cursor.</summary>
    public void DeleteToLineStart() => RemoveRange(LineStart(Cursor), Cursor);

    /// <summary>Ctrl+K: deletes from the cursor to the end of the line.</summary>
    public void DeleteToLineEnd() => RemoveRange(Cursor, LineEnd(Cursor));

    /// <summary>Ctrl+W: deletes the word before the cursor.</summary>
    public void DeleteWordBack() => RemoveRange(PreviousWord(Cursor), Cursor);

    private void RemoveRange(int start, int end)
    {
        if (end <= start) return;
        Checkpoint("remove");
        _text.Remove(start, end - start);
        Cursor = start;
        _preferredColumn = null;
    }

    public bool IsOnFirstLine => LineStart(Cursor) == 0;
    public bool IsOnLastLine => LineEnd(Cursor) == _text.Length;

    /// <summary>Replaces the whole content and puts the cursor at the end.</summary>
    public void SetText(string value)
    {
        Checkpoint("set");
        _text.Clear();
        Cursor = 0;
        _preferredColumn = null;
        InsertCore(NormalizeNewLines(value));
    }

    /// <summary>
    /// The <c>@query</c> token the cursor is inside or right after, if any: its start index
    /// (the <c>@</c>) and the text typed after it.
    /// </summary>
    public (int Start, string Query)? MentionAtCursor()
    {
        var start = Cursor;
        while (start > 0 && !char.IsWhiteSpace(_text[start - 1])) start--;
        if (start >= Cursor || _text[start] != '@') return null;
        return (start, _text.ToString(start + 1, Cursor - start - 1));
    }

    /// <summary>Replaces [start, Cursor) with <paramref name="value"/>.</summary>
    public void ReplaceToCursor(int start, string value)
    {
        Checkpoint("replace");
        _text.Remove(start, Cursor - start);
        Cursor = start;
        InsertCore(NormalizeNewLines(value));
    }

    public void Clear()
    {
        if (_text.Length > 0) Checkpoint("clear");
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
