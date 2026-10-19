using Spectre.Console;
using Spectre.Console.Rendering;

namespace RalfAI;

/// <summary>
/// Full-screen, scrollable read-only text viewer — F3 in both <see cref="FileBrowser"/> and
/// <see cref="FileBrowser"/>. Unlike a one-shot dump, it pages through the file (vertically
/// and horizontally) instead of printing everything and losing anything past the top of the
/// terminal, and it never chokes rendering a huge line since only the visible slice is drawn.
/// </summary>
public static class FileViewer
{
    private const long MaxBytes = 5_000_000;

    public static void Show(string fullPath, string displayName)
    {
        string[] lines;
        try
        {
            var info = new FileInfo(fullPath);
            if (!info.Exists)
            {
                ShowMessage($"[red]File not found — {Markup.Escape(displayName)}.[/]");
                return;
            }
            if (info.Length > MaxBytes)
            {
                ShowMessage($"[yellow]File too large to view ({DirectoryListing.FormatSize(info.Length)} > " +
                            $"{DirectoryListing.FormatSize(MaxBytes)}) — open it in an external editor instead (F4).[/]");
                return;
            }

            var bytes = File.ReadAllBytes(fullPath);
            if (bytes.Take(8000).Any(b => b == 0))
            {
                ShowMessage("[yellow]Binary file — cannot view as text (F4 opens it externally).[/]");
                return;
            }

            // Splitting on \n (and trimming a trailing \r) is enough for both line-ending styles
            // without needing a full text-line reader for a read-only viewer.
            lines = System.Text.Encoding.UTF8.GetString(bytes)
                .Split('\n')
                .Select(l => l.TrimEnd('\r'))
                .ToArray();
        }
        catch (Exception ex)
        {
            ShowMessage($"[red]Could not read file: {Markup.Escape(ex.Message)}[/]");
            return;
        }

        RunViewer(lines, displayName);
    }

    private static void ShowMessage(string markup)
    {
        AnsiConsole.Clear();
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(markup);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Premi un tasto per tornare…[/]");
        Console.ReadKey(intercept: true);
    }

    private static void RunViewer(string[] lines, string displayName)
    {
        var top = 0;    // first visible line
        var left = 0;   // first visible column (horizontal scroll)
        var lineNumberWidth = lines.Length.ToString().Length;

        try { Console.CursorVisible = false; } catch (IOException) { }

        AnsiConsole.Live(Render(lines, displayName, top, left, lineNumberWidth)).Start(ctx =>
        {
            var done = false;
            while (!done)
            {
                ctx.UpdateTarget(Render(lines, displayName, top, left, lineNumberWidth));
                ctx.Refresh();

                var body = Math.Max(1, (Console.IsOutputRedirected ? 24 : Console.WindowHeight) - 5);
                var key = Console.ReadKey(intercept: true);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:    top = Math.Max(0, top - 1); break;
                    case ConsoleKey.DownArrow:  top = Math.Min(Math.Max(0, lines.Length - 1), top + 1); break;
                    case ConsoleKey.PageUp:     top = Math.Max(0, top - body); break;
                    case ConsoleKey.PageDown:   top = Math.Min(Math.Max(0, lines.Length - 1), top + body); break;
                    case ConsoleKey.Home:       top = 0; left = 0; break;
                    case ConsoleKey.End:        top = Math.Max(0, lines.Length - body); break;
                    case ConsoleKey.LeftArrow:  left = Math.Max(0, left - 8); break;
                    case ConsoleKey.RightArrow: left += 8; break;
                    case ConsoleKey.F3:
                    case ConsoleKey.F10:
                    case ConsoleKey.Escape:
                        done = true;
                        break;
                }
            }
        });

        try { Console.CursorVisible = false; } catch (IOException) { }
    }

    private static IRenderable Render(string[] lines, string displayName, int top, int left, int lineNumberWidth)
    {
        var windowHeight = Console.IsOutputRedirected ? 24 : Console.WindowHeight;
        var body = Math.Max(1, windowHeight - 5);
        var windowWidth = Console.IsOutputRedirected ? 120 : Console.WindowWidth;
        var textWidth = Math.Max(10, windowWidth - lineNumberWidth - 5);

        var visibleLines = new List<string>();
        var end = Math.Min(lines.Length, top + body);
        for (var i = top; i < end; i++)
        {
            var lineNumber = (i + 1).ToString().PadLeft(lineNumberWidth);
            var content = left < lines[i].Length ? lines[i][left..] : "";
            if (content.Length > textWidth) content = content[..textWidth];
            visibleLines.Add($"[grey]{lineNumber}[/] [dim]│[/] {Markup.Escape(content)}");
        }
        if (visibleLines.Count == 0) visibleLines.Add("[dim](empty file)[/]");

        var panel = new Panel(string.Join("\n", visibleLines))
        {
            Header = new PanelHeader($" {Markup.Escape(DirectoryListing.Truncate(displayName, 70))} "),
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse("cyan"),
            Padding = new Padding(1, 0, 1, 0),
            Expand = true
        };

        var lastLine = Math.Max(1, Math.Min(lines.Length, top + body));
        var status = $"[dim]Line {top + 1}-{lastLine} of {lines.Length}" +
                     (left > 0 ? $" · col {left + 1}+" : "") +
                     "  ·  ↑↓ scroll · PgUp/PgDn page · ←→ horizontal · Home/End · F3/Esc close[/]";

        return new Rows(panel, new Markup(status));
    }
}
