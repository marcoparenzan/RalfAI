using Spectre.Console;
using Spectre.Console.Rendering;

namespace RalfAI;

/// <summary>
/// A single-pane file browser: navigate the filesystem, view files, and rename/delete/create
/// folders — used both to pick RalfAI's target folder (at startup with no CLI argument, or
/// via F7 during a session) and as a lightweight general-purpose browser. Runs entirely inside
/// the console via raw key reads, like <see cref="InputCollector"/>.
/// </summary>
public sealed class FileBrowser
{
    private string _path;
    private List<FileSystemEntry> _entries = [];
    private int _selected;
    private int _scrollOffset;
    private string? _message;

    private FileBrowser(string startDirectory)
    {
        _path = Directory.Exists(startDirectory) ? Path.GetFullPath(startDirectory) : Directory.GetCurrentDirectory();
        Reload();
    }

    /// <summary>
    /// Opens the file browser. Returns the folder the user picked with F9, or <c>null</c> if
    /// they cancelled (F10/Esc). Falls back to a plain text prompt when stdin is redirected,
    /// since key-by-key reading requires a real console.
    /// </summary>
    public static string? Pick(string startDirectory)
    {
        if (Console.IsInputRedirected)
        {
            // Spectre's TextPrompt reads key-by-key via its own input abstraction, which throws
            // on redirected stdin even for plain text — use Console.ReadLine() directly instead,
            // same as InputCollector.CollectPlain() does for the same reason.
            AnsiConsole.MarkupLine($"[dim]Input non interattivo — invio vuoto usa {Markup.Escape(startDirectory)}.[/]");
            AnsiConsole.Markup("Target folder: ");
            string? path;
            try { path = Console.ReadLine(); }
            catch { return null; }

            path = string.IsNullOrWhiteSpace(path) ? startDirectory : path.Trim();
            return string.IsNullOrWhiteSpace(path) ? null : path;
        }

        return new FileBrowser(startDirectory).Run();
    }

    // ── Main loop ────────────────────────────────────────────────────────────

    private string? Run()
    {
        string? picked = null;

        while (true)
        {
            var action = Action.None;

            AnsiConsole.Live(RenderAll()).Start(ctx =>
            {
                var innerDone = false;
                while (!innerDone)
                {
                    ctx.UpdateTarget(RenderAll());
                    ctx.Refresh();

                    var key = Console.ReadKey(intercept: true);
                    _message = null;

                    switch (key.Key)
                    {
                        case ConsoleKey.UpArrow:   MoveSelection(-1); break;
                        case ConsoleKey.DownArrow: MoveSelection(1); break;
                        case ConsoleKey.PageUp:    MoveSelection(-10); break;
                        case ConsoleKey.PageDown:  MoveSelection(10); break;
                        case ConsoleKey.Enter:     EnterSelected(); break;
                        case ConsoleKey.Backspace: GoUp(); break;

                        case ConsoleKey.F1: action = Action.Help;   innerDone = true; break;
                        case ConsoleKey.F2: action = Action.Rename; innerDone = true; break;
                        case ConsoleKey.F3: action = Action.View;   innerDone = true; break;
                        case ConsoleKey.F7: action = Action.MkDir;  innerDone = true; break;

                        case ConsoleKey.F8:
                        case ConsoleKey.Delete:
                            action = Action.Delete;
                            innerDone = true;
                            break;

                        case ConsoleKey.F9:
                            picked = _path;
                            action = Action.Quit;
                            innerDone = true;
                            break;

                        case ConsoleKey.F10:
                        case ConsoleKey.Escape:
                            picked = null;
                            action = Action.Quit;
                            innerDone = true;
                            break;
                    }
                }
            });

            switch (action)
            {
                case Action.Quit:   return picked;
                case Action.Help:   ShowHelp(); break;
                case Action.Rename: RenameSelected(); break;
                case Action.View:   ViewSelected(); break;
                case Action.MkDir:  MakeDirectory(); break;
                case Action.Delete: DeleteSelected(); break;
            }
        }
    }

    private enum Action { None, Quit, Help, Rename, View, MkDir, Delete }

    // ── Navigation ───────────────────────────────────────────────────────────

    private void MoveSelection(int delta)
    {
        if (_entries.Count == 0) return;
        _selected = Math.Clamp(_selected + delta, 0, _entries.Count - 1);
    }

    private void EnterSelected()
    {
        var entry = SelectedEntry();
        if (entry is null || !entry.Value.IsDirectory) return;

        var target = entry.Value.Name == ".."
            ? Directory.GetParent(_path)?.FullName ?? _path
            : Path.Combine(_path, entry.Value.Name);

        NavigateTo(target);
    }

    private void GoUp()
    {
        var parent = Directory.GetParent(_path)?.FullName;
        if (parent is not null) NavigateTo(parent);
    }

    private void NavigateTo(string path)
    {
        try
        {
            _path = Path.GetFullPath(path);
            Reload();
        }
        catch (Exception ex)
        {
            _message = $"[red]Cannot open folder: {Markup.Escape(ex.Message)}[/]";
        }
    }

    private FileSystemEntry? SelectedEntry()
        => _selected >= 0 && _selected < _entries.Count ? _entries[_selected] : null;

    private void Reload()
    {
        _entries = LoadEntries(_path);
        _selected = 0;
        _scrollOffset = 0;
    }

    private static List<FileSystemEntry> LoadEntries(string path)
    {
        var entries = new List<FileSystemEntry>();
        try
        {
            var di = new DirectoryInfo(path);
            if (di.Parent is not null)
                entries.Add(new FileSystemEntry("..", true, 0));

            foreach (var d in di.GetDirectories().OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
                entries.Add(new FileSystemEntry(d.Name, true, 0));

            foreach (var f in di.GetFiles().OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
                entries.Add(new FileSystemEntry(f.Name, false, f.Length));
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            // Leave entries as loaded so far (possibly just ".."); the panel just looks empty.
        }
        return entries;
    }

    // ── File operations (run outside the Live display) ─────────────────────

    private void RenameSelected()
    {
        var entry = SelectedEntry();
        if (entry is null || entry.Value.Name == "..")
        {
            _message = "[yellow]Nothing to rename.[/]";
            return;
        }

        var oldPath = Path.Combine(_path, entry.Value.Name);
        var newName = AnsiConsole.Prompt(
            new TextPrompt<string>($"New name for [bold]{Markup.Escape(entry.Value.Name)}[/]:")
                .DefaultValue(entry.Value.Name));

        if (string.IsNullOrWhiteSpace(newName) || newName == entry.Value.Name) return;

        var newPath = Path.Combine(_path, newName);
        try
        {
            if (entry.Value.IsDirectory) Directory.Move(oldPath, newPath);
            else File.Move(oldPath, newPath);
            _message = $"[green]Renamed to {Markup.Escape(newName)}.[/]";
        }
        catch (Exception ex)
        {
            _message = $"[red]Rename failed: {Markup.Escape(ex.Message)}[/]";
        }

        Reload();
    }

    private void MakeDirectory()
    {
        var name = AnsiConsole.Prompt(new TextPrompt<string>("New folder name:"));
        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            Directory.CreateDirectory(Path.Combine(_path, name));
            _message = $"[green]Created {Markup.Escape(name)}.[/]";
        }
        catch (Exception ex)
        {
            _message = $"[red]Could not create folder: {Markup.Escape(ex.Message)}[/]";
        }

        Reload();
    }

    private void DeleteSelected()
    {
        var entry = SelectedEntry();
        if (entry is null || entry.Value.Name == "..")
        {
            _message = "[yellow]Nothing to delete.[/]";
            return;
        }

        var path = Path.Combine(_path, entry.Value.Name);
        var kind = entry.Value.IsDirectory ? "folder (and everything inside it)" : "file";
        var confirmed = AnsiConsole.Confirm(
            $"Delete {kind} [red]{Markup.Escape(entry.Value.Name)}[/]?", defaultValue: false);
        if (!confirmed) return;

        try
        {
            if (entry.Value.IsDirectory) Directory.Delete(path, recursive: true);
            else File.Delete(path);
            _message = $"[green]Deleted {Markup.Escape(entry.Value.Name)}.[/]";
        }
        catch (Exception ex)
        {
            _message = $"[red]Delete failed: {Markup.Escape(ex.Message)}[/]";
        }

        Reload();
    }

    private void ViewSelected()
    {
        var entry = SelectedEntry();
        if (entry is null || entry.Value.IsDirectory)
        {
            _message = "[yellow]Select a file to view.[/]";
            return;
        }

        var path = Path.Combine(_path, entry.Value.Name);
        string content;
        try
        {
            var info = new FileInfo(path);
            if (info.Length > 200_000)
            {
                _message = "[yellow]File too large to preview (>200 KB) — use an editor instead.[/]";
                return;
            }

            var bytes = File.ReadAllBytes(path);
            if (bytes.Take(8000).Any(b => b == 0))
            {
                _message = "[yellow]Binary file — cannot preview.[/]";
                return;
            }

            content = System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            _message = $"[red]Could not read file: {Markup.Escape(ex.Message)}[/]";
            return;
        }

        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule($"[bold]{Markup.Escape(entry.Value.Name)}[/]").LeftJustified());
        AnsiConsole.WriteLine(content);
        AnsiConsole.Write(new Rule());
        AnsiConsole.MarkupLine("[dim]Premi un tasto per tornare…[/]");
        Console.ReadKey(intercept: true);
    }

    private static void ShowHelp()
    {
        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]Key[/]").Centered())
            .AddColumn("[bold]What it does[/]");

        table.AddRow("[cyan]↑ / ↓[/]",       "Move selection");
        table.AddRow("[cyan]PgUp / PgDn[/]", "Move selection by 10");
        table.AddRow("[cyan]Enter[/]",       "Open folder");
        table.AddRow("[cyan]Backspace[/]",   "Go up one level");
        table.AddRow("[green]F9[/]",         "Use the current folder as RalfAI target");
        table.AddRow("[cyan]F3[/]",          "View file (read-only preview)");
        table.AddRow("[cyan]F2[/]",          "Rename");
        table.AddRow("[cyan]F7[/]",          "Create new folder");
        table.AddRow("[red]F8 / Del[/]",     "Delete (asks for confirmation)");
        table.AddRow("[grey]F1[/]",          "Show this table");
        table.AddRow("[red]F10 / Esc[/]",    "Cancel and return");

        AnsiConsole.WriteLine();
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Premi un tasto per continuare…[/]");
        Console.ReadKey(intercept: true);
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    private IRenderable RenderAll()
    {
        var windowHeight = Console.IsOutputRedirected ? 24 : Console.WindowHeight;
        var listHeight = Math.Max(5, windowHeight - 6);

        var panel = RenderPanel(listHeight);

        var hint = _message
            ?? "[dim]↑↓ move · PgUp/PgDn page · Enter open · ⌫ up · F9 use folder · " +
               "F2 rename · F7 mkdir · F8/Del delete · F3 view · F1 help · F10/Esc cancel[/]";

        return new Rows(panel, new Markup(hint));
    }

    private IRenderable RenderPanel(int listHeight)
    {
        var start = Math.Clamp(_scrollOffset, 0, Math.Max(0, _entries.Count - listHeight));
        if (_selected < start) start = _selected;
        if (_selected >= start + listHeight) start = _selected - listHeight + 1;
        _scrollOffset = start;

        var lines = new List<string>();
        var end = Math.Min(_entries.Count, start + listHeight);
        for (var i = start; i < end; i++)
        {
            var e = _entries[i];
            var name = e.IsDirectory ? $"{e.Name}/" : e.Name;
            var sizeText = e.IsDirectory ? "" : FormatSize(e.Size);
            var line = $"{Truncate(name, 60),-60}{sizeText,10}";

            var escaped = Markup.Escape(line);
            if (i == _selected)
                lines.Add($"[invert]{escaped}[/]");
            else if (e.IsDirectory)
                lines.Add($"[blue]{escaped}[/]");
            else
                lines.Add(escaped);
        }

        if (_entries.Count == 0)
            lines.Add("[dim](empty)[/]");

        return new Panel(string.Join("\n", lines))
        {
            Header = new PanelHeader($" {Markup.Escape(Truncate(_path, 70))} "),
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse("cyan"),
            Padding = new Padding(1, 0, 1, 0),
            Expand = true
        };
    }

    private static string Truncate(string text, int maxLength)
        => text.Length <= maxLength ? text : "…" + text[^(maxLength - 1)..];

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return unit == 0 ? $"{bytes} B" : $"{size:0.#} {units[unit]}";
    }

    private readonly record struct FileSystemEntry(string Name, bool IsDirectory, long Size);
}
