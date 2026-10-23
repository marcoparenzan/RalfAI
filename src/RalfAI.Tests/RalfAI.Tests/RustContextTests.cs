using System.Runtime.InteropServices;
using RalfAI;
using RalfAI.Contexts;
using RalfAI.Plugins;
using Xunit;

public class RustContextTests
{
    [Fact]
    public void RegistryIncludesRustWithAllItsTools()
    {
        var context = ContextRegistry.CreateBuiltIn("RUST");
        Assert.IsType<RustContext>(context);
        var names = context!.GetTools(Path.GetTempPath()).Select(tool => tool.Name).ToList();
        Assert.Contains("Rust_EnsureToolchain", names);
        Assert.Contains("Rust_RunCargo", names);
        Assert.Contains("Rust_RunRustup", names);
    }

    [Fact]
    public void WorkspaceTraversalIsRejected()
    {
        var root = Path.Combine(Path.GetTempPath(), "rust-workspace");
        Assert.Equal(Path.Combine(root, "src"), RustPlugin.ResolveDirectory(root, "src"));
        Assert.Throws<UnauthorizedAccessException>(() => RustPlugin.ResolveDirectory(root, "../rust-workspace-other"));
        Assert.Throws<UnauthorizedAccessException>(() => RustPlugin.ResolveDirectory(root, ".."));
    }

    [Theory]
    [InlineData("Windows", Architecture.X64, "x86_64-pc-windows-msvc")]
    [InlineData("Windows", Architecture.Arm64, "aarch64-pc-windows-msvc")]
    [InlineData("Linux", Architecture.X64, "x86_64-unknown-linux-gnu")]
    [InlineData("OSX", Architecture.Arm64, "aarch64-apple-darwin")]
    public void RustupTripleMatchesThePlatform(string platform, Architecture architecture, string expected)
        => Assert.Equal(expected, RustPlugin.RustupTriple(OSPlatform.Create(platform), architecture));

    [Fact]
    public void UnsupportedPlatformHasNoTriple()
        => Assert.Null(RustPlugin.RustupTriple(OSPlatform.Create("FreeBSD"), Architecture.X64));

    [Fact]
    public void ChecksumIsReadFromThePublishedFormat()
    {
        var hash = new string('A', 64);
        Assert.Equal(new string('a', 64), RustPlugin.ParseChecksum($"{hash} *rustup-init.exe\n"));
        Assert.Null(RustPlugin.ParseChecksum("<html>not found</html>"));
        Assert.Null(RustPlugin.ParseChecksum(new string('z', 64)));
    }

    [Fact]
    public void LongBuildLogsKeepHeadAndTail()
    {
        var text = "HEAD" + new string('x', 100_000) + "TAIL";
        var shortened = RustPlugin.Truncate(text);
        Assert.True(shortened.Length < 31_000);
        Assert.StartsWith("HEAD", shortened);
        Assert.EndsWith("TAIL", shortened);
        Assert.Contains("omitted", shortened);
    }

    [Fact]
    public void ProfileFilesCanEnableTheRustTools()
    {
        var names = RalfAI.Profiles.ToolCatalog.Resolve(["Rust"], Path.GetTempPath()).Select(t => t.Name).ToList();
        Assert.Equal(["Rust_EnsureToolchain", "Rust_RunCargo", "Rust_RunRustup"], names);
    }
}
