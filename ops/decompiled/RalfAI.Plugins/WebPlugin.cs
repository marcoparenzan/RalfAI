using System;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;

namespace RalfAI.Plugins;

public sealed class WebPlugin
{
	private static readonly HttpClient Http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(30L),
		DefaultRequestHeaders = { { "User-Agent", "RalfAI/1.0" } }
	};

	[Description("Download the text content of an HTTP or HTTPS URL. Use this when the user provides a link to a spec, doc, package page, or source file. Returns the raw text (HTML, Markdown, plain text, JSON, etc.). Content longer than 50 000 characters is truncated.")]
	public async Task<string> FetchUrl([Description("Full URL starting with http:// or https://")] string url)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
		{
			return "ERROR: '" + url + "' is not a valid http/https URL.";
		}
		try
		{
			string text = await Http.GetStringAsync(uri);
			return (text.Length > 50000) ? (text.Substring(0, 50000) + "\n\n… [content truncated at 50 000 chars]") : text;
		}
		catch (Exception ex)
		{
			return "ERROR fetching " + url + ": " + ex.Message;
		}
	}
}
