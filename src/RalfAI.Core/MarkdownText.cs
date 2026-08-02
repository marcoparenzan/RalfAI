using System.Text.RegularExpressions;

namespace RalfAI;

/// <summary>
/// Strips common Markdown syntax down to plain text, for surfaces that can't render it: the
/// console (which would otherwise print literal *, #, ` characters) and text-to-speech (which
/// would otherwise read those symbols aloud). The raw Markdown is still kept in the session log.
/// </summary>
public static partial class MarkdownText
{
    [GeneratedRegex(@"```[a-zA-Z0-9]*\n?")]
    private static partial Regex CodeFenceRegex();

    [GeneratedRegex(@"`([^`]*)`")]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"(\*\*\*|___)(.+?)\1")]
    private static partial Regex BoldItalicRegex();

    [GeneratedRegex(@"(\*\*|__)(.+?)\1")]
    private static partial Regex BoldRegex();

    [GeneratedRegex(@"(?<!\w)(\*|_)(.+?)\1(?!\w)")]
    private static partial Regex ItalicRegex();

    [GeneratedRegex(@"^\s{0,3}#{1,6}\s+", RegexOptions.Multiline)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^\s{0,3}>\s?", RegexOptions.Multiline)]
    private static partial Regex BlockquoteRegex();

    [GeneratedRegex(@"^\s*([-*+]|\d+\.)\s+", RegexOptions.Multiline)]
    private static partial Regex ListMarkerRegex();

    [GeneratedRegex(@"^\s*([-*_]\s?){3,}\s*$", RegexOptions.Multiline)]
    private static partial Regex HorizontalRuleRegex();

    [GeneratedRegex(@"!?\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex LinkRegex();

    public static string StripFormatting(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var result = text;
        result = CodeFenceRegex().Replace(result, "");
        result = LinkRegex().Replace(result, "$1");
        result = BoldItalicRegex().Replace(result, "$2");
        result = BoldRegex().Replace(result, "$2");
        result = ItalicRegex().Replace(result, "$2");
        result = InlineCodeRegex().Replace(result, "$1");
        result = HorizontalRuleRegex().Replace(result, "");
        result = HeadingRegex().Replace(result, "");
        result = BlockquoteRegex().Replace(result, "");
        result = ListMarkerRegex().Replace(result, "");

        return result.Trim();
    }
}
