using System.ComponentModel;

namespace RalfAI.Plugins;

/// <summary>
/// Lets the agent download web pages or raw files by URL.
/// Content is truncated at 50 000 characters to stay within context limits.
/// </summary>
public sealed class WebPlugin
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders = { { "User-Agent", "RalfAI/1.0" } }
    };

    [Description(
        "Download the text content of an HTTP or HTTPS URL. " +
        "Use this when the user provides a link to a spec, doc, package page, or source file. " +
        "Returns the raw text (HTML, Markdown, plain text, JSON, etc.). " +
        "Content longer than 50 000 characters is truncated.")]
    public async Task<string> FetchUrl(
        [Description("Full URL starting with http:// or https://")] string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
            return $"ERROR: '{url}' is not a valid http/https URL.";

        try
        {
            var text = await Http.GetStringAsync(uri);
            return text.Length > 50_000
                ? text[..50_000] + "\n\n… [content truncated at 50 000 chars]"
                : text;
        }
        catch (Exception ex)
        {
            return $"ERROR fetching {url}: {ex.Message}";
        }
    }
}
