using Anthropic;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

namespace RalfAI.Providers;

/// <summary>
/// Builds a Microsoft.Extensions.AI <see cref="IChatClient"/> from a shared, provider-agnostic
/// configuration shape (<c>AIProvider</c> selects one of "openai" / "anthropic" / "foundry", each
/// with its own settings section). Extracted out of RalfAI's AgentEngine so any project that reads
/// the same config shape - RalfAI itself, ComicForge, or whatever comes next - gets the same
/// provider wiring without depending on RalfAI's CLI/agent machinery.
///
/// Callers own how the <see cref="IConfiguration"/> is loaded (RalfAI uses RalfAIConfig.Load with
/// its RALFAI_CONFIG_PATH env var; other hosts can use their own convention) - this factory only
/// cares about the values once they're in an IConfiguration.
/// </summary>
public static class ChatClientFactory
{
    /// <param name="config">
    /// Expects: <c>AIProvider</c> ("openai" | "anthropic" | "foundry", default "foundry"), and a
    /// matching section: <c>OpenAI:ApiKey</c>/<c>OpenAI:ModelId</c>,
    /// <c>Anthropic:ApiKey</c>/<c>Anthropic:ModelId</c>, or <c>AzureAIFoundry:Endpoint</c>/
    /// <c>AzureAIFoundry:ApiKey</c>/<c>AzureAIFoundry:ModelId</c> (optionally
    /// <c>AzureAIFoundry:ContextModels:{contextKey}</c> + <c>AzureAIFoundry:Models:{name}:ModelId</c>
    /// to pick a different deployment per context).
    /// </param>
    /// <param name="contextKey">Optional key into AzureAIFoundry:ContextModels for per-context model selection.</param>
    public static (IChatClient Client, string ModelId) Create(IConfiguration config, string? contextKey = null) =>
        (config["AIProvider"] ?? "foundry").Trim().ToLowerInvariant() switch
        {
            "openai" => BuildOpenAI(config),
            "anthropic" => BuildAnthropic(config),
            _ => BuildFoundry(config, contextKey)
        };

    private static (IChatClient, string) BuildOpenAI(IConfiguration c)
    {
        var key = c["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey missing");
        var model = c["OpenAI:ModelId"] ?? "gpt-5.1";
        return (new OpenAIClient(key).GetChatClient(model).AsIChatClient(), model);
    }

    private static (IChatClient, string) BuildAnthropic(IConfiguration c)
    {
        var key = c["Anthropic:ApiKey"] ?? throw new InvalidOperationException("Anthropic:ApiKey missing");
        var model = c["Anthropic:ModelId"] ?? "claude-sonnet-4-5";
        return (new AnthropicClient { ApiKey = key }.AsIChatClient(model), model);
    }

    private static (IChatClient, string) BuildFoundry(IConfiguration c, string? contextKey)
    {
        var endpoint = c["AzureAIFoundry:Endpoint"] ?? throw new InvalidOperationException("AzureAIFoundry:Endpoint missing");
        var key = c["AzureAIFoundry:ApiKey"] ?? throw new InvalidOperationException("AzureAIFoundry:ApiKey missing");
        var named = contextKey is null ? null : c[$"AzureAIFoundry:ContextModels:{contextKey}"] ?? c["AzureAIFoundry:DefaultModel"];
        var model = named is null ? c["AzureAIFoundry:ModelId"] ?? "deepseek-r1" : c[$"AzureAIFoundry:Models:{named}:ModelId"] ?? named;
        var host = new Uri(endpoint).GetLeftPart(UriPartial.Authority);
        return (new AzureOpenAIClient(new Uri(host), new AzureKeyCredential(key)).GetChatClient(model).AsIChatClient(), model);
    }
}
