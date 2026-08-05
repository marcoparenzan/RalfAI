using System.ComponentModel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace RalfAI.Plugins;

/// <summary>
/// Basic JPG/PNG manipulation (info, resize, crop, rotate, flip, grayscale, format
/// conversion) inside the target folder. Registered as an engine-level tool (like AI.Ask), so
/// it's available in every context regardless of stack — image assets (icons, screenshots,
/// web assets) show up everywhere. Paths are resolved the same way as FileSystemPlugin:
/// relative to the target folder root, blocked from traversing out of it.
/// </summary>
public sealed class ImagePlugin(string rootFolder)
{
    private static readonly string[] SupportedExtensions = [".jpg", ".jpeg", ".png"];

    [Description("Get width, height, format and file size of a JPG/PNG image.")]
    public async Task<string> GetInfo(
        [Description("Relative path from project root")] string relativePath)
    {
        var fullPath = ResolveExisting(relativePath, out var error);
        if (fullPath is null) return error!;

        try
        {
            using var image = await Image.LoadAsync(fullPath);
            var bytes = new FileInfo(fullPath).Length;
            var format = image.Metadata.DecodedImageFormat?.Name ?? "unknown";
            return $"{image.Width}x{image.Height} px, format: {format}, {bytes} bytes";
        }
        catch (UnknownImageFormatException)
        {
            return $"ERROR: not a recognizable JPG/PNG image — {relativePath}";
        }
    }

    [Description("Resize a JPG/PNG image, writing the result to a new (or the same) file. Set one of width/height to 0 to derive it automatically while keeping the aspect ratio.")]
    public Task<string> Resize(
        [Description("Relative path of the source image")] string relativePath,
        [Description("Relative path for the resized output (can equal the source)")] string outputPath,
        [Description("Target width in pixels (0 = derive from height)")] int width,
        [Description("Target height in pixels (0 = derive from width)")] int height,
        [Description("If true, fit within width x height preserving aspect ratio; if false, stretch to the exact size")] bool keepAspectRatio = true)
        => Transform(relativePath, outputPath, image =>
        {
            if (width <= 0 && height <= 0)
                throw new ArgumentException("At least one of width/height must be greater than 0.");

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(Math.Max(width, 0), Math.Max(height, 0)),
                Mode = keepAspectRatio ? ResizeMode.Max : ResizeMode.Stretch
            }));
        });

    [Description("Crop a rectangular region out of a JPG/PNG image, writing the result to a new (or the same) file.")]
    public Task<string> Crop(
        [Description("Relative path of the source image")] string relativePath,
        [Description("Relative path for the cropped output (can equal the source)")] string outputPath,
        [Description("Left edge of the crop rectangle, in pixels")] int x,
        [Description("Top edge of the crop rectangle, in pixels")] int y,
        [Description("Width of the crop rectangle, in pixels")] int width,
        [Description("Height of the crop rectangle, in pixels")] int height)
        => Transform(relativePath, outputPath, image =>
        {
            var bounds = new Rectangle(0, 0, image.Width, image.Height);
            var crop = Rectangle.Intersect(bounds, new Rectangle(x, y, width, height));
            if (crop.Width <= 0 || crop.Height <= 0)
                throw new ArgumentException($"Crop rectangle ({x},{y},{width}x{height}) falls outside the image ({image.Width}x{image.Height}).");

            image.Mutate(i => i.Crop(crop));
        });

    [Description("Rotate a JPG/PNG image by an arbitrary angle (degrees, clockwise), writing the result to a new (or the same) file.")]
    public Task<string> Rotate(
        [Description("Relative path of the source image")] string relativePath,
        [Description("Relative path for the rotated output (can equal the source)")] string outputPath,
        [Description("Rotation angle in degrees, clockwise (e.g. 90, 180, -45)")] float degrees)
        => Transform(relativePath, outputPath, image => image.Mutate(x => x.Rotate(degrees)));

    [Description("Flip a JPG/PNG image horizontally or vertically, writing the result to a new (or the same) file.")]
    public Task<string> Flip(
        [Description("Relative path of the source image")] string relativePath,
        [Description("Relative path for the flipped output (can equal the source)")] string outputPath,
        [Description("true = mirror left-right (horizontal), false = mirror top-bottom (vertical)")] bool horizontal)
        => Transform(relativePath, outputPath,
            image => image.Mutate(x => x.Flip(horizontal ? FlipMode.Horizontal : FlipMode.Vertical)));

    [Description("Convert a JPG/PNG image to grayscale, writing the result to a new (or the same) file.")]
    public Task<string> Grayscale(
        [Description("Relative path of the source image")] string relativePath,
        [Description("Relative path for the grayscale output (can equal the source)")] string outputPath)
        => Transform(relativePath, outputPath, image => image.Mutate(x => x.Grayscale()));

    [Description("Convert a JPG/PNG image to the other format. The target format is inferred from outputPath's extension (.jpg/.jpeg or .png).")]
    public Task<string> ConvertFormat(
        [Description("Relative path of the source image")] string relativePath,
        [Description("Relative path for the converted output, with the target extension (.jpg/.jpeg or .png)")] string outputPath)
        => Transform(relativePath, outputPath, _ => { });

    // ── Shared load/mutate/save pipeline ────────────────────────────────────

    private async Task<string> Transform(string relativePath, string outputPath, Action<Image> mutate)
    {
        var fullPath = ResolveExisting(relativePath, out var error);
        if (fullPath is null) return error!;

        var outputExtension = Path.GetExtension(outputPath);
        if (!SupportedExtensions.Contains(outputExtension, StringComparer.OrdinalIgnoreCase))
            return $"ERROR: unsupported output extension '{outputExtension}' — only .jpg/.jpeg/.png are supported.";

        string fullOutputPath;
        try
        {
            fullOutputPath = ResolveSafe(outputPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            return $"ERROR: {ex.Message}";
        }

        try
        {
            using var image = await Image.LoadAsync(fullPath);
            mutate(image);

            Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
            await image.SaveAsync(fullOutputPath, GetEncoder(outputExtension));

            return $"OK — written {outputPath} ({image.Width}x{image.Height} px)";
        }
        catch (UnknownImageFormatException)
        {
            return $"ERROR: not a recognizable JPG/PNG image — {relativePath}";
        }
        catch (ArgumentException ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    private static SixLabors.ImageSharp.Formats.IImageEncoder GetEncoder(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => new JpegEncoder(),
        ".png" => new PngEncoder(),
        _ => throw new NotSupportedException($"Unsupported image extension: {extension}")
    };

    private string? ResolveExisting(string relativePath, out string? error)
    {
        var extension = Path.GetExtension(relativePath);
        if (!SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            error = $"ERROR: unsupported extension '{extension}' — only .jpg/.jpeg/.png are supported.";
            return null;
        }

        string fullPath;
        try
        {
            fullPath = ResolveSafe(relativePath);
        }
        catch (UnauthorizedAccessException ex)
        {
            error = $"ERROR: {ex.Message}";
            return null;
        }

        if (!File.Exists(fullPath))
        {
            error = $"ERROR: file not found — {relativePath}";
            return null;
        }

        error = null;
        return fullPath;
    }

    private string ResolveSafe(string relativePath)
    {
        var full = Path.GetFullPath(Path.Combine(rootFolder, relativePath));
        if (!full.StartsWith(rootFolder, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Path traversal blocked: {relativePath}");
        return full;
    }
}
