using System.Net;
using Microsoft.Extensions.Configuration;
using RalfAI.Plugins;
using RalfAI.Profiles;
using Xunit;

namespace RalfAI.Tests;

public sealed class ContextSyncTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ralf-sync-{Guid.NewGuid():N}");
    private string Source => Path.Combine(_root, "source");
    private string Local => Path.Combine(_root, "contexts");

    public ContextSyncTests()
    {
        Directory.CreateDirectory(Source);
        Directory.CreateDirectory(Local);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private static string Context(string name, string body = "Work in {{targetFolder}}.")
        => $"name: \"{name}\"\ndescription: \"d\"\ntools: [FileSystem]\nprompt: |\n  {body}\n";

    /// <summary>Publishes contexts in the source folder, with an index like config/contexts/index.yaml.</summary>
    private void Publish(params (string Name, int Version, string Yaml, string[] Previous)[] contexts)
    {
        var lines = new List<string> { "updated: 2026-10-24", "contexts:" };
        foreach (var (name, version, yaml, previous) in contexts)
        {
            File.WriteAllText(Path.Combine(Source, name + ".yaml"), yaml);
            lines.Add($"  - name: {name}\n    version: {version}\n    sha256: {ContextSync.Hash(yaml)}");
            if (previous.Length > 0) lines.Add("    previous: [" + string.Join(", ", previous) + "]");
        }
        File.WriteAllText(Path.Combine(Source, "index.yaml"), string.Join("\n", lines) + "\n");
    }

    private Task<ContextSyncReport> Sync() => ContextSync.SyncAsync(new FolderContextSource(Source), Local, markChecked: true);
    private string LocalText(string name) => File.ReadAllText(Path.Combine(Local, name + ".yaml"));

    // ── Adding, updating, keeping ────────────────────────────────────────────

    [Fact]
    public async Task ContextsTheFolderLacksAreDownloaded()
    {
        Publish(("python", 1, Context("Python"), []));

        var report = await Sync();

        Assert.Equal(["python"], report.Added);
        Assert.Contains("Python", LocalText("python"));
    }

    [Fact]
    public async Task ANewerVersionReplacesAFileNobodyChanged()
    {
        Publish(("python", 1, Context("Python"), []));
        await Sync();
        Publish(("python", 2, Context("Python 3"), [ContextSync.Hash(Context("Python"))]));

        var report = await Sync();

        Assert.Equal(["python"], report.Updated);
        Assert.Contains("Python 3", LocalText("python"));
    }

    [Fact]
    public async Task AFileTheUserChangedIsKeptAndReportedOncePerVersion()
    {
        Publish(("python", 1, Context("Python"), []));
        await Sync();
        File.WriteAllText(Path.Combine(Local, "python.yaml"), Context("Il mio Python"));
        Publish(("python", 2, Context("Python 3"), []));

        Assert.Equal(["python"], (await Sync()).KeptChanged);
        Assert.Empty((await Sync()).KeptChanged);                                // not again for the same version
        Assert.Contains("Il mio Python", LocalText("python"));
    }

    [Fact]
    public async Task ADeletedContextStaysDeleted()
    {
        Publish(("python", 1, Context("Python"), []));
        await Sync();
        File.Delete(Path.Combine(Local, "python.yaml"));
        Publish(("python", 2, Context("Python 3"), []));

        var report = await Sync();

        Assert.False(report.Any);
        Assert.False(File.Exists(Path.Combine(Local, "python.yaml")));
    }

    [Fact]
    public async Task AUserFileWithTheSameNameIsNeverTouched()
    {
        File.WriteAllText(Path.Combine(Local, "python.yaml"), Context("Mio"));
        Publish(("python", 1, Context("Python"), []));
        await Sync();
        Publish(("python", 2, Context("Python 3"), []));
        await Sync();

        Assert.Contains("Mio", LocalText("python"));
    }

    [Fact]
    public async Task LineEndingsDoNotMakeAFileLookChanged()
    {
        Publish(("python", 1, Context("Python"), []));
        await Sync();
        File.WriteAllText(Path.Combine(Local, "python.yaml"), LocalText("python").ReplaceLineEndings("\r\n"));    // a Git checkout on Windows
        Publish(("python", 2, Context("Python 3"), []));

        Assert.Equal(["python"], (await Sync()).Updated);
    }

    // ── What a source cannot do ──────────────────────────────────────────────

    [Theory]
    [InlineData("../outside")]
    [InlineData("contexts")]
    public async Task AnIndexCannotWriteOutsideTheFolderOrReplaceTheDesigner(string name)
    {
        File.WriteAllText(Path.Combine(Source, "index.yaml"), $"contexts:\n  - name: \"{name}\"\n    version: 1\n");

        var report = await Sync();

        Assert.Single(report.Rejected);
        Assert.Empty(Directory.GetFiles(_root, "*.yaml", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.GetFiles(Local, "*.yaml"));                          // nothing written in the folder either
    }

    [Fact]
    public async Task ABrokenContextIsNotWritten()
    {
        Publish(("python", 1, "name: x\ntools: [NoSuchGroup]\nprompt: |\n  p\n", []));

        var report = await Sync();

        Assert.Contains(report.Rejected, r => r.StartsWith("python v1") && r.Contains("NoSuchGroup"));
        Assert.False(File.Exists(Path.Combine(Local, "python.yaml")));
    }

    [Fact]
    public async Task AnUnreachableSourceFailsWithoutChangingAnything()
    {
        await Assert.ThrowsAnyAsync<IOException>(() => ContextSync.SyncAsync(new FolderContextSource(Path.Combine(_root, "nowhere")), Local, true));
        Assert.False(File.Exists(Path.Combine(Local, ContextSync.StateFile)));
    }

    // ── From the version that kept only names ────────────────────────────────

    [Fact]
    public async Task AFileWrittenByTheOlderVersionIsUpdatedIfUnchangedAndKeptIfChanged()
    {
        var old = Context("Vecchio");
        File.WriteAllText(Path.Combine(Local, "a.yaml"), old);
        File.WriteAllText(Path.Combine(Local, "b.yaml"), Context("Cambiato a mano"));
        File.WriteAllLines(Path.Combine(Local, ".defaults"), ["# list", "a", "b"]);
        Publish(("a", 2, Context("Nuovo"), [ContextSync.Hash(old)]), ("b", 2, Context("Nuovo"), [ContextSync.Hash(old)]));

        var report = await Sync();

        Assert.Equal(["a"], report.Updated);
        Assert.Equal(["b"], report.KeptChanged);
        Assert.False(File.Exists(Path.Combine(Local, ".defaults")));
    }

    // ── The contexts that ship with RalfAI ───────────────────────────────────

    [Fact]
    public async Task EveryShippedContextMatchesItsIndexEntry()
    {
        var source = new EmbeddedContextSource();
        var index = ContextIndex.Parse(await source.ReadAsync("index.yaml", default));

        Assert.Equal(["dotnet", "dotnet10", "dotnet11", "rust", "markdown", "html"], index.Contexts.Select(c => c.Name));
        foreach (var offer in index.Contexts)
        {
            var yaml = await source.ReadAsync(offer.Name + ".yaml", default);
            // Changed a file in config/contexts? Raise its version, move the old sha256 to 'previous' and put this one.
            Assert.True(ContextSync.Hash(yaml) == offer.Sha256, $"{offer.Name}.yaml does not match index.yaml: sha256 should be {ContextSync.Hash(yaml)}");
            Assert.DoesNotContain(offer.Sha256!, offer.Previous);
            ContextStore.Parse(yaml, offer.Name);
        }
    }

    [Fact]
    public async Task DotnetIsTheSameAsTheLatestVersionedContext()
    {
        var source = new EmbeddedContextSource();
        var latest = ContextStore.Parse(await source.ReadAsync("dotnet.yaml", default), "dotnet");
        var ten = ContextStore.Parse(await source.ReadAsync("dotnet10.yaml", default), "dotnet10");
        Assert.Equal(ten.BuildSystemPrompt("x"), latest.BuildSystemPrompt("x"));
        Assert.Equal(ten.Tools, latest.Tools);
    }

    // ── When to look, and where ──────────────────────────────────────────────

    private static IConfiguration Config(params (string Key, string Value)[] values)
        => new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(v => v.Key, v => (string?)v.Value)).Build();

    [Fact]
    public async Task CheckingIsDueOnceByDefaultThenOnlyWhenAskedOrWhenItIsTime()
    {
        var now = DateTimeOffset.Now;
        Assert.True(ContextSync.IsDue(Config(), Local, false, now));                          // never checked

        Publish(("python", 1, Context("Python"), []));
        await Sync();                                                                        // records the check
        Assert.False(ContextSync.IsDue(Config(), Local, false, now));
        Assert.False(ContextSync.IsDue(Config(("Contexts:Check", "false")), Local, false, now));
        Assert.True(ContextSync.IsDue(Config(("Contexts:Check", "true")), Local, false, now));
        Assert.True(ContextSync.IsDue(Config(("Contexts:Check", "false")), Local, sourceGivenOnCommandLine: true, now));
        Assert.False(ContextSync.IsDue(Config(("Contexts:Check", "false"), ("Contexts:CheckEveryDays", "7")), Local, false, now.AddDays(6)));
        Assert.True(ContextSync.IsDue(Config(("Contexts:Check", "false"), ("Contexts:CheckEveryDays", "7")), Local, false, now.AddDays(8)));
    }

    [Theory]
    [InlineData("https://github.com/marcoparenzan/RalfAI/config/contexts", "https://raw.githubusercontent.com/marcoparenzan/RalfAI/main/config/contexts/")]
    [InlineData("http://github.com/marcoparenzan/ralfai/config/contexts/", "https://raw.githubusercontent.com/marcoparenzan/ralfai/main/config/contexts/")]
    [InlineData("https://github.com/o/r/tree/dev/ctx", "https://raw.githubusercontent.com/o/r/dev/ctx/")]
    [InlineData("https://github.com/o/r/blob/v2/a/b", "https://raw.githubusercontent.com/o/r/v2/a/b/")]
    [InlineData("https://example.com/ctx", "https://example.com/ctx/")]
    public void GitHubPageAddressesBecomeRawFileAddresses(string address, string expected)
        => Assert.Equal(expected, ContextSync.RawGitHubUri(address).ToString());

    [Fact]
    public void ASourceThatIsNotAnAddressIsAFolder()
        => Assert.IsType<FolderContextSource>(ContextSync.CreateSource(Source));

    [Fact]
    public async Task AWebSourceReadsFilesUnderItsAddressAndReportsErrors()
    {
        using var client = new HttpClient(new FakeHandler(req => req.RequestUri!.AbsolutePath.EndsWith("/ctx/index.yaml")
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("contexts: []") }
            : new HttpResponseMessage(HttpStatusCode.NotFound)));
        var source = new HttpContextSource(new Uri("https://example.com/ctx/"), client);

        Assert.Equal("contexts: []", await source.ReadAsync("index.yaml", default));
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => source.ReadAsync("x.yaml", default));
        Assert.Contains("404", ex.Message);
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }

    // ── Turning the check off in the config file ─────────────────────────────

    [Fact]
    public void TheCheckIsTurnedOffInTheConfigFileKeepingEverythingElse()
    {
        var path = Path.Combine(_root, "config.json");
        File.WriteAllText(path, """{ "AzureAIFoundry": { "Model": "gpt-5", "Note": "è così" }, "Contexts": { "Source": "D:\\ctx" } }""");

        Assert.Null(ContextSync.TryTurnOffCheck(path));

        var config = new ConfigurationBuilder().AddJsonFile(path).Build();
        Assert.Equal("False", config["Contexts:Check"], ignoreCase: true);
        Assert.Equal("gpt-5", config["AzureAIFoundry:Model"]);
        Assert.Equal("è così", config["AzureAIFoundry:Note"]);
        Assert.Equal(@"D:\ctx", config["Contexts:Source"]);
        Assert.Contains("è così", File.ReadAllText(path));                                   // written readable, not escaped
    }

    [Fact]
    public void AConfigFileWithCommentsIsNotRewritten()
    {
        var path = Path.Combine(_root, "config.json");
        var text = "{\n  // my key\n  \"A\": 1\n}";
        File.WriteAllText(path, text);

        Assert.Contains("comments", ContextSync.TryTurnOffCheck(path));
        Assert.Equal(text, File.ReadAllText(path));
    }

    [Fact]
    public void WithoutAConfigFileOfYourOwnNothingIsWritten() => Assert.NotNull(ContextSync.TryTurnOffCheck(null));
}

public sealed class DotNetToolTests
{
    [Fact]
    public void ToolProjectSettingsAreReadFromTheProjectFile()
    {
        var info = ToolProject.Read("""
            <Project><PropertyGroup><PackAsTool>true</PackAsTool><ToolCommandName>ralf</ToolCommandName>
            <PackageId>RalfAI</PackageId><Version> 1.2.3 </Version></PropertyGroup></Project>
            """, "RalfAI");
        Assert.Equal(new ToolProject(true, "RalfAI", "1.2.3", "ralf"), info);
    }

    [Fact]
    public void WithoutAPackageIdTheAssemblyOrProjectNameIsUsed()
    {
        Assert.Equal("my-tool", ToolProject.Read("<AssemblyName>my-tool</AssemblyName>", "MyTool").PackageId);
        Assert.Equal("MyTool", ToolProject.Read("<Project/>", "MyTool").PackageId);
        Assert.False(ToolProject.Read("<Project/>", "MyTool").PackAsTool);
    }

    [Fact]
    public async Task AProjectThatIsNotAToolIsExplainedNotPacked()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ralf-tool-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "App.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType></PropertyGroup></Project>");
            var result = await new DotNetPlugin(root).InstallGlobalTool("App.csproj");
            Assert.Contains("PackAsTool", result);
            Assert.False(Directory.Exists(Path.Combine(root, ".ralfai", "nupkg")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void PublishingIsAllowedAndInstallingAToolAsks()
    {
        var policy = new PermissionPolicy(PermissionMode.Ask);
        var publish = PermissionPolicy.Describe("DotNet_RunPublish", [new("projectPath", "src/App/App.csproj")])!;
        var install = PermissionPolicy.Describe("DotNet_InstallGlobalTool", [new("projectPath", "src/Tool/Tool.csproj")])!;

        Assert.Equal(PermissionDecision.Allow, policy.Evaluate(publish));
        Assert.Equal(PermissionDecision.Ask, policy.Evaluate(install));
        Assert.Contains("installs", PermissionPolicy.WhyAsk(install));
    }

    [Fact]
    public void TheDotNetGroupHasPublishAndGlobalToolInstall()
    {
        var names = ToolCatalog.Resolve(["DotNet"], Path.GetTempPath()).Select(t => t.Name).ToList();
        Assert.Contains("DotNet_RunPublish", names);
        Assert.Contains("DotNet_InstallGlobalTool", names);
    }
}
