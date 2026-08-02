using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;
using RalfAI.Voice;

namespace RalfAI;

/// <summary>
/// Collects multi-line input into a plain (unboxed) prompt line and submits it on Ctrl+Enter.
///
/// This solves the problem of pasting text that contains blank lines:
/// every line (including empty ones) is accumulated, and only Ctrl+Enter triggers processing.
///
/// In an interactive console, input is collected live, key by key, with no persistent key-binding
/// list — press F1 for a full-screen overlay with the command reference instead. When stdin is
/// redirected (piped/scripted input), falls back to a plain line-by-line prompt with typed
/// /-commands, since key-by-key reading (and therefore function keys) requires a real console.
///
/// Interactive key bindings (live prompt only — see F1 for the full reference):
///   Ctrl+Enter        submit the current buffer
///   Enter              insert a newline (multi-line compose / paste)
///   Esc                clear the buffer and start over
///   F1                 show command reference (full-screen overlay)
///   F2                 dictate a line via the microphone (requires AzureSpeech configuration)
///   F3                 read the agent's last response aloud (requires AzureSpeech configuration)
///   F5                 clear the screen
///   F6                 switch to a different generation context
///   F7                 switch to a different target folder
///   F8                 toggle debug messages (tool calls, rate-limit notices)
///   F10 / Ctrl+Q       quit RalfAI
/// </summary>
public sealed class InputCollector(SpeechService? speech = null)
{
    private readonly List<string> _lines = [];
    private string? _hintOverride;
    private string? _status;

    /// <summary>
    /// Collects input until Ctrl+Enter and returns the assembled text.
    /// Returns <c>null</c> when the user quits (F10/Ctrl+Q, or EOF/'exit' in piped mode).
    /// Any other string (including the internal "/speak", "/clear" markers) is
    /// returned as-is for the caller to handle.
    /// </summary>
    /// <param name="status">
    /// Ready-to-render Spectre markup (e.g. current context/model/folder) shown above the
    /// prompt line — lets the caller keep the user oriented without a persistent command list.
    /// </param>
    public string? Collect(string? status = null)
    {
        _lines.Clear();
        _status = status;
        return Console.IsInputRedirected ? CollectPlain() : CollectLive();
    }

    // ── Interactive mode: live plain prompt, no border ──────────────────────

    private string? CollectLive()
    {
        var current = new StringBuilder();
        string? result = null;
        var exitLoop = false;

        // Use the terminal's own cursor instead of a fake glyph — avoids Unicode fallback
        // artifacts and looks orphaned less on an otherwise-empty continuation line.
        try { Console.CursorVisible = true; } catch (IOException) { }

        while (!exitLoop)
        {
            var showHelp = false;

            AnsiConsole.Live(RenderPrompt(current)).Start(ctx =>
            {
                var innerDone = false;

                while (!innerDone)
                {
                    ctx.UpdateTarget(RenderPrompt(current));
                    ctx.Refresh();

                    var key = Console.ReadKey(intercept: true);
                    var ctrl = (key.Modifiers & ConsoleModifiers.Control) != 0;

                    if (key.Key == ConsoleKey.Enter && ctrl)
                    {
                        if (current.Length > 0)
                        {
                            _lines.Add(current.ToString());
                            current.Clear();
                        }

                        if (_lines.Count > 0)
                        {
                            result = string.Join("\n", _lines).Trim();
                            exitLoop = true;
                            innerDone = true;
                        }
                        else
                        {
                            _hintOverride = "[yellow]Buffer is empty — type something first.[/]";
                        }
                        continue;
                    }

                    if (key.Key == ConsoleKey.F10 || (key.Key == ConsoleKey.Q && ctrl))
                    {
                        result = null;
                        exitLoop = true;
                        innerDone = true;
                        continue;
                    }

                    switch (key.Key)
                    {
                        case ConsoleKey.Enter:
                            _lines.Add(current.ToString());
                            current.Clear();
                            break;

                        case ConsoleKey.F1:
                            showHelp = true;
                            innerDone = true;
                            break;

                        case ConsoleKey.F2:
                            DictateInto(ctx);
                            break;

                        case ConsoleKey.F3:
                            result = "/speak";
                            exitLoop = true;
                            innerDone = true;
                            break;

                        case ConsoleKey.F5:
                            result = "/clear";
                            exitLoop = true;
                            innerDone = true;
                            break;

                        case ConsoleKey.F6:
                            result = "/switch-context";
                            exitLoop = true;
                            innerDone = true;
                            break;

                        case ConsoleKey.F7:
                            result = "/switch-folder";
                            exitLoop = true;
                            innerDone = true;
                            break;

                        case ConsoleKey.F8:
                            result = "/toggle-debug";
                            exitLoop = true;
                            innerDone = true;
                            break;

                        case ConsoleKey.Backspace:
                            if (current.Length > 0)
                                current.Remove(current.Length - 1, 1);
                            else if (_lines.Count > 0)
                            {
                                current.Append(_lines[^1]);
                                _lines.RemoveAt(_lines.Count - 1);
                            }
                            break;

                        case ConsoleKey.Escape:
                            _lines.Clear();
                            current.Clear();
                            break;

                        default:
                            if (!char.IsControl(key.KeyChar))
                                current.Append(key.KeyChar);
                            break;
                    }
                }

                ctx.UpdateTarget(RenderPrompt(current));
                ctx.Refresh();
            });

            if (showHelp) ShowHelpOverlay();
        }

        try { Console.CursorVisible = false; } catch (IOException) { }

        return result;
    }

    // ── Voice dictation (F2) ─────────────────────────────────────────────────

    /// <summary>
    /// Listens on the microphone and appends the recognized text as a new line in the
    /// buffer, as if the user had typed it. No-op with a hint if AzureSpeech isn't configured.
    /// </summary>
    private void DictateInto(LiveDisplayContext ctx)
    {
        if (speech is null)
        {
            _hintOverride = "[red]Voice non configurata — imposta AzureSpeech in appsettings.json.[/]";
            return;
        }

        ctx.UpdateTarget(new Markup("[cyan]🎤 In ascolto… parla pure — si ferma da solo alla pausa.[/]"));
        ctx.Refresh();

        var (spoken, error) = speech.RecognizeOnceAsync().GetAwaiter().GetResult();

        if (spoken is not null)
            _lines.Add(spoken);

        _hintOverride = spoken is not null
            ? null
            : $"[red]{Markup.Escape(error ?? "Riconoscimento non riuscito.")}[/]";
    }

    private IRenderable RenderPrompt(StringBuilder current)
    {
        var displayLines = new List<string>(_lines) { current.ToString() };
        var hasContent = displayLines.Any(l => l.Length > 0);

        // No fake cursor glyph here — the real terminal cursor (shown during CollectLive) ends
        // up in the right place naturally, since it's just wherever this text stopped printing.
        IRenderable promptLine = hasContent
            ? new Markup(string.Join("\n", displayLines.Select((l, i) =>
                (i == 0 ? "[green]❯[/] " : "  ") + Markup.Escape(l))))
            : new Markup("[green]❯[/] [grey]Type your message… (F1 for help)[/]");

        // Only transient feedback (errors, "buffer empty", dictation status) gets a line below
        // the prompt — no persistent key-binding list. Shown for exactly one render, then cleared.
        var hint = _hintOverride;
        _hintOverride = null;

        var rows = new List<IRenderable>();
        if (_status is not null) rows.Add(new Markup(_status));
        rows.Add(promptLine);
        if (hint is not null) rows.Add(new Markup(hint));

        return rows.Count == 1 ? rows[0] : new Rows(rows);
    }

    // ── Fallback mode: plain line-by-line prompt (redirected/piped stdin) ───
    //
    // Function keys and Ctrl+Enter can't be read from redirected stdin, so this path keeps
    // the original typed /-commands for scripted/piped usage.

    private string? CollectPlain()
    {
        while (true)
        {
            // Prompt changes after the first line to show multi-line mode
            if (_lines.Count == 0)
                AnsiConsole.Markup("[bold green]you>[/] ");
            else
                AnsiConsole.Markup($"[dim]  ({_lines.Count} lines)[/] ");

            string? line;
            try { line = Console.ReadLine(); }
            catch { return null; }

            if (line is null) return null; // EOF / Ctrl+Z

            var trimmed = line.Trim();

            switch (trimmed.ToLowerInvariant())
            {
                case "exit":
                case "/exit":
                    return null;

                case "/cancel":
                    _lines.Clear();
                    AnsiConsole.MarkupLine("[dim]  ↩ Buffer cleared.[/]");
                    continue;

                case "/exe":
                    if (_lines.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[dim]  Buffer is empty — type something first.[/]");
                        continue;
                    }
                    return string.Join("\n", _lines).Trim();

                case "/help":
                    ShowHelpOverlay();
                    continue;

                case "/speak":
                    return "/speak";

                case "/clear":
                    return "/clear";

                case "/switch-context":
                    return "/switch-context";

                case "/switch-folder":
                    return "/switch-folder";

                case "/toggle-debug":
                    return "/toggle-debug";

                case "/voice":
                    if (speech is null)
                    {
                        AnsiConsole.MarkupLine("[red]  Voice non configurata — imposta AzureSpeech in appsettings.json.[/]");
                        continue;
                    }

                    AnsiConsole.MarkupLine("[cyan]  🎤 In ascolto… parla pure.[/]");
                    var (spoken, error) = speech.RecognizeOnceAsync().GetAwaiter().GetResult();

                    if (spoken is null)
                    {
                        AnsiConsole.MarkupLine($"[red]  {Markup.Escape(error ?? "Riconoscimento non riuscito.")}[/]");
                        continue;
                    }

                    _lines.Add(spoken);
                    AnsiConsole.MarkupLine($"[dim]  ({_lines.Count} lines) \"{Markup.Escape(spoken)}\"[/]");
                    continue;

                default:
                    _lines.Add(line);
                    if (_lines.Count == 1)
                        AnsiConsole.MarkupLine("[dim]  (add more lines or type [bold]/exe[/] to send)[/]");
                    continue;
            }
        }
    }

    // ── F1 / /help — full-screen overlay, not part of the persistent prompt ─

    private static void ShowHelpOverlay()
    {
        AnsiConsole.Clear();
        AnsiConsole.WriteLine();

        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]Key[/]").Centered())
            .AddColumn("[bold]What it does[/]");

        table.AddRow("[green]Ctrl+Enter[/]", "Send the current buffer to the agent");
        table.AddRow("[yellow]Esc[/]",       "Clear the buffer and start fresh");
        table.AddRow("[cyan]F2[/]",          "Dictate a line via microphone (needs AzureSpeech config)");
        table.AddRow("[cyan]F3[/]",          "Read the agent's last response aloud (needs AzureSpeech config)");
        table.AddRow("[cyan]F5[/]",          "Clear the screen");
        table.AddRow("[cyan]F6[/]",          "Switch to a different generation context");
        table.AddRow("[cyan]F7[/]",          "Switch to a different target folder (file browser)");
        table.AddRow("[cyan]F8[/]",          "Toggle debug messages (tool calls, rate-limit notices)");
        table.AddRow("[grey]F1[/]",          "Show this table");
        table.AddRow("[red]F10 / Ctrl+Q[/]", "Quit RalfAI");

        AnsiConsole.Write(Align.Center(table));
        AnsiConsole.WriteLine();
        AnsiConsole.Write(Align.Center(new Markup("[dim]Premi un tasto per tornare…[/]")));

        Console.ReadKey(intercept: true);
        AnsiConsole.Clear();
    }
}
