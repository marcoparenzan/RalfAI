using Spectre.Console;
using Spectre.Console.Rendering;
using RalfAI.Voice;

namespace RalfAI;

/// <summary>Collects rich multi-line input and submits it on Ctrl+Enter.</summary>
public sealed class InputCollector(SpeechService? speech = null)
{
    private readonly List<string> _lines = [];
    private string? _hintOverride;
    private string? _status;

    public string? Collect(string? status = null)
    {
        _lines.Clear();
        _status = status;
        return Console.IsInputRedirected ? CollectPlain() : CollectLive();
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
                        result = buffer.Text.Trim();
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
                    if (pasted is null)
                        _hintOverride = "[yellow]Clipboard unavailable; use the terminal paste command.[/]";
                    else
                        buffer.Insert(pasted);
                    return;
                }

                switch (key.Key)
                {
                    case ConsoleKey.Enter: buffer.Insert('\n'); break;
                    case ConsoleKey.Tab: buffer.Insert('\t'); break;
                    case ConsoleKey.Backspace: buffer.Backspace(); break;
                    case ConsoleKey.Delete: buffer.Delete(); break;
                    case ConsoleKey.LeftArrow: buffer.MoveLeft(ctrl); break;
                    case ConsoleKey.RightArrow: buffer.MoveRight(ctrl); break;
                    case ConsoleKey.UpArrow: buffer.MoveVertical(-1); break;
                    case ConsoleKey.DownArrow: buffer.MoveVertical(1); break;
                    case ConsoleKey.Home: buffer.MoveHome(ctrl); break;
                    case ConsoleKey.End: buffer.MoveEnd(ctrl); break;
                    case ConsoleKey.Escape: buffer.Clear(); break;
                    case ConsoleKey.F1: showHelp = true; break;
                    case ConsoleKey.F2: DictateInto(ctx, buffer); break;
                    case ConsoleKey.F3: result = "/speak"; exit = true; break;
                    case ConsoleKey.F5: result = "/clear"; exit = true; break;
                    case ConsoleKey.F6: result = "/switch-context"; exit = true; break;
                    case ConsoleKey.F7: result = "/switch-folder"; exit = true; break;
                    case ConsoleKey.F8: result = "/toggle-debug"; exit = true; break;
                    case ConsoleKey.F9: result = "/files"; exit = true; break;
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
            prompt = new Markup("[green]❯[/] [grey]Type your message… (F1 for help)[/]");
        else
        {
            var before = buffer.Text[..buffer.Cursor];
            var after = buffer.Text[buffer.Cursor..];
            var withCursor = Markup.Escape(before) + "[invert] [/][underline]​[/]" + Markup.Escape(after);
            var lines = withCursor.Split('\n');
            prompt = new Markup(string.Join("\n", lines.Select((line, index) =>
                (index == 0 ? "[green]❯[/] " : "  ") + line)));
        }

        var hint = _hintOverride;
        _hintOverride = null;
        var rows = new List<IRenderable>();
        if (_status is not null) rows.Add(new Markup(_status));
        rows.Add(prompt);
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
                case "/files": return "/files";
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
        table.AddRow("[yellow]Esc[/]", "Clear the current draft");
        table.AddRow("[cyan]F2 / F3[/]", "Dictate / read the last response aloud");
        table.AddRow("[cyan]F5 / F6 / F7 / F8[/]", "Clear screen / context / folder / debug");
        table.AddRow("[cyan]F9[/]", "Open the file manager (Norton Commander-style, dual-pane)");
        table.AddRow("[grey]F1[/]", "Show this table");
        table.AddRow("[red]F10 / Ctrl+Q[/]", "Quit RalfAI");
        table.AddRow("[yellow]Ctrl+C / Esc / Ctrl+X[/]", "While working: interrupt the current turn");
        AnsiConsole.Clear();
        AnsiConsole.WriteLine();
        AnsiConsole.Write(Align.Center(table));
        AnsiConsole.WriteLine();
        AnsiConsole.Write(Align.Center(new Markup("[dim]Premi un tasto per tornare…[/]")));
        Console.ReadKey(intercept: true);
        AnsiConsole.Clear();
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
        table.AddRow("[cyan]/files[/]", "Open the file manager (Norton Commander-style, dual-pane)");
        table.AddRow("[grey]/help[/]", "Show this table");
        table.AddRow("[red]exit / /exit[/]", "Quit RalfAI");
        AnsiConsole.WriteLine();
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }
}
