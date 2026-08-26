using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using RalfAI.ImageGeneration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace RalfAI.Plugins;

public sealed class ImageGenerationPlugin(ImageGenerationService imageGen, string rootFolder)
{
	private static readonly string[] SupportedExtensions = new string[3] { ".jpg", ".jpeg", ".png" };

	[Description("Generate a new image from a text prompt using AI (text-to-image) and save it as JPG/PNG in the target folder — e.g. a project logo, an icon, or illustrative art. Can take a while to respond; the description shown while it runs is the live progress indicator.")]
	public async Task<string> GenerateAI([Description("Detailed description of the image to generate (subject, style, colors, mood)")] string prompt, [Description("Relative path to save the result, with a .jpg/.jpeg or .png extension")] string outputPath, [Description("Image size: 256x256, 512x512, 1024x1024 (default), 1792x1024 or 1024x1792")] string size = "1024x1024")
	{
		string extension = Path.GetExtension(outputPath);
		if (!SupportedExtensions.Contains<string>(extension, StringComparer.OrdinalIgnoreCase))
		{
			return "ERROR: unsupported output extension '" + extension + "' — only .jpg/.jpeg/.png are supported.";
		}
		string fullPath;
		try
		{
			fullPath = ResolveSafe(outputPath);
		}
		catch (UnauthorizedAccessException ex)
		{
			return "ERROR: " + ex.Message;
		}
		var (bytes, error) = await imageGen.GenerateAsync(prompt, size);
		if (bytes == null)
		{
			return "ERROR: image generation failed — " + error;
		}
		Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
		using Image image = Image.Load(bytes.ToArray());
		IImageEncoder imageEncoder2;
		if (!extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
		{
			IImageEncoder imageEncoder = new JpegEncoder();
			imageEncoder2 = imageEncoder;
		}
		else
		{
			IImageEncoder imageEncoder = new PngEncoder();
			imageEncoder2 = imageEncoder;
		}
		IImageEncoder encoder = imageEncoder2;
		await image.SaveAsync(fullPath, encoder);
		return $"OK — generated and written {outputPath} ({image.Width}x{image.Height} px, model: {imageGen.ModelId})";
	}

	private string ResolveSafe(string relativePath)
	{
		string fullPath = Path.GetFullPath(Path.Combine(rootFolder, relativePath));
		if (!fullPath.StartsWith(rootFolder, StringComparison.OrdinalIgnoreCase))
		{
			throw new UnauthorizedAccessException("Path traversal blocked: " + relativePath);
		}
		return fullPath;
	}
}
