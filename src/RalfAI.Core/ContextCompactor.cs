using System.Globalization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace RalfAI;

/// <param name="Enabled">Context:Compaction (default true).</param>
/// <param name="RecentChars">Context:RecentToolChars - tool output (results and large arguments) kept intact, newest first. Default 20000 characters, about 5k tokens.</param>
/// <param name="MinChars">Context:CompactMinChars - items shorter than this are never replaced. Default 600.</param>
/// <param name="KeepImagesInLastUserMessages">Context:KeepImages - images stay in this many latest user messages. Default 2.</param>
public sealed record CompactionOptions(bool Enabled = true, int RecentChars = 20_000, int MinChars = 600, int KeepImagesInLastUserMessages = 2)
{
    public static CompactionOptions FromConfig(IConfiguration config) => new(
        !string.Equals(config["Context:Compaction"], "off", StringComparison.OrdinalIgnoreCase),
        int.TryParse(config["Context:RecentToolChars"], out var recent) && recent >= 0 ? recent : 20_000,
        int.TryParse(config["Context:CompactMinChars"], out var min) && min >= 0 ? min : 600,
        int.TryParse(config["Context:KeepImages"], out var images) && images >= 0 ? images : 2);
}

public readonly record struct CompactionStats(int Results, int Arguments, int Images, long CharsSaved)
{
    public bool Any => Results + Arguments + Images > 0;
}

/// <summary>
/// Keeps a long session affordable. Every model call re-sends the whole conversation, and most of it is old tool output:
/// file contents the agent read, full file bodies it wrote, build logs, screenshots. This replaces the old bulk of that with a
/// short stub (what it was, how big, how to get it back) <em>in the copy sent to the model</em>; the session itself is untouched.
/// The newest <see cref="CompactionOptions.RecentChars"/> characters of tool output stay as they are, so the agent keeps the
/// working memory of what it is doing right now. Stubs are deterministic, so once an item is compacted the prompt prefix stays
/// stable and provider-side prompt caching keeps working.
/// </summary>
public static class ContextCompactor
{
    // Stubs must read the same on every machine, or the cached prompt prefix would change with the locale.
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static (IList<ChatMessage> Messages, CompactionStats Stats) Compact(IList<ChatMessage> messages, CompactionOptions options)
    {
        if (!options.Enabled || messages.Count == 0) return (messages, default);

        // Which items are big enough to matter, newest first, and which of them still fit the "recent" budget.
        var keep = new HashSet<(int Message, int Content)>();
        long used = 0;
        for (var m = messages.Count - 1; m >= 0; m--)
        {
            for (var c = messages[m].Contents.Count - 1; c >= 0; c--)
            {
                var size = CompactableSize(messages[m].Contents[c]);
                if (size < options.MinChars) continue;
                if (used + size <= options.RecentChars)
                {
                    used += size;
                    keep.Add((m, c));
                }
            }
        }

        // Images stay only in the last few user messages.
        var imageKeepFrom = int.MaxValue;
        var seenUserImages = 0;
        for (var m = messages.Count - 1; m >= 0; m--)
        {
            if (messages[m].Role != ChatRole.User || !messages[m].Contents.Any(IsImage)) continue;
            if (++seenUserImages <= options.KeepImagesInLastUserMessages) imageKeepFrom = m;
        }

        int results = 0, arguments = 0, images = 0;
        long saved = 0;
        List<ChatMessage>? output = null;

        for (var m = 0; m < messages.Count; m++)
        {
            var original = messages[m];
            List<AIContent>? changed = null;

            for (var c = 0; c < original.Contents.Count; c++)
            {
                var content = original.Contents[c];
                AIContent? replacement = null;

                switch (content)
                {
                    case FunctionResultContent result when CompactableSize(result) >= options.MinChars && !keep.Contains((m, c)):
                        var text = ResultText(result);
                        replacement = new FunctionResultContent(result.CallId, ResultStub(text));
                        results++;
                        saved += text.Length - ((string)((FunctionResultContent)replacement).Result!).Length;
                        break;

                    case FunctionCallContent call when CompactableSize(call) >= options.MinChars && !keep.Contains((m, c)):
                        var (compacted, delta) = CompactArguments(call, options.MinChars);
                        replacement = compacted;
                        arguments++;
                        saved += delta;
                        break;

                    case DataContent image when IsImage(image) && m < imageKeepFrom:
                        replacement = new TextContent($"[image omitted to save context: {image.MediaType}, {(image.Data.Length / 1024).ToString("N0", Invariant)} KB]");
                        images++;
                        saved += image.Data.Length;
                        break;
                }

                if (replacement is null) continue;
                changed ??= [.. original.Contents];
                changed[c] = replacement;
            }

            if (changed is null) continue;
            output ??= [.. messages];
            output[m] = new ChatMessage(original.Role, changed)
            {
                AuthorName = original.AuthorName,
                MessageId = original.MessageId,
                AdditionalProperties = original.AdditionalProperties,
            };
        }

        return output is null ? (messages, default) : (output, new CompactionStats(results, arguments, images, saved));
    }

    private static bool IsImage(AIContent content) => content is DataContent { MediaType: { } type } && type.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    private static string ResultText(FunctionResultContent result) => result.Result?.ToString() ?? "";

    private static int CompactableSize(AIContent content) => content switch
    {
        FunctionResultContent result => ResultText(result).Length,
        FunctionCallContent call => call.Arguments?.Values.OfType<string>().Sum(s => s.Length) ?? 0,
        _ => 0,
    };

    private static string ResultStub(string text)
    {
        var firstLine = text.Split('\n', 2)[0].Trim();
        if (firstLine.Length > 100) firstLine = firstLine[..100].TrimEnd() + "…";
        return $"[earlier tool result omitted to save context: {text.Length.ToString("N0", Invariant)} characters. It began: \"{firstLine}\". Call the tool again if you need its content.]";
    }

    /// <summary>Long string arguments (typically the whole body of a file that was written) become a short marker.</summary>
    private static (FunctionCallContent Call, long Saved) CompactArguments(FunctionCallContent call, int minChars)
    {
        long saved = 0;
        var arguments = new Dictionary<string, object?>();
        foreach (var (key, value) in call.Arguments ?? new Dictionary<string, object?>())
        {
            if (value is string s && s.Length >= minChars)
            {
                var head = s.Length > 60 ? s[..60].ReplaceLineEndings(" ") + "…" : s;
                var stub = $"{head} [{s.Length.ToString("N0", Invariant)} characters omitted to save context]";
                saved += s.Length - stub.Length;
                arguments[key] = stub;
            }
            else arguments[key] = value;
        }
        return (new FunctionCallContent(call.CallId, call.Name, arguments), saved);
    }
}

/// <summary>Applies <see cref="ContextCompactor"/> to every request on its way to the model.</summary>
public sealed class CompactingChatClient(IChatClient inner, CompactionOptions options, Action<CompactionStats>? onCompacted = null) : DelegatingChatClient(inner)
{
    public override Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? chatOptions = null, CancellationToken cancellationToken = default)
        => base.GetResponseAsync(Compact(messages), chatOptions, cancellationToken);

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? chatOptions = null, CancellationToken cancellationToken = default)
        => base.GetStreamingResponseAsync(Compact(messages), chatOptions, cancellationToken);

    private IEnumerable<ChatMessage> Compact(IEnumerable<ChatMessage> messages)
    {
        var list = messages as IList<ChatMessage> ?? messages.ToList();
        var (compacted, stats) = ContextCompactor.Compact(list, options);
        if (stats.Any) onCompacted?.Invoke(stats);
        return compacted;
    }
}
