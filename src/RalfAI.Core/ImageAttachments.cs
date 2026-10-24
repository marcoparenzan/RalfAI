using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace RalfAI;

/// <summary>An image sent to the model along with a user message.</summary>
public sealed record ImageAttachment(string Path, string FileName, string MediaType, BinaryData Content, int Width, int Height, bool Resized)
{
    public long SizeBytes => Content.ToMemory().Length;
}

/// <summary>
/// Finds <c>@file</c> mentions of image files in a prompt and loads them as <see cref="ImageAttachment"/>s.
/// The mention text stays in the prompt, so the model also knows each image's name.
/// </summary>
public static partial class ImageAttachments
{
    public const int MaxImages = 8;
    public const long MaxFileBytes = 20 * 1024 * 1024;
    private const int MaxSide = 2048;

    private static readonly Dictionary<string, string> MediaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
    };

    // @path or @"path with spaces"
    [GeneratedRegex("@(?:\"(?<quoted>[^\"]+)\"|(?<plain>[^\\s\"]+))")]
    private static partial Regex MentionRegex();

    public static bool IsImagePath(string path) => MediaTypes.ContainsKey(System.IO.Path.GetExtension(path));

    /// <summary>
    /// Resolves the image mentions in <paramref name="text"/> (relative paths against
    /// <paramref name="baseFolder"/>). Mentions of other files, or of files that do not exist, are ignored.
    /// Problems with an image that was clearly meant (too big, unreadable) are returned as warnings.
    /// </summary>
    public static (List<ImageAttachment> Images, List<string> Warnings) Resolve(string text, string baseFolder)
    {
        var images = new List<ImageAttachment>();
        var warnings = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in MentionRegex().Matches(text))
        {
            var raw = match.Groups["quoted"].Success ? match.Groups["quoted"].Value : match.Groups["plain"].Value.TrimEnd('.', ',', ';', ':', '!', '?', ')');
            if (!IsImagePath(raw)) continue;

            var full = System.IO.Path.GetFullPath(System.IO.Path.IsPathRooted(raw) ? raw : System.IO.Path.Combine(baseFolder, raw));
            if (!File.Exists(full) || !seen.Add(full)) continue;

            if (images.Count >= MaxImages)
            {
                warnings.Add($"Only the first {MaxImages} images are attached; skipped {raw}.");
                continue;
            }

            var (image, error) = TryLoad(full);
            if (image is null) warnings.Add($"{raw}: {error}");
            else images.Add(image);
        }
        return (images, warnings);
    }

    public static (ImageAttachment? Image, string? Error) TryLoad(string fullPath, int maxSide = MaxSide)
    {
        try
        {
            var info = new FileInfo(fullPath);
            if (info.Length > MaxFileBytes)
                return (null, $"file is {info.Length / 1024 / 1024} MB, over the {MaxFileBytes / 1024 / 1024} MB limit");

            var mediaType = MediaTypes[System.IO.Path.GetExtension(fullPath)];
            var bytes = File.ReadAllBytes(fullPath);
            var size = Image.Identify(bytes) ?? throw new InvalidDataException("not a readable image");
            var (width, height, resized) = (size.Width, size.Height, false);

            // Large images cost many tokens for no gain: shrink to at most maxSide px (WebP/GIF are re-encoded as PNG).
            if (Math.Max(width, height) > maxSide)
            {
                using var source = Image.Load(bytes);
                source.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(maxSide, maxSide) }));
                if (mediaType is not "image/jpeg") mediaType = "image/png";
                using var output = new MemoryStream();
                if (mediaType == "image/png") source.Save(output, new PngEncoder());
                else source.Save(output, new JpegEncoder { Quality = 90 });
                bytes = output.ToArray();
                (width, height, resized) = (source.Width, source.Height, true);
            }

            return (new ImageAttachment(fullPath, System.IO.Path.GetFileName(fullPath), mediaType, BinaryData.FromBytes(bytes), width, height, resized), null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or UnknownImageFormatException or ImageFormatException)
        {
            return (null, ex.Message);
        }
    }
}
