using System.Diagnostics;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace RalfAI;

/// <summary>
/// Dual-pane file manager in the Norton Commander tradition: two directory panels side by
/// side, Tab switches which one is active, and the classic F-key bar (View/Edit/Copy/Move/
/// MkDir/Delete/Quit) operates on the active pane, defaulting copy/move destinations to the
/// *other* pane's current folder. General-purpose file management around the target folder —
/// separate from <see cref="FileBrowser"/>, which stays a lightweight single-pane picker for
/// choosing RalfAI's target folder. Runs entirely inside the console via raw key reads, like
/// <see cref="InputCollector"/>; unusable when stdin is redirected (needs a real console).
/// </summary>
public static class NortonCommander
{
    /// <param name="editorCommand">Command F4 uses to open the selected file externally — same <c>Editor</c> config key as <see cref="FileBrowser"/>.</param>
    public static void Open(string startDirectory, string editorCommand = FileBrowser.DefaultEditor)
    {
        if (Console.IsInputRedirected)
        {
            AnsiConsole.MarkupLine("[yellow]Il file manager richiede un terminale interattivo (stdin non rediretto).[/]");
            return;
        }

        new Session(startDirectory, editorCommand).Run();
    }

    private sealed class Pane(string path)
    {
        public string Path { get; set; } = path;
        public List<DirectoryEntry> Entries { get; set; } = DirectoryListing.Load(path);
        public int Selected { get; set; }
        public int ScrollOffset { get; set; }

        public DirectoryEntry? SelectedEntry() => Selected >= 0 && Selected < Entries.Count ? Entries[Selected] : null;

        public void Reload()
        {
            Entries = DirectoryListing.Load(Path);
            Selected = Math.Clamp(Selected, 0, Math.Max(0, Entries.Count - 1));
        }

        public void NavigateTo(string target)
        {
            Path = System.IO.Path.GetFullPath(target);
            Entries = DirectoryListing.Load(Path);
            Selected = 0;
            ScrollOffset = 0;
        }
    }

    private sealed class Session(string startDirectory, string editorCommand)
    {
        private readonly string _editorCommand = editorCommand;
        private readonly Pane[] _panes =
        [
            new Pane(Directory.Exists(startDirectory) ? Path.GetFullPath(startDirectory) : Directory.GetCurrentDirectory()),
            new Pane(Directory.Exists(startDirectory) ? Path.GetFullPath(startDirectory) : Directory.GetCurrentDirectory())
        ];
        private int _active;
        private string? _message;

        private Pane Active => _panes[_active];
        private Pane Inactive => _panes[1 - _active];

        public void Run()
        {
            try { Console.CursorVisible = false; } catch (IOException) { }

            var quit = false;
            while (!quit)
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
                            case ConsoleKey.Tab:       _active = 1 - _active; break;
                            case ConsoleKey.UpArrow:   MoveSelection(-1); break;
                            case ConsoleKey.DownArrow: MoveSelection(1); break;
                            case ConsoleKey.PageUp:    MoveSelection(-10); break;
                            case ConsoleKey.PageDown:  MoveSelection(10); break;
                            case ConsoleKey.Home:      Active.Selected = 0; break;
                            case ConsoleKey.End:       Active.Selected = Math.Max(0, Active.Entries.Count - 1); break;
                            case ConsoleKey.Enter:     EnterSelected(); break;
                            case ConsoleKey.Backspace: GoUp(); break;

                            case ConsoleKey.F1: action = Action.Help;         innerDone = true; break;
                            case ConsoleKey.F3: action = Action.View;         innerDone = true; break;
                            case ConsoleKey.F4: action = Action.OpenExternal; innerDone = true; break;
                            case ConsoleKey.F5: action = Action.Copy;         innerDone = true; break;
                            case ConsoleKey.F6: action = Action.Move;         innerDone = true; break;
                            case ConsoleKey.F7: action = Action.MkDir;        innerDone = true; break;

                            case ConsoleKey.F8:
                            case ConsoleKey.Delete:
                                action = Action.Delete;
                                innerDone = true;
                                break;

                            case ConsoleKey.F10:
                            case ConsoleKey.Escape:
                                quit = true;
                                innerDone = true;
                                break;
                        }
                    }
                });

                switch (action)
                {
                    case Action.Help:         ShowHelp(); break;
                    case Action.View:         ViewSelected(); break;
                    case Action.OpenExternal: OpenExternal(); break;
                    case Action.Copy:         CopySelected(); break;
                    case Action.Move:         MoveSelected(); break;
                    case Action.MkDir:        MakeDirectory(); break;
                    case Action.Delete:       DeleteSelected(); break;
                }
            }

            AnsiConsole.Clear();
        }

        private enum Action { None, Help, View, OpenExternal, Copy, Move, MkDir, Delete }

        // ── Navigation ───────────────────────────────────────────────────────

        private void MoveSelection(int delta)
        {
            var pane = Active;
            if (pane.Entries.Count == 0) return;
            pane.Selected = Math.Clamp(pane.Selected + delta, 0, pane.Entries.Count - 1);
        }

        private void EnterSelected()
        {
            var entry = Active.SelectedEntry();
            if (entry is null || !entry.Value.IsDirectory) return;

            var target = entry.Value.Name == ".."
                ? Directory.GetParent(Active.Path)?.FullName ?? Active.Path
                : Path.Combine(Active.Path, entry.Value.Name);

            NavigateActiveTo(target);
        }

        private void GoUp()
        {
            var parent = Directory.GetParent(Active.Path)?.FullName;
            if (parent is not null) NavigateActiveTo(parent);
        }

        private void NavigateActiveTo(string target)
        {
            try { Active.NavigateTo(target); }
            catch (Exception ex) { _message = $"[red]Cannot open folder: {Markup.Escape(ex.Message)}[/]"; }
        }

        // ── F3 View / F4 Edit ───────────────────────────────────────────────

        private void ViewSelected()
        {
            var entry = Active.SelectedEntry();
            if (entry is null || entry.Value.IsDirectory)
            {
                _message = "[yellow]Select a file to view.[/]";
                return;
            }

            FileViewer.Show(Path.Combine(Active.Path, entry.Value.Name), entry.Value.Name);
        }

        private void OpenExternal()
        {
            var entry = Active.SelectedEntry();
            if (entry is null || entry.Value.IsDirectory)
            {
                _message = "[yellow]Select a file to open.[/]";
                return;
            }

            var path = Path.Combine(Active.Path, entry.Value.Name);
            try
            {
                var psi = new ProcessStartInfo(_editorCommand) { UseShellExecute = true };
                psi.ArgumentList.Add(path);
                Process.Start(psi);
                _message = $"[green]Opened {Markup.Escape(entry.Value.Name)} in {Markup.Escape(_editorCommand)}.[/]";
            }
            catch (Exception ex)
            {
                _message = $"[red]Could not open '{Markup.Escape(_editorCommand)}': {Markup.Escape(ex.Message)}.[/]";
            }
        }

        // ── F5 Copy / F6 Move ────────────────────────────────────────────────

        private void CopySelected()
        {
            var entry = Active.SelectedEntry();
            if (entry is null || entry.Value.Name == "..")
            {
                _message = "[yellow]Nothing to copy.[/]";
                return;
            }

            var source = Path.Combine(Active.Path, entry.Value.Name);
            var defaultDest = Path.Combine(Inactive.Path, entry.Value.Name);
            var destInput = AnsiConsole.Prompt(
                new TextPrompt<string>($"Copy [bold]{Markup.Escape(entry.Value.Name)}[/] to:").DefaultValue(defaultDest));
            if (string.IsNullOrWhiteSpace(destInput)) return;

            var dest = Path.GetFullPath(destInput);
            if (string.Equals(dest, source, StringComparison.OrdinalIgnoreCase))
            {
                _message = "[yellow]Source and destination are the same — nothing to do.[/]";
                return;
            }

            try
            {
                if (entry.Value.IsDirectory) CopyDirectoryRecursive(source, dest);
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    File.Copy(source, dest, overwrite: false);
                }
                _message = $"[green]Copied to {Markup.Escape(dest)}.[/]";
            }
            catch (Exception ex)
            {
                _message = $"[red]Copy failed: {Markup.Escape(ex.Message)}[/]";
            }

            Active.Reload();
            Inactive.Reload();
        }

        private void MoveSelected()
        {
            var entry = Active.SelectedEntry();
            if (entry is null || entry.Value.Name == "..")
            {
                _message = "[yellow]Nothing to move.[/]";
                return;
            }

            var source = Path.Combine(Active.Path, entry.Value.Name);
            // Same folder, different name → rename. Different folder → move (Norton Commander's
            // classic "RenMov" — one command does both, depending on what you type.
            var defaultDest = Path.Combine(Inactive.Path, entry.Value.Name);
            var destInput = AnsiConsole.Prompt(
                new TextPrompt<string>($"Move/rename [bold]{Markup.Escape(entry.Value.Name)}[/] to:").DefaultValue(defaultDest));
            if (string.IsNullOrWhiteSpace(destInput)) return;

            var dest = Path.GetFullPath(destInput);
            if (string.Equals(dest, source, StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                if (entry.Value.IsDirectory) Directory.Move(source, dest);
                else File.Move(source, dest);
                _message = $"[green]Moved to {Markup.Escape(dest)}.[/]";
            }
            catch (Exception ex)
            {
                _message = $"[red]Move failed: {Markup.Escape(ex.Message)}[/]";
            }

            Active.Reload();
            Inactive.Reload();
        }

        private static void CopyDirectoryRecursive(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: false);
            foreach (var dir in Directory.GetDirectories(sourceDir))
                CopyDirectoryRecursive(dir, Path.Combine(destDir, Path.GetFileName(dir)));
        }

        // ── F7 MkDir / F8 Delete ─────────────────────────────────────────────

        private void MakeDirectory()
        {
            var name = AnsiConsole.Prompt(new TextPrompt<string>("New folder name:"));
            if (string.IsNullOrWhiteSpace(name)) return;

            try
            {
                Directory.CreateDirectory(Path.Combine(Active.Path, name));
                _message = $"[green]Created {Markup.Escape(name)}.[/]";
            }
            catch (Exception ex)
            {
                _message = $"[red]Could not create folder: {Markup.Escape(ex.Message)}[/]";
            }

            Active.Reload();
        }

        private void DeleteSelected()
        {
            var entry = Active.SelectedEntry();
            if (entry is null || entry.Value.Name == "..")
            {
                _message = "[yellow]Nothing to delete.[/]";
                return;
            }

            var path = Path.Combine(Active.Path, entry.Value.Name);
            var kind = entry.Value.IsDirectory ? "folder (and everything inside it)" : "file";
            var confirmed = AnsiConsole.Confirm($"Delete {kind} [red]{Markup.Escape(entry.Value.Name)}[/]?", defaultValue: false);
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

            Active.Reload();
            if (string.Equals(Active.Path, Inactive.Path, StringComparison.OrdinalIgnoreCase)) Inactive.Reload();
        }

        // ── Help ─────────────────────────────────────────────────────────────

        private static void ShowHelp()
        {
            var table = new Table().RoundedBorder().BorderColor(Color.Grey)
                .AddColumn(new TableColumn("[bold]Key[/]").Centered()).AddColumn("[bold]What it does[/]");

            table.AddRow("[cyan]Tab[/]",         "Switch active pane");
            table.AddRow("[cyan]↑ / ↓[/]",       "Move selection");
            table.AddRow("[cyan]PgUp / PgDn[/]", "Move selection by 10");
            table.AddRow("[cyan]Home / End[/]",  "First / last entry");
            table.AddRow("[cyan]Enter[/]",       "Open folder");
            table.AddRow("[cyan]Backspace[/]",   "Go up one level");
            table.AddRow("[cyan]F3[/]",          "View file (scrollable, read-only)");
            table.AddRow("[cyan]F4[/]",          "Open file in external editor (Editor config)");
            table.AddRow("[cyan]F5[/]",          "Copy to the other pane's folder");
            table.AddRow("[cyan]F6[/]",          "Move/rename (edit the path to move, keep it to rename)");
            table.AddRow("[cyan]F7[/]",          "Create new folder in the active pane");
            table.AddRow("[red]F8 / Del[/]",     "Delete (asks for confirmation)");
            table.AddRow("[grey]F1[/]",          "Show this table");
            table.AddRow("[red]F10 / Esc[/]",    "Quit the file manager");

            AnsiConsole.WriteLine();
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Premi un tasto per continuare…[/]");
            Console.ReadKey(intercept: true);
        }

        // ── Rendering ────────────────────────────────────────────────────────

        private IRenderable RenderAll()
        {
            var windowHeight = Console.IsOutputRedirected ? 24 : Console.WindowHeight;
            var listHeight = Math.Max(5, windowHeight - 7);

            var columns = new Columns(RenderPane(0, listHeight), RenderPane(1, listHeight)) { Expand = true };

            var hint = _message
                ?? "[dim]Tab switch pane · ↑↓ move · PgUp/PgDn page · Enter open · ⌫ up · " +
                   "F3 view · F4 edit · F5 copy · F6 move/rename · F7 mkdir · F8/Del delete · " +
                   "F1 help · F10/Esc quit[/]";

            return new Rows(columns, new Markup(hint));
        }

        private IRenderable RenderPane(int index, int listHeight)
        {
            var pane = _panes[index];
            var isActive = index == _active;

            var start = Math.Clamp(pane.ScrollOffset, 0, Math.Max(0, pane.Entries.Count - listHeight));
            if (pane.Selected < start) start = pane.Selected;
            if (pane.Selected >= start + listHeight) start = pane.Selected - listHeight + 1;
            pane.ScrollOffset = start;

            var lines = new List<string>();
            var end = Math.Min(pane.Entries.Count, start + listHeight);
            for (var i = start; i < end; i++)
            {
                var e = pane.Entries[i];
                var name = e.IsDirectory ? $"{e.Name}/" : e.Name;
                var sizeText = e.IsDirectory ? "" : DirectoryListing.FormatSize(e.Size);
                var line = $"{DirectoryListing.Truncate(name, 34),-34}{sizeText,8}";
                var escaped = Markup.Escape(line);

                if (isActive && i == pane.Selected) lines.Add($"[invert]{escaped}[/]");
                else if (e.IsDirectory) lines.Add($"[blue]{escaped}[/]");
                else lines.Add(escaped);
            }
            if (pane.Entries.Count == 0) lines.Add("[dim](empty)[/]");

            return new Panel(string.Join("\n", lines))
            {
                Header = new PanelHeader($" {Markup.Escape(DirectoryListing.Truncate(pane.Path, 40))} "),
                Border = BoxBorder.Rounded,
                BorderStyle = Style.Parse(isActive ? "cyan" : "grey"),
                Padding = new Padding(1, 0, 1, 0),
                Expand = true
            };
        }
    }
}
