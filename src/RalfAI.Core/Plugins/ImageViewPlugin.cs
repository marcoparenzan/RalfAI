using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace RalfAI.Plugins;

/// <summary>
/// Lets the model look at image files of the project. Not every provider accepts images inside a tool result, so the tool only
/// checks the files and answers with one line per image; <see cref="ImageViewingChatClient"/> then attaches the pictures in a user
/// message right after that result, in every request of the same turn.
/// </summary>
public sealed partial class ImageViewPlugin(string rootFolder)
{
    public const int MaxImagesPerCall = 8;
    public const int HighSide = 1536;
    public const int LowSide = 512;

    [Description("Look at image files of the project (PNG, JPEG, GIF, WebP): the images are shown to you right after this result. " +
                 "Use it whenever the content of a picture matters - style, layout, text in a screenshot, what a drawing shows. " +
                 "'low' detail is cheap and enough for overall style and composition; 'high' for small text and fine detail. " +
                 "Up to 8 images per call; call it again for more. SVG files are text: read them with FileSystem.ReadFile.")]
    public string View(
        [Description("Paths relative to the project root, e.g. ['refs/a.png', 'some/b.webp']")] string[] relativePaths,
        [Description("'high' (default) or 'low'")] string detail = "high")
    {
        if (relativePaths is null || relativePaths.Length == 0) return "No paths given.";
        var side = detail.Trim().Equals("low", StringComparison.OrdinalIgnoreCase) ? "low" : "high";
        var lines = new List<string>();
        var problems = new List<string>();

        foreach (var path in relativePaths.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (lines.Count >= MaxImagesPerCall) { problems.Add($"{path}: not shown, only {MaxImagesPerCall} images per call"); continue; }
            string full;
            try { full = SafePath.Resolve(rootFolder, path); }
            catch (UnauthorizedAccessException ex) { problems.Add($"{path}: {ex.Message}"); continue; }

            if (path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)) { problems.Add($"{path}: SVG is text, read it with FileSystem.ReadFile"); continue; }
            if (!ImageAttachments.IsImagePath(path)) { problems.Add($"{path}: not a PNG, JPEG, GIF or WebP file"); continue; }
            if (!File.Exists(full)) { problems.Add($"{path}: file not found"); continue; }

            var (image, error) = ImageAttachments.TryLoad(full, side == "low" ? LowSide : HighSide);
            if (image is null) { problems.Add($"{path}: {error}"); continue; }
            lines.Add(Marker(path.Replace('\\', '/'), image.Width, image.Height, side));
        }

        var sb = new StringBuilder();
        sb.AppendLine(lines.Count == 0
            ? "No image could be shown."
            : $"Showing {lines.Count} image(s); they are attached right after this result, in this order:");
        foreach (var line in lines) sb.AppendLine(line);
        foreach (var problem in problems) sb.AppendLine("Not shown - " + problem);
        return sb.ToString().TrimEnd();
    }

    internal static string Marker(string path, int width, int height, string detail) => $"[image: {path} | {width}x{height} | {detail}]";

    [GeneratedRegex(@"^\[image: (?<path>.+) \| \d+x\d+ \| (?<detail>low|high)\]\r?$", RegexOptions.Multiline)]
    internal static partial Regex MarkerRegex();
}

/// <summary>
/// Attaches the images an <see cref="ImageViewPlugin"/> call asked for, as a user message right after its result. Only for the calls
/// of the current turn (after the last user message): later turns keep the model's own description, not the pixels, which would
/// otherwise be sent again with every request. Nothing is stored in the session.
/// </summary>
public sealed class ImageViewingChatClient(IChatClient inner, string rootFolder) : DelegatingChatClient(inner)
{
    private readonly Dictionary<(string Path, DateTime Written, string Detail), DataContent> _cache = [];

    public override Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => base.GetResponseAsync(Attach(messages), options, cancellationToken);

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => base.GetStreamingResponseAsync(Attach(messages), options, cancellationToken);

    internal IEnumerable<ChatMessage> Attach(IEnumerable<ChatMessage> messages)
    {
        var list = messages as IList<ChatMessage> ?? messages.ToList();
        var turnStart = -1;
        for (var i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].Role == ChatRole.User && !list[i].Contents.Any(c => c is FunctionResultContent)) { turnStart = i; break; }
        }

        List<ChatMessage>? result = null;
        for (var i = 0; i < list.Count; i++)
        {
            var message = list[i];
            result?.Add(message);
            if (i <= turnStart || message.Role != ChatRole.Tool) continue;

            var images = new List<AIContent>();
            foreach (var output in message.Contents.OfType<FunctionResultContent>())
            {
                if (output.Result?.ToString() is not { } text) continue;
                foreach (Match match in ImageViewPlugin.MarkerRegex().Matches(text))
                {
                    var path = match.Groups["path"].Value;
                    if (Load(path, match.Groups["detail"].Value) is { } image)
                    {
                        images.Add(new TextContent($"Image: {path}"));
                        images.Add(image);
                    }
                }
            }
            if (images.Count == 0) continue;

            result ??= [.. list.Take(i + 1)];
            result.Add(new ChatMessage(ChatRole.User, [new TextContent("[Images requested with Image.View]"), .. images]));
        }
        return result ?? list;
    }

    private DataContent? Load(string relativePath, string detail)
    {
        try
        {
            var full = SafePath.Resolve(rootFolder, relativePath);
            if (!File.Exists(full)) return null;
            var key = (full, File.GetLastWriteTimeUtc(full), detail);
            if (_cache.TryGetValue(key, out var cached)) return cached;

            var (image, _) = ImageAttachments.TryLoad(full, detail == "low" ? ImageViewPlugin.LowSide : ImageViewPlugin.HighSide);
            if (image is null) return null;
            return _cache[key] = new DataContent(image.Content.ToMemory(), image.MediaType);
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
