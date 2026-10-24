using Microsoft.Extensions.AI;
using RalfAI.Plugins;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using static RalfAI.Tests.ScriptedChatClient;

namespace RalfAI.Tests;

public sealed class ImageViewTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ralf-view-{Guid.NewGuid():N}");

    public ImageViewTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private void MakeImage(string name, int width, int height)
    {
        var path = Path.Combine(_root, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var image = new Image<Rgba32>(width, height, new Rgba32(200, 30, 30));
        if (name.EndsWith(".webp")) image.SaveAsWebp(path); else image.SaveAsPng(path);
    }

    private static ChatMessage ToolResult(string id, string text) => new(ChatRole.Tool, [new FunctionResultContent(id, text)]);

    // ── The tool ─────────────────────────────────────────────────────────────

    [Fact]
    public void TheToolChecksTheFilesAndSaysWhatItShows()
    {
        MakeImage("refs/a.png", 40, 20);
        File.WriteAllText(Path.Combine(_root, "logo.svg"), "<svg/>");

        var result = new ImageViewPlugin(_root).View(["refs/a.png", "logo.svg", "missing.png", "../outside.png"]);

        Assert.Contains("[image: refs/a.png | 40x20 | high]", result);
        Assert.Contains("logo.svg: SVG is text", result);
        Assert.Contains("missing.png: file not found", result);
        Assert.Contains("../outside.png: Path traversal blocked", result);
    }

    [Fact]
    public void LowDetailShrinksBigImagesAndWebPIsAccepted()
    {
        MakeImage("big.webp", 3000, 1500);

        var result = new ImageViewPlugin(_root).View(["big.webp"], "low");

        Assert.Contains($"[image: big.webp | {ImageViewPlugin.LowSide}x{ImageViewPlugin.LowSide / 2} | low]", result);
    }

    [Fact]
    public void AtMostEightImagesPerCall()
    {
        for (var i = 0; i < 10; i++) MakeImage($"f{i}.png", 4, 4);
        var result = new ImageViewPlugin(_root).View(Enumerable.Range(0, 10).Select(i => $"f{i}.png").ToArray());

        Assert.Equal(8, ImageViewPlugin.MarkerRegex().Matches(result).Count);
        Assert.Contains("only 8 images per call", result);
    }

    // ── Attaching the pictures ───────────────────────────────────────────────

    [Fact]
    public void ThePicturesFollowTheToolResultAsAUserMessage()
    {
        MakeImage("a.png", 8, 8);
        var client = new ImageViewingChatClient(new NoClient(), _root);
        var marker = ImageViewPlugin.Marker("a.png", 8, 8, "high");

        var sent = client.Attach([new(ChatRole.User, "guarda"), ToolResult("c1", "Showing 1 image(s):\n" + marker)]).ToList();

        Assert.Equal(3, sent.Count);
        Assert.Equal(ChatRole.User, sent[2].Role);
        var picture = Assert.Single(sent[2].Contents.OfType<DataContent>());
        Assert.Equal("image/png", picture.MediaType);
        Assert.Contains(sent[2].Contents.OfType<TextContent>(), t => t.Text == "Image: a.png");
    }

    [Fact]
    public void PicturesFromEarlierTurnsAreNotSentAgain()
    {
        MakeImage("a.png", 8, 8);
        var client = new ImageViewingChatClient(new NoClient(), _root);
        var marker = ImageViewPlugin.Marker("a.png", 8, 8, "high");
        List<ChatMessage> messages = [new(ChatRole.User, "guarda"), ToolResult("c1", marker), new(ChatRole.Assistant, "è rossa"), new(ChatRole.User, "e quindi?")];

        Assert.Same(messages, client.Attach(messages));
    }

    [Fact]
    public void WithoutAMarkerNothingChanges()
    {
        var client = new ImageViewingChatClient(new NoClient(), _root);
        List<ChatMessage> messages = [new(ChatRole.User, "x"), ToolResult("c1", "OK — written a.txt")];
        Assert.Same(messages, client.Attach(messages));
    }

    [Fact]
    public void AMarkerCannotReachOutsideTheProject()
    {
        var outside = Path.Combine(Path.GetTempPath(), $"ralf-outside-{Guid.NewGuid():N}.png");
        using (var image = new Image<Rgba32>(4, 4)) image.SaveAsPng(outside);
        try
        {
            var client = new ImageViewingChatClient(new NoClient(), _root);
            List<ChatMessage> messages = [new(ChatRole.User, "x"), ToolResult("c1", ImageViewPlugin.Marker("../" + Path.GetFileName(outside), 4, 4, "high"))];
            Assert.Same(messages, client.Attach(messages));
        }
        finally
        {
            File.Delete(outside);
        }
    }

    private sealed class NoClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }
}

/// <summary>Image.View inside the turn loop: the model asks to see a file and its next request carries the picture.</summary>
public sealed class EngineImageViewTests
{
    [Fact]
    public async Task TheModelSeesThePictureItAskedFor()
    {
        var client = Of(call => call.Index == 0
            ? [ToolCalls(("Image_View", new() { ["relativePaths"] = new[] { "shot.png" } }))]
            : [Text("vedo un quadrato rosso")]);
        using var h = await EngineHarness.CreateAsync(client);
        using (var image = new Image<Rgba32>(16, 16, new Rgba32(255, 0, 0))) image.SaveAsPng(h.PathOf("shot.png"));

        var result = await h.Engine.SendAsync("che cosa c'è in shot.png?");

        Assert.Equal("vedo un quadrato rosso", result.FinalText);
        Assert.Contains(client.Calls[1].Messages, m => m.Role == ChatRole.User && m.Contents.OfType<DataContent>().Any(d => d.MediaType == "image/png"));
        Assert.DoesNotContain(client.Calls[0].Messages, m => m.Contents.OfType<DataContent>().Any());        Assert.Contains(client.Calls[0].Options!.Tools!, t => t.Name == "Image_View");     // so the check below is not vacuous
    }

    [Fact]
    public async Task ItCanBeSwitchedOffForModelsWithoutVision()
    {
        var client = Of(_ => [Text("ok")]);
        using var h = await EngineHarness.CreateAsync(client, new() { ["Images:View"] = "false" });
        await h.Engine.SendAsync("ciao");

        Assert.DoesNotContain(client.Calls[0].Options!.Tools!, t => t.Name == "Image_View");
    }
}
