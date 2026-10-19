using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OpenAI.Images;

namespace RalfAI.ImageGeneration;

public sealed record ImageReference(BinaryData Content, string FileName, string MediaType);

/// <summary>
/// Generates images through either an OpenAI image client or a Microsoft Foundry/OpenAI v1
/// endpoint. Project endpoints are preserved and normalized to /openai/v1/images/generations.
/// </summary>
public sealed class ImageGenerationService
{
    private static readonly HttpClient HttpClient = new() { Timeout = Timeout.InfiniteTimeSpan };

    private readonly ImageClient? client;
    private readonly Uri? endpoint;
    private readonly string? apiKey;
    private readonly bool useApiKeyHeader;

    public ImageGenerationService(ImageClient client, string modelId)
    {
        this.client = client;
        ModelId = modelId;
    }

    private ImageGenerationService(Uri endpoint, string apiKey, string modelId, bool useApiKeyHeader)
    {
        this.endpoint = endpoint;
        this.apiKey = apiKey;
        this.useApiKeyHeader = useApiKeyHeader;
        ModelId = modelId;
    }

    public string ModelId { get; }

    public static ImageGenerationService? FromConfig(IConfiguration config)
    {
        var provider = (config["AIProvider"] ?? "foundry").Trim().ToLowerInvariant();
        return provider switch
        {
            "openai" => FromOpenAI(config),
            "anthropic" => null,
            _ => FromFoundry(config)
        };
    }

    private static ImageGenerationService? FromOpenAI(IConfiguration config)
    {
        var apiKey = config["OpenAI:ApiKey"];
        var modelId = config["OpenAI:ImageModelId"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(modelId))
            return null;

        return new ImageGenerationService(
            new Uri("https://api.openai.com/v1/images/generations"),
            apiKey,
            modelId,
            useApiKeyHeader: false);
    }

    private static ImageGenerationService? FromFoundry(IConfiguration config)
    {
        var endpoint = config["AzureAIFoundry:Endpoint"];
        var apiKey = config["AzureAIFoundry:ApiKey"];
        var modelId = config["AzureAIFoundry:ImageModelId"];
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(modelId))
            return null;

        return new ImageGenerationService(
            BuildFoundryImageEndpoint(endpoint),
            apiKey,
            modelId,
            useApiKeyHeader: true);
    }

    public Task<(BinaryData? Bytes, string? Error)> GenerateAsync(
        string prompt,
        string size,
        CancellationToken ct = default) =>
        GenerateAsync(prompt, size, transparentBackground: false, ct);

    public async Task<(BinaryData? Bytes, string? Error)> GenerateAsync(
        string prompt,
        string size,
        bool transparentBackground,
        CancellationToken ct = default)
    {
        if (endpoint is not null)
            return await GenerateViaRestAsync(prompt, size, transparentBackground, ct);

        try
        {
            var options = new ImageGenerationOptions
            {
                Size = ParseSize(size),
                ResponseFormat = GeneratedImageFormat.Bytes
            };
            var result = await client!.GenerateImageAsync(prompt, options, ct);
            return (result.Value.ImageBytes, null);
        }
        catch (System.ClientModel.ClientResultException ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<(BinaryData? Bytes, string? Error)> GenerateAsync(
        string prompt,
        string size,
        bool transparentBackground,
        IReadOnlyList<ImageReference> references,
        CancellationToken ct = default)
    {
        if (references.Count == 0)
            return await GenerateAsync(prompt, size, transparentBackground, ct);

        if (references.Count > 16)
            return (null, "A maximum of 16 reference images is supported.");

        if (endpoint is null)
            return (null, "Reference-image generation requires the REST image edits endpoint.");

        return await EditViaRestAsync(prompt, size, transparentBackground, references, ct);
    }

    private async Task<(BinaryData? Bytes, string? Error)> GenerateViaRestAsync(
        string prompt,
        string size,
        bool transparentBackground,
        CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromMinutes(10));
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        if (useApiKeyHeader)
            request.Headers.Add("api-key", apiKey);
        else
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        request.Content = JsonContent.Create(new
        {
            model = ModelId,
            prompt,
            n = 1,
            size,
            quality = "medium",
            background = transparentBackground ? "transparent" : "opaque",
            output_format = "png"
        });

        try
        {
            using var response = await HttpClient.SendAsync(request, timeout.Token);
            var body = await response.Content.ReadAsStringAsync(timeout.Token);
            if (!response.IsSuccessStatusCode)
                return (null, BuildProviderError(response.StatusCode, body));

            using var json = JsonDocument.Parse(body);
            var base64 = json.RootElement.GetProperty("data")[0].GetProperty("b64_json").GetString();
            return string.IsNullOrWhiteSpace(base64)
                ? (null, "Provider returned no base64 image data.")
                : (BinaryData.FromBytes(Convert.FromBase64String(base64)), null);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return (null, "Image generation timed out after ten minutes.");
        }
    }

    private async Task<(BinaryData? Bytes, string? Error)> EditViaRestAsync(
        string prompt,
        string size,
        bool transparentBackground,
        IReadOnlyList<ImageReference> references,
        CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromMinutes(10));
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildImageEditEndpoint(endpoint!));
        if (useApiKeyHeader)
            request.Headers.Add("api-key", apiKey);
        else
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(ModelId), "model");
        form.Add(new StringContent(prompt), "prompt");
        form.Add(new StringContent("1"), "n");
        form.Add(new StringContent(size), "size");
        form.Add(new StringContent("high"), "quality");
        form.Add(new StringContent("high"), "input_fidelity");
        form.Add(new StringContent(transparentBackground ? "transparent" : "opaque"), "background");
        form.Add(new StringContent("png"), "output_format");

        foreach (var reference in references)
        {
            var mediaType = reference.MediaType.ToLowerInvariant();
            if (mediaType is not "image/png" and not "image/jpeg")
                return (null, $"Unsupported reference image type: {reference.MediaType}.");

            var image = new ByteArrayContent(reference.Content.ToArray());
            image.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            form.Add(image, "image[]", Path.GetFileName(reference.FileName));
        }

        request.Content = form;
        try
        {
            using var response = await HttpClient.SendAsync(request, timeout.Token);
            var body = await response.Content.ReadAsStringAsync(timeout.Token);
            if (!response.IsSuccessStatusCode)
                return (null, BuildProviderError(response.StatusCode, body));

            using var json = JsonDocument.Parse(body);
            var base64 = json.RootElement.GetProperty("data")[0].GetProperty("b64_json").GetString();
            return string.IsNullOrWhiteSpace(base64)
                ? (null, "Provider returned no base64 image data.")
                : (BinaryData.FromBytes(Convert.FromBase64String(base64)), null);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return (null, "Image editing timed out after ten minutes.");
        }
    }

    private static Uri BuildImageEditEndpoint(Uri generationEndpoint)
    {
        var value = generationEndpoint.ToString();
        const string generationPath = "/images/generations";
        return value.EndsWith(generationPath, StringComparison.OrdinalIgnoreCase)
            ? new Uri(value[..^generationPath.Length] + "/images/edits")
            : new Uri(value.TrimEnd('/') + "/images/edits");
    }

    private static Uri BuildFoundryImageEndpoint(string endpoint)
    {
        var uri = new Uri(endpoint.TrimEnd('/'));
        if (uri.Host.EndsWith(".services.ai.azure.com", StringComparison.OrdinalIgnoreCase) &&
            uri.AbsolutePath.StartsWith("/api/projects/", StringComparison.OrdinalIgnoreCase))
        {
            return new Uri($"{uri.Scheme}://{uri.Host}/openai/v1/images/generations");
        }

        var normalized = uri.ToString().TrimEnd('/');
        if (!normalized.EndsWith("/openai/v1", StringComparison.OrdinalIgnoreCase))
            normalized += "/openai/v1";

        return new Uri(normalized + "/images/generations");
    }

    private static string BuildProviderError(System.Net.HttpStatusCode statusCode, string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            var message = json.RootElement.GetProperty("error").GetProperty("message").GetString();
            if (!string.IsNullOrWhiteSpace(message))
                return $"HTTP {(int)statusCode}: {message}";
        }
        catch (JsonException)
        {
        }

        return $"HTTP {(int)statusCode} ({statusCode}).";
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
