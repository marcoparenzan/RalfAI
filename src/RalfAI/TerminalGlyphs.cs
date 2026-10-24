using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("RalfAI.Tests")]

namespace RalfAI;

public enum GlyphMode { Unicode, Ascii }

/// <summary>
/// Makes the arrows, bullets and emoji of the UI show up - or degrade cleanly. On Windows a console defaults to an OEM code page
/// (850, 437…) that cannot represent them, so they came out as "?". At startup:
/// <list type="bullet">
/// <item><c>unicode</c> (or <c>auto</c> on a terminal known to cope - a pseudo console such as Windows Terminal or VS Code, ConEmu, or a UTF-8 locale; and always when output
/// is redirected): output is switched to UTF-8.</item>
/// <item><c>ascii</c> (or <c>auto</c> elsewhere): symbols - arrows, box drawing, bullets, emoji - are replaced by plain equivalents
/// of the same width (<c>→</c> becomes <c>&gt;</c>, <c>…</c> becomes <c>.</c>), and so is anything else the console encoding cannot show.
/// Accented Latin letters are kept.</item>
/// </list>
/// Set it with <c>Ui:Glyphs</c> (auto | unicode | ascii) or <c>--ascii</c>.
/// </summary>
public static class TerminalGlyphs
{
    public static GlyphMode Configure(string? setting, bool forceAscii = false)
    {
        var mode = Decide(setting, forceAscii, Console.IsOutputRedirected, Environment.GetEnvironmentVariable, IsPseudoConsole());
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        try
        {
            // Text piped into the program (scripts, tests) is UTF-8; read as the OEM code page it turned "è" into "├¿".
            if (Console.IsInputRedirected) Console.InputEncoding = utf8;

            if (mode == GlyphMode.Unicode)
            {
                Console.OutputEncoding = utf8;
            }
            else
            {
                // Asked for ASCII: symbols are replaced even if the console could encode them (its font may still lack them).
                Console.SetOut(new GlyphFallbackWriter(Console.Out, Console.OutputEncoding, replaceSymbols: true));
            }
        }
        catch (IOException)
        {
            // no real console attached (some hosts): nothing to configure
        }
        return mode;
    }

    /// <summary>Whether the console is a ConPTY pseudo console: its window is then a hidden "PseudoConsoleWindow".</summary>
    private static bool IsPseudoConsole()
    {
        if (!OperatingSystem.IsWindows()) return false;
        try
        {
            var window = GetConsoleWindow();
            if (window == IntPtr.Zero) return false;
            var name = new StringBuilder(64);
            return GetClassName(window, name, name.Capacity) > 0 && name.ToString() == "PseudoConsoleWindow";
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return false;
        }
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder className, int maxCount);

    internal static GlyphMode Decide(string? setting, bool forceAscii, bool outputRedirected, Func<string, string?> env, bool pseudoConsole = false)
    {
        if (forceAscii) return GlyphMode.Ascii;

        switch (setting?.Trim().ToLowerInvariant())
        {
            case "ascii": return GlyphMode.Ascii;
            case "unicode": return GlyphMode.Unicode;
        }

        // auto: redirected output (a file, a pipe) is read by programs, and they expect UTF-8.
        if (outputRedirected) return GlyphMode.Unicode;

        if (OperatingSystem.IsWindows())
        {
            // A pseudo console is a modern terminal drawing the text (Windows Terminal - also when it opens cmd as the default
            // terminal, where WT_SESSION is not set - VS Code, …), not the old console window.
            var modern = pseudoConsole || env("WT_SESSION") is not null || env("TERM_PROGRAM") is not null || env("ConEmuPID") is not null || env("WSL_DISTRO_NAME") is not null;
            return modern ? GlyphMode.Unicode : GlyphMode.Ascii;
        }

        var locale = env("LC_ALL") ?? env("LC_CTYPE") ?? env("LANG") ?? "";
        return locale.Contains("utf", StringComparison.OrdinalIgnoreCase) ? GlyphMode.Unicode : GlyphMode.Ascii;
    }
}

/// <summary>Passes text through, replacing the characters the target encoding cannot represent with plain equivalents.</summary>
public sealed class GlyphFallbackWriter : TextWriter
{
    // Every stand-in takes exactly as many columns as the character it replaces: Spectre lays text out (wrapping, panels,
    // live redraws that move the cursor up N lines) before it reaches this writer, so a wider stand-in ("..." for "…")
    // made lines wrap where Spectre did not expect it and every redraw landed lower on the screen.
    private static readonly Dictionary<string, string> Fallbacks = new()
    {
        ["…"] = ".", ["—"] = "-", ["–"] = "-", ["−"] = "-", ["‑"] = "-",
        ["·"] = ".", ["•"] = "*", ["●"] = "*", ["○"] = "o", ["■"] = "#", ["█"] = "#", ["▓"] = "#", ["▒"] = "=", ["░"] = ".",
        ["→"] = ">", ["←"] = "<", ["↑"] = "^", ["↓"] = "v", ["↩"] = "<", ["⇒"] = ">", ["▶"] = ">", ["❯"] = ">", ["›"] = ">", ["»"] = ">",
        ["✻"] = "*", ["✎"] = "~", ["⚠"] = "!", ["✓"] = "v", ["✔"] = "v", ["✗"] = "x", ["✘"] = "x", ["×"] = "x",
        ["⛔"] = "x ", ["📎"] = "+ ", ["🎤"] = "o ", ["🔊"] = "< ", ["📁"] = "# ", ["📄"] = "= ",
        ["“"] = "\"", ["”"] = "\"", ["‘"] = "'", ["’"] = "'", ["«"] = "<",
        ["─"] = "-", ["━"] = "-", ["═"] = "=", ["│"] = "|", ["┃"] = "|", ["║"] = "|",
        ["┌"] = "+", ["┐"] = "+", ["└"] = "+", ["┘"] = "+", ["├"] = "+", ["┤"] = "+", ["┬"] = "+", ["┴"] = "+", ["┼"] = "+",
        ["╭"] = "+", ["╮"] = "+", ["╰"] = "+", ["╯"] = "+", ["╔"] = "+", ["╗"] = "+", ["╚"] = "+", ["╝"] = "+",
    };

    internal static IEnumerable<string> KnownSymbols => Fallbacks.Keys;

    /// <summary>Columns the element takes on screen, measured the way Spectre measures it.</summary>
    internal static int Columns(string element) => new Spectre.Console.Rendering.Segment(element).CellCount();

    /// <summary>The stand-in for <paramref name="element"/>, padded or cut to the same number of columns.</summary>
    internal static string StandIn(string element)
    {
        var width = Columns(element);
        var plain = Fallbacks.TryGetValue(element, out var known) ? known : "?";
        return plain.Length >= width ? plain[..Math.Max(width, 0)] : plain.PadRight(width);
    }

    private readonly TextWriter _inner;
    private readonly bool _replaceSymbols;
    private readonly Encoding _target;
    private readonly Encoding _strict;
    private readonly Dictionary<string, bool> _canShow = [];
    private char _pendingHighSurrogate;

    /// <param name="replaceSymbols">Also replace symbols the target could encode (everything outside the Latin letters).</param>
    public GlyphFallbackWriter(TextWriter inner, Encoding target, bool replaceSymbols = false)
    {
        _inner = inner;
        _replaceSymbols = replaceSymbols;
        _target = target;
        // Same encoding, but throwing instead of silently writing '?', to find out what it cannot show.
        // .NET knows only Unicode/ASCII/Latin-1 by itself: the OEM code pages (850, 437…) come from this provider.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _strict = Encoding.GetEncoding(target.CodePage, EncoderFallback.ExceptionFallback, DecoderFallback.ReplacementFallback);
    }

    public override Encoding Encoding => _target;

    public override void Write(char value)
    {
        if (char.IsHighSurrogate(value)) { _pendingHighSurrogate = value; return; }
        if (_pendingHighSurrogate != '\0')
        {
            var pair = new string([_pendingHighSurrogate, value]);
            _pendingHighSurrogate = '\0';
            _inner.Write(Convert(pair));
            return;
        }
        _inner.Write(Convert(value.ToString()));
    }

    public override void Write(string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        if (_pendingHighSurrogate != '\0') { var held = _pendingHighSurrogate; _pendingHighSurrogate = '\0'; value = held + value; }
        _inner.Write(ConvertText(value));
    }

    public override void Write(char[] buffer, int index, int count) => Write(new string(buffer, index, count));

    public override void Flush() => _inner.Flush();

    /// <summary>The text as the target encoding can show it.</summary>
    public string ConvertText(string text)
    {
        if (text.All(c => c < 128)) return text;               // the common case costs nothing

        var sb = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var element = char.IsHighSurrogate(text[i]) && i + 1 < text.Length ? text.Substring(i++, 2) : text[i].ToString();
            sb.Append(Convert(element));
        }
        return sb.ToString();
    }

    private string Convert(string element)
    {
        if (element.Length == 1 && element[0] < 128) return element;
        var isLatinLetterOrPunctuation = element.Length == 1 && element[0] <= 0x024F;     // Latin-1 Supplement, Latin Extended-A/B
        if ((!_replaceSymbols || isLatinLetterOrPunctuation) && CanShow(element)) return element;
        return StandIn(element);
    }

    private bool CanShow(string element)
    {
        if (_canShow.TryGetValue(element, out var known)) return known;
        try { _strict.GetByteCount(element); known = true; }
        catch (EncoderFallbackException) { known = false; }
        return _canShow[element] = known;
    }
}
