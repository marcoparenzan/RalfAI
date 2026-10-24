using Spectre.Console;
using Spectre.Console.Rendering;
using RalfAI.Voice;

namespace RalfAI;

/// <summary>Collects rich multi-line input and submits it on Ctrl+Enter.</summary>
public sealed class InputCollector(SpeechService? speech = null, Func<string>? targetFolder = null)
{
    private const int MaxSuggestions = 6;

    private readonly List<string> _lines = [];
    private readonly PromptHistory _history = new();
    private string? _hintOverride;
    private string? _status;

    // History browsing (↑/↓ on an empty or already-recalled prompt).
    private const int CollapsePasteLines = 6;
    private const int CollapsePasteChars = 600;

    // Long Ctrl+V pastes are shown as a placeholder token and expanded again on submit.
    private readonly Dictionary<string, string> _pastes = [];
    private int _pasteCounter;

    private int _historyIndex = -1;
    private string _draft = "";

    // @file mention popup.
    private FileIndex _files = FileIndex.Empty;
    private List<Suggestion> _suggestions = [];
    private int _suggestion;
    private int _replaceStart;
    private bool _slash;
    private int _dismissedAt = -1;

    private sealed record Suggestion(string Insert, string Label);

    // Commands the live prompt understands (the same strings the F-keys submit).
    private static readonly (string Command, string Description)[] SlashCommands =
    [
        ("/undo", "revert the changes of the last turn"),
        ("/switch-context", "change context / profile"),
        ("/switch-folder", "change target folder"),
        ("/toggle-debug", "toggle debug messages"),
        ("/speak", "read the last response aloud"),
        ("/clear", "clear the screen"),
    ];

    public string? Collect(string? status = null)
    {
        _lines.Clear();
        _status = status;
        _historyIndex = -1;
        _suggestions = [];
        _pastes.Clear();
        _pasteCounter = 0;

        var folder = targetFolder?.Invoke();
        if (folder is not null)
        {
            _history.Load(folder);
            _files = FileIndex.Build(folder);
        }

        var result = Console.IsInputRedirected ? CollectPlain() : CollectLive();
        // Commands (/speak, /clear…) aren't prompts worth recalling.
        if (result is { Length: > 0 } && !result.StartsWith('/')) _history.Add(result);
        return result;
    }

    private void UpdateSuggestions(PromptBuffer buffer)
    {
        int start;
        List<Suggestion> found;
        var text = buffer.Text;

        if (text.StartsWith('/') && buffer.Cursor == text.Length && !text.Any(char.IsWhiteSpace))
        {
            _slash = true;
            start = 0;
            found = SlashCommands
                .Where(c => c.Command.StartsWith(text, StringComparison.OrdinalIgnoreCase))
                .Select(c => new Suggestion(c.Command, $"{c.Command,-16} {c.Description}"))
                .ToList();
        }
        else if (buffer.MentionAtCursor() is { } mention)
        {
            _slash = false;
            start = mention.Start;
            found = _files.Search(mention.Query, MaxSuggestions)
                .Select(path => new Suggestion(FormatMention(path) + " ", path))
                .ToList();
        }
        else
        {
            _dismissedAt = -1;
            _suggestions = [];
            return;
        }

        if (start == _dismissedAt)
        {
            _suggestions = [];
            return;
        }

        var previous = _suggestion < _suggestions.Count ? _suggestions[_suggestion].Insert : null;
        _suggestions = found;
        _replaceStart = start;
        _suggestion = Math.Max(0, previous is null ? 0 : found.FindIndex(x => x.Insert == previous));
    }

    /// <summary>"@path", quoted when the path contains spaces.</summary>
    private static string FormatMention(string path)
        => path.Any(char.IsWhiteSpace) ? $"@\"{path}\"" : "@" + path;

    /// <summary>
    /// If the clipboard holds an image (or copied image files), inserts one @mention per image;
    /// they are attached to the message when it is sent. Returns false when there is none.
    /// </summary>
    private bool PasteImages(PromptBuffer buffer)
    {
        var folder = targetFolder?.Invoke();
        if (folder is null) return false;

        var images = ClipboardReader.TryReadImages(folder);
        if (images.Count == 0) return false;

        foreach (var image in images)
        {
            var relative = Path.GetRelativePath(folder, image);
            var shown = relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative) ? image : relative;
            buffer.Insert(FormatMention(shown.Replace('\\', '/')) + " ");
        }
        _hintOverride = $"[dim]📎 {images.Count} image{(images.Count == 1 ? "" : "s")} from the clipboard will be attached.[/]";
        return true;
    }

    private string CollapsePaste(string pasted)
    {
        var lines = pasted.Count(c => c == (char)10) + 1;
        if (lines < CollapsePasteLines && pasted.Length < CollapsePasteChars) return pasted;

        var token = $"[Pasted #{++_pasteCounter}: {lines} lines]";
        _pastes[token] = pasted.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return token;
    }

    private string ExpandPastes(string text)
    {
        foreach (var (token, content) in _pastes)
            text = text.Replace(token, content, StringComparison.Ordinal);
        return text;
    }

    private void RecallHistory(PromptBuffer buffer, int direction)
    {
        var entries = _history.Entries;
        if (entries.Count == 0) return;
        if (direction < 0)
        {
            if (_historyIndex < 0) { _draft = buffer.Text; _historyIndex = entries.Count; }
            if (_historyIndex == 0) return;
            buffer.SetText(entries[--_historyIndex]);
        }
        else
        {
            if (++_historyIndex >= entries.Count)
            {
                _historyIndex = -1;
                buffer.SetText(_draft);
            }
            else buffer.SetText(entries[_historyIndex]);
        }
    }

    private string? CollectLive()
    {
        var buffer = new PromptBuffer();
        string? result = null;
        var exit = false;

        try { Console.CursorVisible = false; } catch (IOException) { }
        while (!exit)
        {
            var showHelp = false;

            void ProcessKey(ConsoleKeyInfo key, LiveDisplayContext ctx)
            {
                var ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);
                var shift = key.Modifiers.HasFlag(ConsoleModifiers.Shift);

                if (key.Key == ConsoleKey.Enter && ctrl)
                {
                    if (buffer.IsEmpty)
                        _hintOverride = "[yellow]Buffer is empty — type something first.[/]";
                    else
                    {
                        result = ExpandPastes(buffer.Text).Trim();
                        exit = true;
                    }
                    return;
                }

                if (key.Key == ConsoleKey.F10 || (ctrl && key.Key == ConsoleKey.Q))
                {
                    exit = true;
                    return;
                }

                if ((ctrl && key.Key == ConsoleKey.V) || (shift && key.Key == ConsoleKey.Insert))
                {
                    var pasted = ClipboardReader.ReadText();
                    if (string.IsNullOrWhiteSpace(pasted) && PasteImages(buffer)) return;

                    if (pasted is null)
                        _hintOverride = "[yellow]Clipboard unavailable; use the terminal paste command.[/]";
                    else
                        buffer.Insert(CollapsePaste(pasted));
                    return;
                }

                if (ctrl && key.Key == ConsoleKey.Z)
                {
                    if (!buffer.Undo()) _hintOverride = "[yellow]Nothing to undo.[/]";
                    return;
                }

                if (ctrl)
                {
                    switch (key.Key)
                    {
                        case ConsoleKey.A: buffer.MoveHome(); return;
                        case ConsoleKey.E: buffer.MoveEnd(); return;
                        case ConsoleKey.U: buffer.DeleteToLineStart(); return;
                        case ConsoleKey.K: buffer.DeleteToLineEnd(); return;
                        case ConsoleKey.W: buffer.DeleteWordBack(); return;
                    }
                }

                if (_suggestions.Count > 0)
                {
                    switch (key.Key)
                    {
                        case ConsoleKey.UpArrow:
                            _suggestion = (_suggestion - 1 + _suggestions.Count) % _suggestions.Count;
                            return;
                        case ConsoleKey.DownArrow:
                            _suggestion = (_suggestion + 1) % _suggestions.Count;
                            return;
                        case ConsoleKey.Tab:
                        case ConsoleKey.Enter:
                            var chosen = _suggestions[_suggestion];
                            // A slash command already typed in full: Enter submits it.
                            if (_slash && key.Key == ConsoleKey.Enter && buffer.Text == chosen.Insert)
                            {
                                result = chosen.Insert;
                                exit = true;
                            }
                            else buffer.ReplaceToCursor(_replaceStart, chosen.Insert);
                            return;
                        case ConsoleKey.Escape:
                            _dismissedAt = _replaceStart;
                            return;
                    }
                }

                switch (key.Key)
                {
                    case ConsoleKey.Enter: buffer.Insert('\n'); break;
                    case ConsoleKey.Tab: buffer.Insert('\t'); break;
                    case ConsoleKey.Backspace: buffer.Backspace(); break;
                    case ConsoleKey.Delete: buffer.Delete(); break;
                    case ConsoleKey.LeftArrow: buffer.MoveLeft(ctrl); break;
                    case ConsoleKey.RightArrow: buffer.MoveRight(ctrl); break;
                    case ConsoleKey.UpArrow:
                        if (buffer.IsOnFirstLine && (buffer.IsEmpty || _historyIndex >= 0)) RecallHistory(buffer, -1);
                        else buffer.MoveVertical(-1);
                        break;
                    case ConsoleKey.DownArrow:
                        if (_historyIndex >= 0 && buffer.IsOnLastLine) RecallHistory(buffer, 1);
                        else buffer.MoveVertical(1);
                        break;
                    case ConsoleKey.Home: buffer.MoveHome(ctrl); break;
                    case ConsoleKey.End: buffer.MoveEnd(ctrl); break;
                    case ConsoleKey.Escape: buffer.Clear(); _historyIndex = -1; break;
                    case ConsoleKey.F1: showHelp = true; break;
                    case ConsoleKey.F2: DictateInto(ctx, buffer); break;
                    case ConsoleKey.F3: result = "/speak"; exit = true; break;
                    case ConsoleKey.F5: result = "/clear"; exit = true; break;
                    case ConsoleKey.F6: result = "/switch-context"; exit = true; break;
                    case ConsoleKey.F7: result = "/switch-folder"; exit = true; break;
                    case ConsoleKey.F8: result = "/toggle-debug"; exit = true; break;
                    default:
                        // Tab is a control char (handled above) — anything else that's
                        // still a control char here isn't printable text, skip it.
                        if (!char.IsControl(key.KeyChar)) buffer.Insert(key.KeyChar);
                        break;
                }
            }

            AnsiConsole.Live(RenderPrompt(buffer)).Start(ctx =>
            {
                while (!exit && !showHelp)
                {
                    ctx.UpdateTarget(RenderPrompt(buffer));
                    ctx.Refresh();

                    // Process the key that woke us up, then drain any more that are
                    // already queued (e.g. a raw terminal paste that isn't routed
                    // through Ctrl+V/Shift+Insert's atomic clipboard read below —
                    // that one bypasses this loop entirely) without re-rendering
                    // between them. Re-rendering after every single keystroke is slow
                    // enough that a fast burst can outrun it and risk losing keys
                    // once the console's input queue fills up; draining first and
                    // rendering once afterwards keeps every key and feels instant
                    // instead of laggy, with no visible difference for normal typing
                    // (a human keystroke never has another one already queued).
                    do
                    {
                        ProcessKey(Console.ReadKey(intercept: true), ctx);
                        UpdateSuggestions(buffer);
                    }
                    while (!exit && !showHelp && Console.KeyAvailable);
                }
            });

            if (showHelp) ShowHelpOverlayInteractive();
        }

        return result;
    }

    private void DictateInto(LiveDisplayContext ctx, PromptBuffer buffer)
    {
        if (speech is null)
        {
            _hintOverride = "[red]Voice non configurata — imposta AzureSpeech in appsettings.json.[/]";
            return;
        }

        ctx.UpdateTarget(new Markup("[cyan]🎤 In ascolto… parla pure — si ferma da solo alla pausa.[/]"));
        ctx.Refresh();
        var (spoken, error) = speech.RecognizeOnceAsync().GetAwaiter().GetResult();
        if (spoken is not null) buffer.Insert(spoken);
        else _hintOverride = $"[red]{Markup.Escape(error ?? "Riconoscimento non riuscito.")}[/]";
    }

    private IRenderable RenderPrompt(PromptBuffer buffer)
    {
        IRenderable prompt;
        if (buffer.IsEmpty)
            prompt = new Markup("[green]❯[/] [grey]Type your message… (@ files · / commands · ↑ history · F1 help)[/]");
        else
        {
            var before = buffer.Text[..buffer.Cursor];
            var after = buffer.Text[buffer.Cursor..];
            // A solid, explicitly-colored block instead of relying on the terminal's ambient
            // "invert" (which can be too subtle to notice depending on the color scheme) —
            // this stays clearly visible either way and doesn't need real cursor positioning.
            var withCursor = Markup.Escape(before) + "[black on green] [/]" + Markup.Escape(after);
            var lines = withCursor.Split('\n');
            prompt = new Markup(string.Join("\n", lines.Select((line, index) =>
                (index == 0 ? "[green]❯[/] " : "  ") + line)));
        }

        var hint = _hintOverride;
        _hintOverride = null;
        var rows = new List<IRenderable>();
        if (_status is not null) rows.Add(new Markup(_status));
        rows.Add(prompt);
        if (_suggestions.Count > 0)
            rows.Add(new Markup(string.Join("\n", _suggestions.Select((item, i) => i == _suggestion
                ? $"    [black on green] {Markup.Escape(item.Label)} [/]"
                : $"    [grey] {Markup.Escape(item.Label)}[/]"))));
        if (hint is not null) rows.Add(new Markup(hint));
        return rows.Count == 1 ? rows[0] : new Rows(rows);
    }

    private string? CollectPlain()
    {
        while (true)
        {
            AnsiConsole.Markup(_lines.Count == 0 ? "[bold green]you>[/] " : $"[dim]  ({_lines.Count} lines)[/] ");
            string? line;
            try { line = Console.ReadLine(); } catch { return null; }
            if (line is null) return null;

            switch (line.Trim().ToLowerInvariant())
            {
                case "exit": case "/exit": return null;
                case "/cancel":
                    _lines.Clear();
                    AnsiConsole.MarkupLine("[dim]  ↩ Buffer cleared.[/]");
                    continue;
                case "/exe":
                    if (_lines.Count > 0) return string.Join("\n", _lines).Trim();
                    AnsiConsole.MarkupLine("[dim]  Buffer is empty — type something first.[/]");
                    continue;
                case "/help": ShowHelpOverlayPlain(); continue;
                case "/speak": return "/speak";
                case "/clear": return "/clear";
                case "/switch-context": return "/switch-context";
                case "/switch-folder": return "/switch-folder";
                case "/toggle-debug": return "/toggle-debug";
                case "/undo": return "/undo";
                case "/voice":
                    if (speech is null)
                    {
                        AnsiConsole.MarkupLine("[red]  Voice non configurata — imposta AzureSpeech in appsettings.json.[/]");
                        continue;
                    }
                    AnsiConsole.MarkupLine("[cyan]  🎤 In ascolto… parla pure.[/]");
                    var (spoken, error) = speech.RecognizeOnceAsync().GetAwaiter().GetResult();
                    if (spoken is null)
                        AnsiConsole.MarkupLine($"[red]  {Markup.Escape(error ?? "Riconoscimento non riuscito.")}[/]");
                    else
                        _lines.Add(spoken);
                    continue;
                default:
                    _lines.Add(line);
                    if (_lines.Count == 1)
                        AnsiConsole.MarkupLine("[dim]  (add more lines or type [bold]/exe[/] to send)[/]");
                    continue;
            }
        }
    }

    private static void ShowHelpOverlayInteractive()
    {
        var table = new Table().RoundedBorder().BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]Key[/]").Centered()).AddColumn("[bold]What it does[/]");
        table.AddRow("[green]Ctrl+Enter[/]", "Send the current buffer");
        table.AddRow("[green]Enter[/]", "Insert a newline");
        table.AddRow("[green]← → ↑ ↓[/]", "Move the cursor; Ctrl+←/→ moves by word");
        table.AddRow("[green]Home / End[/]", "Start/end of line; add Ctrl for the whole document");
        table.AddRow("[green]Backspace / Del[/]", "Delete before/after the cursor");
        table.AddRow("[green]Ctrl+V / Shift+Ins[/]", "Paste clipboard text atomically");
        table.AddRow("[green]@[/]", "Mention a file: type @name, ↑/↓ to pick, Tab/Enter to insert, Esc to dismiss");
        table.AddRow("[green]↑ / ↓[/]", "On an empty prompt: recall previous prompts (↓ goes back to the draft)");
        table.AddRow("[green]/[/]", "Command menu: type / then ↑/↓, Tab to complete, Enter to run");
        table.AddRow("[green]Ctrl+A/E · U/K · W[/]", "Line start/end · delete to line start/end · delete previous word");
        table.AddRow("[green]Images[/]", "Ctrl+V with an image on the clipboard, or @picture.png, attaches it to the message");
        table.AddRow("[cyan]/undo[/]", "Revert the files the last turn changed (type / for the command menu)");
        table.AddRow("[green]Ctrl+Z[/]", "Undo the last edit (typing is undone a word at a time)");
        table.AddRow("[green]Ctrl+V[/]", "Paste; long pastes collapse to [[Pasted #n: N lines]] and are sent in full");
        table.AddRow("[yellow]Esc[/]", "Clear the current draft");
        table.AddRow("[cyan]F2 / F3[/]", "Dictate / read the last response aloud");
        table.AddRow("[cyan]F5 / F6 / F7 / F8[/]", "Clear screen / context / folder / debug");
        table.AddRow("[grey]F1[/]", "Show this table");
        table.AddRow("[red]F10 / Ctrl+Q[/]", "Quit RalfAI");
        table.AddRow("[yellow]Ctrl+C / Esc / Ctrl+X[/]", "While working: interrupt the current turn");
        // Drawn over the conversation, which comes back untouched when the panel closes.
        using var overlay = FullScreenConsole.Overlay();
        AnsiConsole.WriteLine();
        AnsiConsole.Write(Align.Center(table));
        AnsiConsole.WriteLine();
        AnsiConsole.Write(Align.Center(new Markup("[dim]Premi un tasto per tornare…[/]")));
        Console.ReadKey(intercept: true);
    }

    private static void ShowHelpOverlayPlain()
    {
        var table = new Table().RoundedBorder().BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]Command[/]").Centered()).AddColumn("[bold]What it does[/]");
        table.AddRow("[green]/exe[/]", "Send the accumulated buffer");
        table.AddRow("[yellow]/cancel[/]", "Clear the buffer without sending");
        table.AddRow("[cyan]/voice / /speak[/]", "Dictate / read the last response aloud");
        table.AddRow("[cyan]/clear[/]", "Clear the screen");
        table.AddRow("[cyan]/switch-context / /switch-folder[/]", "Switch context / folder");
        table.AddRow("[cyan]/toggle-debug[/]", "Toggle debug messages");
        table.AddRow("[cyan]/undo[/]", "Revert the files the last turn changed");
        table.AddRow("[grey]/help[/]", "Show this table");
        table.AddRow("[red]exit / /exit[/]", "Quit RalfAI");
        AnsiConsole.WriteLine();
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }
}
