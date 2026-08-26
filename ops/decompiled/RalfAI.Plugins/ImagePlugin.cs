using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace RalfAI.Plugins;

public sealed class ImagePlugin(string rootFolder)
{
	private static readonly string[] SupportedExtensions = new string[3] { ".jpg", ".jpeg", ".png" };

	[Description("Get width, height, format and file size of a JPG/PNG image.")]
	public async Task<string> GetInfo([Description("Relative path from project root")] string relativePath)
	{
		string fullPath = ResolveExisting(relativePath, out string error);
		if (fullPath == null)
		{
			return error;
		}
		try
		{
			using Image image = await Image.LoadAsync(fullPath);
			long bytes = new FileInfo(fullPath).Length;
			string format = image.Metadata.DecodedImageFormat?.Name ?? "unknown";
			return $"{image.Width}x{image.Height} px, format: {format}, {bytes} bytes";
		}
		catch (UnknownImageFormatException)
		{
			return "ERROR: not a recognizable JPG/PNG image — " + relativePath;
		}
	}

	[Description("Resize a JPG/PNG image, writing the result to a new (or the same) file. Set one of width/height to 0 to derive it automatically while keeping the aspect ratio.")]
	public Task<string> Resize([Description("Relative path of the source image")] string relativePath, [Description("Relative path for the resized output (can equal the source)")] string outputPath, [Description("Target width in pixels (0 = derive from height)")] int width, [Description("Target height in pixels (0 = derive from width)")] int height, [Description("If true, fit within width x height preserving aspect ratio; if false, stretch to the exact size")] bool keepAspectRatio = true)
	{
		return Transform(relativePath, outputPath, delegate(Image image)
		{
			if (width <= 0 && height <= 0)
			{
				throw new ArgumentException("At least one of width/height must be greater than 0.");
			}
			image.Mutate(delegate(IImageProcessingContext x)
			{
				x.Resize(new ResizeOptions
				{
					Size = new Size(Math.Max(width, 0), Math.Max(height, 0)),
					Mode = (keepAspectRatio ? ResizeMode.Max : ResizeMode.Stretch)
				});
			});
		});
	}

	[Description("Crop a rectangular region out of a JPG/PNG image, writing the result to a new (or the same) file.")]
	public Task<string> Crop([Description("Relative path of the source image")] string relativePath, [Description("Relative path for the cropped output (can equal the source)")] string outputPath, [Description("Left edge of the crop rectangle, in pixels")] int x, [Description("Top edge of the crop rectangle, in pixels")] int y, [Description("Width of the crop rectangle, in pixels")] int width, [Description("Height of the crop rectangle, in pixels")] int height)
	{
		return Transform(relativePath, outputPath, delegate(Image image)
		{
			Rectangle a = new Rectangle(0, 0, image.Width, image.Height);
			Rectangle crop = Rectangle.Intersect(a, new Rectangle(x, y, width, height));
			if (crop.Width <= 0 || crop.Height <= 0)
			{
				throw new ArgumentException($"Crop rectangle ({x},{y},{width}x{height}) falls outside the image ({image.Width}x{image.Height}).");
			}
			image.Mutate(delegate(IImageProcessingContext i)
			{
				i.Crop(crop);
			});
		});
	}

	[Description("Rotate a JPG/PNG image by an arbitrary angle (degrees, clockwise), writing the result to a new (or the same) file.")]
	public Task<string> Rotate([Description("Relative path of the source image")] string relativePath, [Description("Relative path for the rotated output (can equal the source)")] string outputPath, [Description("Rotation angle in degrees, clockwise (e.g. 90, 180, -45)")] float degrees)
	{
		return Transform(relativePath, outputPath, delegate(Image image)
		{
			image.Mutate(delegate(IImageProcessingContext x)
			{
				x.Rotate(degrees);
			});
		});
	}

	[Description("Flip a JPG/PNG image horizontally or vertically, writing the result to a new (or the same) file.")]
	public Task<string> Flip([Description("Relative path of the source image")] string relativePath, [Description("Relative path for the flipped output (can equal the source)")] string outputPath, [Description("true = mirror left-right (horizontal), false = mirror top-bottom (vertical)")] bool horizontal)
	{
		return Transform(relativePath, outputPath, delegate(Image image)
		{
			image.Mutate(delegate(IImageProcessingContext x)
			{
				x.Flip(horizontal ? FlipMode.Horizontal : FlipMode.Vertical);
			});
		});
	}

	[Description("Convert a JPG/PNG image to grayscale, writing the result to a new (or the same) file.")]
	public Task<string> Grayscale([Description("Relative path of the source image")] string relativePath, [Description("Relative path for the grayscale output (can equal the source)")] string outputPath)
	{
		return Transform(relativePath, outputPath, delegate(Image image)
		{
			image.Mutate(delegate(IImageProcessingContext x)
			{
				x.Grayscale();
			});
		});
	}

	[Description("Convert a JPG/PNG image to the other format. The target format is inferred from outputPath's extension (.jpg/.jpeg or .png).")]
	public Task<string> ConvertFormat([Description("Relative path of the source image")] string relativePath, [Description("Relative path for the converted output, with the target extension (.jpg/.jpeg or .png)")] string outputPath)
	{
		return Transform(relativePath, outputPath, delegate
		{
		});
	}

	private async Task<string> Transform(string relativePath, string outputPath, Action<Image> mutate)
	{
		string fullPath = ResolveExisting(relativePath, out string error);
		if (fullPath == null)
		{
			return error;
		}
		string outputExtension = Path.GetExtension(outputPath);
		if (!SupportedExtensions.Contains<string>(outputExtension, StringComparer.OrdinalIgnoreCase))
		{
			return "ERROR: unsupported output extension '" + outputExtension + "' — only .jpg/.jpeg/.png are supported.";
		}
		string fullOutputPath;
		try
		{
			fullOutputPath = ResolveSafe(outputPath);
		}
		catch (UnauthorizedAccessException ex)
		{
			return "ERROR: " + ex.Message;
		}
		try
		{
			using Image image = await Image.LoadAsync(fullPath);
			mutate(image);
			Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath));
			await image.SaveAsync(fullOutputPath, GetEncoder(outputExtension));
			return $"OK — written {outputPath} ({image.Width}x{image.Height} px)";
		}
		catch (UnknownImageFormatException)
		{
			return "ERROR: not a recognizable JPG/PNG image — " + relativePath;
		}
		catch (ArgumentException ex3)
		{
			return "ERROR: " + ex3.Message;
		}
	}

	private static IImageEncoder GetEncoder(string extension)
	{
		string text = extension.ToLowerInvariant();
		if (1 == 0)
		{
		}
		IImageEncoder result;
		switch (text)
		{
		case ".jpg":
		case ".jpeg":
			result = new JpegEncoder();
			break;
		case ".png":
			result = new PngEncoder();
			break;
		default:
			throw new NotSupportedException("Unsupported image extension: " + extension);
		}
		if (1 == 0)
		{
		}
		return result;
	}

	private string? ResolveExisting(string relativePath, out string? error)
	{
		string extension = Path.GetExtension(relativePath);
		if (!SupportedExtensions.Contains<string>(extension, StringComparer.OrdinalIgnoreCase))
		{
			error = "ERROR: unsupported extension '" + extension + "' — only .jpg/.jpeg/.png are supported.";
			return null;
		}
		string text;
		try
		{
			text = ResolveSafe(relativePath);
		}
		catch (UnauthorizedAccessException ex)
		{
			error = "ERROR: " + ex.Message;
			return null;
		}
		if (!File.Exists(text))
		{
			error = "ERROR: file not found — " + relativePath;
			return null;
		}
		error = null;
		return text;
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
