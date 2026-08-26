using Microsoft.Extensions.Configuration;
using Spectre.Console;
using RalfAI.Contexts;
using Spectre.Console.Rendering;

namespace RalfAI;

/// <summary>
/// Console frontend on top of <see cref="AgentEngine"/>: renders the chat loop with
/// Spectre.Console (spinner, tool-call lines, panels) and collects input via
/// <see cref="InputCollector"/>. All agent logic (Microsoft Agent Framework, rate limiting,
/// logging, voice) lives in <see cref="AgentEngine"/> so other frontends can reuse it unchanged.
/// </summary>
public sealed class RalfAIApp
{
    private readonly IConfiguration _config;
    private AgentEngine _session;
    private string _contextKey;
    private bool _debug;

    // Set just before each LLM call so tool-call event handlers can update the spinner.
    private StatusContext? _liveStatus;

    private RalfAIApp(IConfiguration config, string contextKey, bool debug, AgentEngine session)
    {
        _config = config;
        _contextKey = contextKey;
        _debug = debug;
        _session = session;
        AttachSessionEvents();
    }

    public static async Task<RalfAIApp> CreateAsync(
        IConfiguration config, string targetFolder, string contextKey, IAgentContext context, bool debug)
    {
        var session = await AgentEngine.CreateAsync(config, targetFolder, contextKey, context);
        return new RalfAIApp(config, contextKey, debug, session);
    }

    private void AttachSessionEvents()
    {
        _session.ToolCallStarted += OnToolCallStarted;
        _session.ToolCallCompleted += OnToolCallCompleted;
        _session.Notice += OnNotice;
        _session.Progress += OnProgress;
    }

    private void DetachSessionEvents()
    {
        _session.ToolCallStarted -= OnToolCallStarted;
        _session.ToolCallCompleted -= OnToolCallCompleted;
        _session.Notice -= OnNotice;
        _session.Progress -= OnProgress;
    }

    // ── Main loop ────────────────────────────────────────────────────────────

    public async Task RunAsync(CancellationToken appToken = default)
    {
        // Ctrl+C cancels the current LLM call (first press); second press exits.
        using var globalCts = CancellationTokenSource.CreateLinkedTokenSource(appToken);

        Console.CancelKeyPress += (_, e) =>
        {
            if (!globalCts.IsCancellationRequested)
            {
                e.Cancel = true;
                globalCts.Cancel();
                AnsiConsole.MarkupLine(
                    "\n  [yellow]⚠ Ctrl+C received — interrupting. Press Ctrl+C again to force quit.[/]");
            }
        };

        var input = new InputCollector(_session.Speech);

        while (!appToken.IsCancellationRequested)
        {

            // Recreate per-turn CTS so Ctrl+C only cancels the current turn.
            using var turnCts = CancellationTokenSource.CreateLinkedTokenSource(appToken);

            // Background task: detect Escape or Ctrl+X during processing.
            // Runs only while we're waiting for the LLM.
            using var escSrc = new CancellationTokenSource();
            var escListener = ListenForEscapeAsync(turnCts, escSrc.Token);

            var text = input.Collect(BuildStatusLine());

            // Stop the escape listener — we're reading input now.
            await escSrc.CancelAsync();
            await escListener.ConfigureAwait(false);

            if (text is null) break;

            // ── Special commands handled by the app (not sent to the agent) ──
            if (text.Equals("/speak", StringComparison.OrdinalIgnoreCase))
            {
                // Listen for Esc/Ctrl+X while audio is playing so it can be interrupted.
                using var escSrcSpeak = new CancellationTokenSource();
                var escListenerSpeak  = ListenForEscapeAsync(turnCts, escSrcSpeak.Token);

                await SpeakLastResponseAsync(turnCts.Token);

                await escSrcSpeak.CancelAsync();
                await escListenerSpeak.ConfigureAwait(false);
                continue;
            }

            if (text.Equals("/clear", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.Clear();
                continue;
            }

            if (text.Equals("/switch-context", StringComparison.OrdinalIgnoreCase))
            {
                await SwitchContext();
                continue;
            }

            if (text.Equals("/switch-folder", StringComparison.OrdinalIgnoreCase))
            {
                await SwitchFolder();
                continue;
            }

            if (text.Equals("/toggle-debug", StringComparison.OrdinalIgnoreCase))
            {
                ToggleDebug();
                continue;
            }

            if (text.Equals("/files", StringComparison.OrdinalIgnoreCase))
            {
                var editorCommand = _config["Editor"] ?? FileBrowser.DefaultEditor;
                NortonCommander.Open(_session.TargetFolder, editorCommand);
                continue;
            }

            // ── LLM processing turn ──────────────────────────────────────────
            AgentTurnResult? turnResult = null;

            // Start the escape listener again while the LLM is working.
            using var escSrc2  = new CancellationTokenSource();
            var escListener2   = ListenForEscapeAsync(turnCts, escSrc2.Token);

            try
            {
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots2)
                    .SpinnerStyle(Style.Parse("cyan"))
                    .StartAsync("[dim cyan]thinking…[/]", async ctx =>
                    {
                        _liveStatus = ctx;
                        try
                        {
                            turnResult = await _session.SendAsync(text, turnCts.Token);
                        }
                        finally
                        {
                            _liveStatus = null;
                        }
                    });
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("  [yellow]↩ Processing cancelled.[/]");
            }

            // Stop escape listener.
            await escSrc2.CancelAsync();
            await escListener2.ConfigureAwait(false);

            if (turnResult is null) continue;

            if (turnResult.Error is not null)
            {
                AnsiConsole.MarkupLine($"\n  [red]⛔ Error: {Markup.Escape(turnResult.Error)}[/]");
                continue;
            }

            ShowAgentResponse(turnResult.Thinking, turnResult.FinalText!, turnResult.Usage, turnResult.Duration, _session.GetCostSummary());
        }

        AnsiConsole.Write(new Rule().RuleStyle("grey dim"));
        AnsiConsole.MarkupLine(
            $"  [dim]Session saved → {_session.LogPath}[/]");
    }

    /// <summary>Context · model · folder shown above the prompt, so switching (F6/F7) stays visible.</summary>
    private string BuildStatusLine()
    {
        var trimmed = _session.TargetFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var folderName = Path.GetFileName(trimmed);
        if (string.IsNullOrEmpty(folderName)) folderName = trimmed;

        return $"[dim]RalfAI v{Markup.Escape(AppVersion.Current)} · {Markup.Escape(_session.Context.Name)} · {Markup.Escape(_session.ModelId)} · {Markup.Escape(folderName)}[/]";
    }

    // ── AgentSession event handlers ──────────────────────────────────────────

    private void OnToolCallStarted(ToolCallStart call)
    {
        // Update the spinner status to the current tool — kept even with debug off, it's
        // transient progress feedback rather than debug noise. Shows the human-readable
        // [Description] instead of the raw Plugin.Function name.
        _liveStatus?.Status($"[yellow]{Markup.Escape(call.Description)}…[/]");

        // Also leave a permanent line above the spinner — always, not just with --debug — so
        // the screen scrolls through what the agent did instead of only ever showing the
        // latest status overwriting the last one. --debug adds the raw tool name/args on top;
        // the tool's own result preview (OnToolCallCompleted) stays debug-only, that's the
        // genuinely verbose part.
        AnsiConsole.MarkupLine(_debug
            ? $"  [bold yellow]⏺[/] [yellow]{Markup.Escape(call.Description)}[/] " +
              $"[dim]({Markup.Escape(call.Name)})[/][grey]({Markup.Escape(call.ArgsPreview)})[/]"
            : $"  [bold yellow]⏺[/] [yellow]{Markup.Escape(call.Description)}…[/]");
    }

    private void OnToolCallCompleted(ToolCallResult call)
    {
        if (!_debug) return;

        var preview = call.Result.Length > 120
            ? call.Result[..120].Replace("\n", " ") + "…"
            : call.Result.Replace("\n", " ");

        AnsiConsole.MarkupLine($"    [dim]⎿  {Markup.Escape(preview)}[/]");
    }

    private void OnProgress(TurnProgress progress)
    {
        _liveStatus?.Status($"[cyan]{Markup.Escape(progress.Message)} · {progress.Elapsed.TotalSeconds:F1}s[/]");

        // Same reasoning as OnToolCallStarted: always leave a permanent, scrolling trace of
        // each phase instead of only the spinner's single line overwriting itself; --debug
        // additionally shows the internal phase code.
        AnsiConsole.MarkupLine(_debug
            ? $"  [dim cyan]◌ {Markup.Escape(progress.Phase)} · {Markup.Escape(progress.Message)} · {progress.Elapsed.TotalSeconds:F1}s[/]"
            : $"  [dim cyan]◌ {Markup.Escape(progress.Message)} · {progress.Elapsed.TotalSeconds:F1}s[/]");
    }

    private void OnNotice(AgentNotice notice)
    {
        // Errors always surface; info/warning pacing notices are debug noise.
        if (!_debug && notice.Level != NoticeLevel.Error) return;

        var color = notice.Level switch
        {
            NoticeLevel.Warning => "yellow",
            NoticeLevel.Error => "red",
            _ => "dim"
        };
        AnsiConsole.MarkupLine($"  [{color}]{Markup.Escape(notice.Message)}[/]");
    }

    // ── UI helpers ───────────────────────────────────────────────────────────

    private async Task SpeakLastResponseAsync(CancellationToken ct)
    {
        if (_session.Speech is null)
        {
            AnsiConsole.MarkupLine("  [red]Voice non configurata — imposta AzureSpeech in appsettings.json.[/]");
            return;
        }

        if (string.IsNullOrWhiteSpace(_session.LastFinalText))
        {
            AnsiConsole.MarkupLine("  [yellow]Nessuna risposta dell'agente da leggere ancora.[/]");
            return;
        }

        AnsiConsole.MarkupLine("  [cyan]🔊 Lettura in corso… [dim](Esc per interrompere)[/][/]");
        try
        {
            var error = await _session.Speech.SpeakAsync(_session.LastFinalText, ct);
            if (error is not null)
                AnsiConsole.MarkupLine($"  [red]{Markup.Escape(error)}[/]");
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("  [yellow]↩ Lettura interrotta.[/]");
        }
    }

    private static void ShowAgentResponse(string? thinking, string text, TurnUsage? usage, TimeSpan? duration, CostSummary? project)
    {
        AnsiConsole.WriteLine();

        if (!string.IsNullOrWhiteSpace(thinking))
        {
            AnsiConsole.Write(new Rule("[grey]✻ Thinking[/]").RuleStyle("grey dim").LeftJustified());
            AnsiConsole.MarkupLine($"[grey italic]{Markup.Escape(MarkdownText.StripFormatting(thinking))}[/]");
            AnsiConsole.WriteLine();
        }

        AnsiConsole.Write(new Rule("[cyan]agent[/]").RuleStyle("cyan dim"));

        // Console can't render Markdown, so strip it before escaping Spectre markup characters.
        AnsiConsole.MarkupLine(Markup.Escape(MarkdownText.StripFormatting(text)));

        if (duration is not null || usage?.IsReported == true)
        {
            var parts = new List<string>();
            if (duration is not null) parts.Add($"{duration.Value.TotalSeconds:F1}s");
            if (usage?.InputTokens is not null) parts.Add($"input {usage.InputTokens:N0} tok");
            if (usage?.OutputTokens is not null) parts.Add($"output {usage.OutputTokens:N0} tok");
            if (usage?.TotalTokens is not null) parts.Add($"total {usage.TotalTokens:N0} tok");
            parts.Add(usage?.EstimatedCostUsd is not null ? $"estimated ${usage.EstimatedCostUsd:F6}" : "cost unavailable (configure Pricing)");

            // Cumulative spend for this project (.ralfai/cost-history.jsonl), not per-turn —
            // answers "how much have I spent here so far", separate from "how much did this
            // one request cost" above.
            if (project is not null)
            {
                var turnWord = project.Turns == 1 ? "turn" : "turns";
                parts.Add(project.TurnsWithCost > 0
                    ? $"project total ${project.TotalCostUsd:F6} over {project.Turns:N0} {turnWord}"
                    : $"project total unavailable over {project.Turns:N0} {turnWord}");
            }

            AnsiConsole.MarkupLine($"[dim]{Markup.Escape(string.Join(" · ", parts))}[/]");
        }

        AnsiConsole.Write(new Rule().RuleStyle("cyan dim"));
        AnsiConsole.WriteLine();
    }

    private async Task SwitchContext()
    {
        var options = ContextRegistry.All().ToList();
        var selected = Math.Max(0, options.FindIndex(o => o.Key == _contextKey));
        (string Key, IAgentContext Context)? choice = null;

        AnsiConsole.Live(RenderContextMenu(options, selected)).Start(ctx =>
        {
            var done = false;
            while (!done)
            {
                ctx.UpdateTarget(RenderContextMenu(options, selected));
                ctx.Refresh();

                var key = Console.ReadKey(intercept: true);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selected = Math.Max(0, selected - 1);
                        break;

                    case ConsoleKey.DownArrow:
                        selected = Math.Min(options.Count - 1, selected + 1);
                        break;

                    case ConsoleKey.Enter:
                        choice = options[selected];
                        done = true;
                        break;

                    case ConsoleKey.Escape:
                        done = true;
                        break;
                }
            }
        });

        AnsiConsole.Clear();

        if (choice is null)
        {
            AnsiConsole.MarkupLine("  [dim]Cambio contesto annullato.[/]");
            return;
        }

        if (choice.Value.Key == _contextKey)
        {
            AnsiConsole.MarkupLine($"  [dim]Già su {Markup.Escape(choice.Value.Context.Name)}.[/]");
            return;
        }

        DetachSessionEvents();
        _contextKey = choice.Value.Key;
        _session = await AgentEngine.CreateAsync(_config, _session.TargetFolder, _contextKey, choice.Value.Context);
        AttachSessionEvents();
        FolderConfigStore.SaveContext(_session.TargetFolder, _contextKey);

        AnsiConsole.MarkupLine(
            $"  [green]✓ Passato a {Markup.Escape(_session.Context.Name)}.[/] " +
            $"[dim]Modello: {Markup.Escape(_session.ModelId)} · " +
            $"({_session.ResumedMessageCount} messaggi ripresi dal log)[/]");
    }

    private static IRenderable RenderContextMenu(List<(string Key, IAgentContext Context)> options, int selected)
    {
        var lines = options.Select((o, i) =>
        {
            var label = Markup.Escape($"{o.Context.Name} — {o.Context.Description}");
            return i == selected ? $"[invert]{label}[/]" : label;
        });

        var panel = new Panel(string.Join("\n", lines))
        {
            Header = new PanelHeader(" Select generation context "),
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse("cyan"),
            Padding = new Padding(1, 0, 1, 0)
        };

        return new Rows(panel, new Markup("[dim]↑↓ move · Enter select · Esc cancel[/]"));
    }

    private async Task SwitchFolder()
    {
        var editorCommand = _config["Editor"] ?? FileBrowser.DefaultEditor;
        var picked = FileBrowser.Pick(_session.TargetFolder, editorCommand);
        AnsiConsole.Clear();

        if (picked is null)
        {
            AnsiConsole.MarkupLine("  [dim]Cambio cartella annullato.[/]");
            return;
        }

        var newFolder = Path.GetFullPath(picked);
        if (string.Equals(newFolder, _session.TargetFolder, StringComparison.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine("  [dim]Stessa cartella già in uso.[/]");
            return;
        }

        Directory.CreateDirectory(newFolder);
        LastFolderStore.Save(newFolder);

        // The destination folder may remember its own context (.ralfai/config.yaml);
        // fall back to whatever context is currently active if it doesn't.
        var newContextKey = FolderConfigStore.LoadContext(newFolder) ?? _contextKey;
        var context = ContextRegistry.Create(newContextKey);
        FolderConfigStore.SaveContext(newFolder, newContextKey);

        DetachSessionEvents();
        _contextKey = newContextKey;
        _session = await AgentEngine.CreateAsync(_config, newFolder, _contextKey, context);
        AttachSessionEvents();

        AnsiConsole.MarkupLine(
            $"  [green]✓ Cartella target: {Markup.Escape(newFolder)}.[/] " +
            $"[dim]Contesto: {Markup.Escape(_session.Context.Name)} · Modello: {Markup.Escape(_session.ModelId)} · " +
            $"({_session.ResumedMessageCount} messaggi ripresi dal log)[/]");
    }

    private void ToggleDebug()
    {
        _debug = !_debug;
        FolderConfigStore.SaveDebug(_session.TargetFolder, _debug);

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine(_debug
            ? "  [green]✓ Debug messages: ON[/] [dim](tool calls, rate-limit notices)[/]"
            : "  [dim]✓ Debug messages: OFF[/]");
    }

    // ── Key listener (Esc / Ctrl+X interrupts the current turn) ─────────────

    private static async Task ListenForEscapeAsync(
        CancellationTokenSource turnCts,
        CancellationToken listenerToken)
    {
        // Console.KeyAvailable throws when stdin is redirected (piped/scripted input) —
        // there are no keystrokes to listen for in that case.
        if (Console.IsInputRedirected) return;

        try
        {
            while (!listenerToken.IsCancellationRequested && !turnCts.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    var isEsc    = key.Key == ConsoleKey.Escape;
                    var isCtrlX  = key.Key == ConsoleKey.X &&
                                   key.Modifiers == ConsoleModifiers.Control;
                    if (isEsc || isCtrlX)
                    {
                        AnsiConsole.MarkupLine(
                            "\n  [yellow]⚠ Escape/Ctrl+X — interrupting turn…[/]");
                        await turnCts.CancelAsync();
                        return;
                    }
                }
                await Task.Delay(50, listenerToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { /* expected */ }
    }
}
