using System;
using System.ClientModel;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Images;

namespace RalfAI.ImageGeneration;

public sealed class ImageGenerationService(ImageClient client, string modelId)
{
	public string ModelId => modelId;

	public static ImageGenerationService? FromConfig(IConfiguration config)
	{
		string text = (config["AIProvider"] ?? "foundry").Trim().ToLowerInvariant();
		if (1 == 0)
		{
		}
		ImageGenerationService result = ((text == "openai") ? FromOpenAI(config) : ((!(text == "anthropic")) ? FromFoundry(config) : null));
		if (1 == 0)
		{
		}
		return result;
	}

	private static ImageGenerationService? FromOpenAI(IConfiguration config)
	{
		string text = config["OpenAI:ApiKey"];
		string text2 = config["OpenAI:ImageModelId"];
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return null;
		}
		ImageClient imageClient = new OpenAIClient(text).GetImageClient(text2);
		return new ImageGenerationService(imageClient, text2);
	}

	private static ImageGenerationService? FromFoundry(IConfiguration config)
	{
		string text = config["AzureAIFoundry:Endpoint"];
		string text2 = config["AzureAIFoundry:ApiKey"];
		string text3 = config["AzureAIFoundry:ImageModelId"];
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2) || string.IsNullOrWhiteSpace(text3))
		{
			return null;
		}
		string text4 = text.TrimEnd('/');
		if (text4.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
		{
			string text5 = text4;
			int length = "/chat/completions".Length;
			text4 = text5.Substring(0, text5.Length - length).TrimEnd('/');
		}
		string leftPart = new Uri(text4).GetLeftPart(UriPartial.Authority);
		ImageClient imageClient = new AzureOpenAIClient(new Uri(leftPart), new AzureKeyCredential(text2)).GetImageClient(text3);
		return new ImageGenerationService(imageClient, text3);
	}

	public async Task<(BinaryData? Bytes, string? Error)> GenerateAsync(string prompt, string size, CancellationToken ct = default(CancellationToken))
	{
		try
		{
			ImageGenerationOptions options = new ImageGenerationOptions
			{
				Size = ParseSize(size),
				ResponseFormat = GeneratedImageFormat.Bytes
			};
			return (Bytes: (await client.GenerateImageAsync(prompt, options, ct)).Value.ImageBytes, Error: null);
		}
		catch (ClientResultException ex)
		{
			ClientResultException ex2 = ex;
			return (Bytes: null, Error: ex2.Message);
		}
	}

	private static GeneratedImageSize ParseSize(string size)
	{
		if (1 == 0)
		{
		}
		GeneratedImageSize result = size switch
		{
			"256x256" => GeneratedImageSize.W256xH256, 
			"512x512" => GeneratedImageSize.W512xH512, 
			"1792x1024" => GeneratedImageSize.W1792xH1024, 
			"1024x1792" => GeneratedImageSize.W1024xH1792, 
			_ => GeneratedImageSize.W1024xH1024, 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
