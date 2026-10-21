using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace RalfAI.Tests;

public sealed class ImageAttachmentsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ralf-img-{Guid.NewGuid():N}");

    public ImageAttachmentsTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string MakePng(string name, int width, int height)
    {
        var path = Path.Combine(_root, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var image = new Image<Rgba32>(width, height);
        image.SaveAsPng(path);
        return path;
    }

    [Fact]
    public void AttachesMentionedImagesAndIgnoresEverythingElse()
    {
        MakePng("a.png", 10, 10);
        File.WriteAllText(Path.Combine(_root, "notes.md"), "x");

        var (images, warnings) = ImageAttachments.Resolve("look at @a.png, @notes.md and @missing.png please", _root);

        Assert.Equal("a.png", Assert.Single(images).FileName);
        Assert.Equal("image/png", images[0].MediaType);
        Assert.Empty(warnings);
    }

    [Fact]
    public void QuotedMentionsMayContainSpacesAndDuplicatesAreAttachedOnce()
    {
        MakePng("sub/my shot.png", 8, 8);

        var (images, _) = ImageAttachments.Resolve("@\"sub/my shot.png\" again @\"sub/my shot.png\"", _root);

        Assert.Single(images);
    }

    [Fact]
    public void LargeImagesAreScaledDownKeepingAspectRatio()
    {
        MakePng("big.png", 4000, 1000);

        var (images, _) = ImageAttachments.Resolve("@big.png", _root);

        var image = Assert.Single(images);
        Assert.True(image.Resized);
        Assert.Equal(2048, image.Width);
        Assert.Equal(512, image.Height);
    }

    [Fact]
    public void BrokenImageProducesAWarningInsteadOfThrowing()
    {
        File.WriteAllText(Path.Combine(_root, "bad.png"), "this is not a png");

        var (images, warnings) = ImageAttachments.Resolve("@bad.png", _root);

        Assert.Empty(images);
        Assert.Contains("bad.png", Assert.Single(warnings));
    }

    [Fact]
    public void AttachesAtMostEightImages()
    {
        for (var i = 0; i < 10; i++) MakePng($"p{i}.png", 4, 4);
        var text = string.Join(' ', Enumerable.Range(0, 10).Select(i => $"@p{i}.png"));

        var (images, warnings) = ImageAttachments.Resolve(text, _root);

        Assert.Equal(ImageAttachments.MaxImages, images.Count);
        Assert.Equal(2, warnings.Count);
    }
}
