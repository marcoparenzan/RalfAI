namespace RalfAI;

/// <summary>
/// Switches the terminal to its alternate screen buffer for the lifetime of the returned
/// <see cref="IDisposable"/> — the same mechanism vim/less/htop use to take over the whole
/// screen without touching the user's normal scrollback, which is restored exactly as it was
/// on dispose. Spectre.Console has no built-in concept of this (it renders sequentially into
/// whatever buffer is current), but it writes plain ANSI under the hood, so entering/exiting
/// the alternate buffer around a whole session composes fine with it.
/// <para>
/// The alternate buffer has no scrollback, so the chat itself runs in the normal buffer (the conversation stays in the
/// terminal's history) unless <c>Ui:FullScreen</c> asks otherwise; full-screen panels (help, context menu, folder picker)
/// use <see cref="Overlay"/>, which leaves the conversation on screen exactly as it was when they close. Entering is
/// nestable: only the outermost enter and exit switch buffers.
/// </para>
/// </summary>
public static class FullScreenConsole
{
    private const string EnterAltScreen = "\x1b[?1049h\x1b[H";
    private const string ExitAltScreen = "\x1b[?1049l";
    private static int s_depth;

    /// <summary>No-op (returns null) when output/input isn't a real interactive console.</summary>
    public static IDisposable? Enter()
    {
        if (Console.IsOutputRedirected || Console.IsInputRedirected) return null;

        if (s_depth++ == 0)
        {
            Console.Out.Write(EnterAltScreen);
            try { Console.CursorVisible = false; } catch (IOException) { }
        }
        return new Session();
    }

    /// <summary>A full-screen panel drawn over the conversation; disposing it brings the conversation back untouched.</summary>
    public static IDisposable? Overlay()
    {
        var session = Enter();
        if (session is not null) Console.Out.Write("\x1b[2J\x1b[H");      // a clean panel even when already in the alternate buffer
        return session;
    }

    private sealed class Session : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (--s_depth > 0) return;
            try { Console.CursorVisible = true; } catch (IOException) { }
            Console.Out.Write(ExitAltScreen);
        }
    }
}
