namespace RalfAI;

/// <summary>
/// Switches the terminal to its alternate screen buffer for the lifetime of the returned
/// <see cref="IDisposable"/> — the same mechanism vim/less/htop use to take over the whole
/// screen without touching the user's normal scrollback, which is restored exactly as it was
/// on dispose. Spectre.Console has no built-in concept of this (it renders sequentially into
/// whatever buffer is current), but it writes plain ANSI under the hood, so entering/exiting
/// the alternate buffer around a whole session composes fine with it.
/// </summary>
public static class FullScreenConsole
{
    private const string EnterAltScreen = "\x1b[?1049h\x1b[H";
    private const string ExitAltScreen = "\x1b[?1049l";

    /// <summary>No-op (returns null) when output/input isn't a real interactive console.</summary>
    public static IDisposable? Enter()
    {
        if (Console.IsOutputRedirected || Console.IsInputRedirected) return null;

        Console.Out.Write(EnterAltScreen);
        try { Console.CursorVisible = false; } catch (IOException) { }

        return new Session();
    }

    private sealed class Session : IDisposable
    {
        public void Dispose()
        {
            try { Console.CursorVisible = true; } catch (IOException) { }
            Console.Out.Write(ExitAltScreen);
        }
    }
}
