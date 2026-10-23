using System.ComponentModel;
using RalfAI.ImageGeneration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace RalfAI.Plugins;

/// <summary>
/// Lets the agent generate new images (logos, icons, illustrations) from a text prompt via
/// <see cref="ImageGenerationService"/>. Only registered when an image-generation model is
/// configured for the active AI provider. Always saves as JPG/PNG, converting the model's raw
/// output with ImageSharp if the requested extension differs.
/// </summary>
public sealed class ImageGenerationPlugin(ImageGenerationService imageGen, string rootFolder)
{
    private static readonly string[] SupportedExtensions = [".jpg", ".jpeg", ".png"];

    [Description(
        "Generate a new image from a text prompt using AI (text-to-image) and save it as JPG/PNG " +
        "in the target folder — e.g. a project logo, an icon, or illustrative art. Can take a " +
        "while to respond; the description shown while it runs is the live progress indicator.")]
    public async Task<string> GenerateAI(
        [Description("Detailed description of the image to generate (subject, style, colors, mood)")] string prompt,
        [Description("Relative path to save the result, with a .jpg/.jpeg or .png extension")] string outputPath,
        [Description("Image size: 256x256, 512x512, 1024x1024 (default), 1792x1024 or 1024x1792")] string size = "1024x1024")
    {
        var extension = Path.GetExtension(outputPath);
        if (!SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return $"ERROR: unsupported output extension '{extension}' — only .jpg/.jpeg/.png are supported.";

        string fullPath;
        try
        {
            fullPath = ResolveSafe(outputPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            return $"ERROR: {ex.Message}";
        }

        var (bytes, error) = await imageGen.GenerateAsync(prompt, size);
        if (bytes is null)
            return $"ERROR: image generation failed — {error}";

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        using var image = Image.Load(bytes.ToArray());
        IImageEncoder encoder = extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            ? new PngEncoder()
            : new JpegEncoder();
        FileChangeNotifier.BeforeChange(rootFolder, fullPath);
        await image.SaveAsync(fullPath, encoder);

        return $"OK — generated and written {outputPath} ({image.Width}x{image.Height} px, model: {imageGen.ModelId})";
    }

    private string ResolveSafe(string relativePath)
    {
        return SafePath.Resolve(rootFolder, relativePath);
    }
}
