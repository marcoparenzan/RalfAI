using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Images;

namespace RalfAI.ImageGeneration;

/// <summary>
/// Wraps AI text-to-image generation (Azure AI Foundry or native OpenAI — DALL-E / gpt-image
/// deployments). Optional, like <see cref="RalfAI.Voice.SpeechService"/>: <see cref="FromConfig"/>
/// returns null when there's no image-generation model configured for the active provider, so
/// RalfAI works fine without it. Anthropic has no image-generation API, so it never resolves
/// one regardless of config.
/// </summary>
public sealed class ImageGenerationService(ImageClient client, string modelId)
{
    public string ModelId => modelId;

    public static ImageGenerationService? FromConfig(IConfiguration config)
    {
        var provider = (config["AIProvider"] ?? "foundry").Trim().ToLowerInvariant();

        return provider switch
        {
            "openai" => FromOpenAI(config),
            "anthropic" => null,
            _ => FromFoundry(config),
        };
    }

    private static ImageGenerationService? FromOpenAI(IConfiguration config)
    {
        var apiKey = config["OpenAI:ApiKey"];
        var modelId = config["OpenAI:ImageModelId"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(modelId))
            return null;

        var client = new OpenAIClient(apiKey).GetImageClient(modelId);
        return new ImageGenerationService(client, modelId);
    }

    private static ImageGenerationService? FromFoundry(IConfiguration config)
    {
        var endpoint = config["AzureAIFoundry:Endpoint"];
        var apiKey = config["AzureAIFoundry:ApiKey"];
        var modelId = config["AzureAIFoundry:ImageModelId"];
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(modelId))
            return null;

        // Same host-normalization as the chat client: strip a project/…/chat-completions path
        // down to the bare resource host.
        var url = endpoint.TrimEnd('/');
        if (url.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            url = url[..^"/chat/completions".Length].TrimEnd('/');
        var host = new Uri(url).GetLeftPart(UriPartial.Authority);

        var client = new AzureOpenAIClient(new Uri(host), new AzureKeyCredential(apiKey)).GetImageClient(modelId);
        return new ImageGenerationService(client, modelId);
    }

    /// <summary>
    /// Generates one image from a text prompt. Returns the raw image bytes on success, or an
    /// error message on failure (invalid prompt, content policy rejection, quota, …).
    /// </summary>
    public async Task<(BinaryData? Bytes, string? Error)> GenerateAsync(
        string prompt, string size, CancellationToken ct = default)
    {
        try
        {
            var options = new ImageGenerationOptions
            {
                Size = ParseSize(size),
                ResponseFormat = GeneratedImageFormat.Bytes
            };

            var result = await client.GenerateImageAsync(prompt, options, ct);
            return (result.Value.ImageBytes, null);
        }
        catch (System.ClientModel.ClientResultException ex)
        {
            return (null, ex.Message);
        }
    }

    private static GeneratedImageSize ParseSize(string size) => size switch
    {
        "256x256" => GeneratedImageSize.W256xH256,
        "512x512" => GeneratedImageSize.W512xH512,
        "1792x1024" => GeneratedImageSize.W1792xH1024,
        "1024x1792" => GeneratedImageSize.W1024xH1792,
        _ => GeneratedImageSize.W1024xH1024
    };
}
