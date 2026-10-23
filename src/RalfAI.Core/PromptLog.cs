using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace RalfAI;

/// <summary>
/// Appends every request to <c>PROMPTS.md</c> at the root of the target folder, as a numbered entry. The engine does it,
/// not the model: it costs no model round-trip, is identical every time, and never loses an entry.
/// Numbering continues from the highest number already in the file, whatever format earlier entries used
/// (<c>12. "text"</c> or <c>## 12. "text"</c>), so files written by older versions or by hand keep working.
/// </summary>
public static partial class PromptLog
{
    public const string DefaultFileName = "PROMPTS.md";
    private const int MaxTitleChars = 100;
    private const int MaxBodyChars = 4000;

    [GeneratedRegex(@"^(?:#{1,6}\s*)?(\d+)\.\s", RegexOptions.Multiline)]
    private static partial Regex EntryNumber();

    /// <summary>Adds an entry; returns its number, or null when nothing was written (empty request, disabled, or an I/O problem).</summary>
    public static int? Append(string targetFolder, string request, string fileName = DefaultFileName, DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(request)) return null;

        try
        {
            var path = Path.Combine(targetFolder, fileName);
            var existing = File.Exists(path) ? File.ReadAllText(path) : "";
            var number = EntryNumber().Matches(existing).Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)).DefaultIfEmpty(0).Max() + 1;

            var text = request.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
            var firstLine = text.Split('\n')[0].Trim();
            var title = firstLine.Length > MaxTitleChars ? firstLine[..MaxTitleChars].TrimEnd() + "…" : firstLine;
            var singleShortLine = text == firstLine && firstLine.Length <= MaxTitleChars;

            var entry = new StringBuilder();
            if (existing.Length == 0) entry.Append("# Prompts\n\nLog cronologico delle richieste, scritto da RalfAI.\n");
            else if (!existing.EndsWith('\n')) entry.Append('\n');
            entry.Append($"\n## {number}. \"{title}\"\n\n");
            if (!singleShortLine)
                entry.Append(text.Length > MaxBodyChars ? text[..MaxBodyChars].TrimEnd() + "\n\n[…richiesta troncata]" : text).Append("\n\n");
            entry.Append($"_{(now ?? DateTimeOffset.Now):yyyy-MM-dd HH:mm}_\n");

            File.AppendAllText(path, entry.ToString().Replace("\n", Environment.NewLine, StringComparison.Ordinal), new UTF8Encoding(false));
            return number;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null; // the log is a convenience: never fail a turn over it
        }
    }
}
