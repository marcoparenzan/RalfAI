using RalfAI.Profiles;
using Xunit;

namespace RalfAI.Tests;

[CollectionDefinition(nameof(ContextStoreCollection), DisableParallelization = true)]
public sealed class ContextStoreCollection;

/// <summary>The contexts folder is process-wide, so these tests point it at a temporary folder one at a time.</summary>
[Collection(nameof(ContextStoreCollection))]
public sealed class ContextStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ralf-ctx-{Guid.NewGuid():N}");
    private readonly string _previous = ContextStore.Folder;

    public ContextStoreTests()
    {
        Directory.CreateDirectory(_root);
        ContextStore.UseFolder(Path.Combine(_root, "contexts"));
    }

    public void Dispose()
    {
        ContextStore.UseFolder(_previous);
        Directory.Delete(_root, recursive: true);
    }

    private string Project
    {
        get { var p = Path.Combine(_root, "project"); Directory.CreateDirectory(p); return p; }
    }

    private const string Minimal = """
        name: "Python"
        description: "Python packages"
        tools: [FileSystem, Web_FetchUrl]
        prompt: |
          You are a Python developer.

          ## Target Folder
          {{targetFolder}}
        """;

    // ── Shipped contexts ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("dotnet", ".NET 10", "DotNet_RunBuild")]
    [InlineData("dotnet11", ".NET 11", "DotNet_RunTest")]
    [InlineData("rust", "Rust", "Rust_RunCargo")]
    [InlineData("markdown", "Markdown", "Web_FetchUrl")]
    [InlineData("html", "HTML", "StaticServer_Start")]
    public void EveryShippedContextIsAValidFileWithItsTools(string key, string name, string tool)
    {
        var context = ContextRegistry.Create(key);

        Assert.Contains(name, context.Name);
        Assert.Contains(tool, context.GetTools(_root).Select(t => t.Name));
        Assert.Contains(_root, context.BuildSystemPrompt(_root));                // the placeholder is filled in
        Assert.DoesNotContain("{{targetFolder}}", context.BuildSystemPrompt(_root));
    }

    [Fact]
    public void ShippedContextsAreWrittenToTheContextsFolderTheFirstTime()
    {
        ContextRegistry.All().ToList();

        foreach (var key in new[] { "dotnet", "dotnet11", "rust", "markdown", "html" })
            Assert.True(File.Exists(Path.Combine(ContextStore.Folder, key + ".yaml")), key);
    }

    [Fact]
    public void ADeletedShippedContextDoesNotComeBack()
    {
        ContextStore.Seed();
        File.Delete(Path.Combine(ContextStore.Folder, "rust.yaml"));

        Assert.Empty(ContextStore.Seed());
        Assert.DoesNotContain(ContextRegistry.All(), c => c.Key == "rust");
    }

    [Fact]
    public void AShippedContextTheFolderNeverHadArrivesLater()
    {
        ContextStore.Seed();
        var list = Path.Combine(ContextStore.Folder, ".defaults");
        File.WriteAllLines(list, File.ReadAllLines(list).Where(l => l != "html"));      // as if 'html' came with a newer version
        File.Delete(Path.Combine(ContextStore.Folder, "html.yaml"));

        Assert.Equal(["html"], ContextStore.Seed());
    }

    [Fact]
    public void AnEditedShippedContextIsNeverOverwritten()
    {
        Directory.CreateDirectory(ContextStore.Folder);
        File.WriteAllText(Path.Combine(ContextStore.Folder, "dotnet.yaml"), Minimal);

        ContextStore.Seed();

        Assert.Equal("Python", ContextRegistry.Create("dotnet").Name);
    }

    // ── Registry ─────────────────────────────────────────────────────────────

    [Fact]
    public void TheContextDesignerIsAlwaysThereAndListedFirst()
    {
        var all = ContextRegistry.All().ToList();
        Assert.Equal(ContextRegistry.IntegratedName, all[0].Key);
        Assert.IsType<ContextDesignerContext>(ContextRegistry.Create("CONTEXTS"));
    }

    [Fact]
    public void ANewFileIsAContextAtOnce()
    {
        Directory.CreateDirectory(ContextStore.Folder);
        File.WriteAllText(Path.Combine(ContextStore.Folder, "python.yaml"), Minimal);

        Assert.Contains(ContextRegistry.All(), c => c.Key == "python" && c.Context.Name == "Python");
        Assert.Equal(["FileSystem_WriteFile", "FileSystem_EditFile"], ContextRegistry.Create("python").GetTools(_root).Select(t => t.Name).Take(2));
    }

    [Fact]
    public void AnUnknownContextNamesTheAvailableOnes()
    {
        var ex = Assert.Throws<ArgumentException>(() => ContextRegistry.Create("cobol"));
        Assert.Contains("contexts", ex.Message);
        Assert.Contains("markdown", ex.Message);
    }

    [Fact]
    public void WithTheDefaultDeletedTheDesignerStarts()
    {
        ContextStore.Seed();
        File.Delete(Path.Combine(ContextStore.Folder, ContextRegistry.DefaultName + ".yaml"));
        Assert.IsType<ContextDesignerContext>(ContextRegistry.Create(null));
    }

    [Fact]
    public void AProjectContextReplacesTheUserOneInThatProject()
    {
        Directory.CreateDirectory(ContextStore.ProjectFolder(Project));
        File.WriteAllText(Path.Combine(ContextStore.ProjectFolder(Project), "markdown.yaml"), Minimal);

        Assert.Equal("Python", ContextRegistry.Create("markdown", Project).Name);
        Assert.NotEqual("Python", ContextRegistry.Create("markdown").Name);
    }

    [Fact]
    public void OlderMarkdownProfilesStillWork()
    {
        Directory.CreateDirectory(ProfileLoader.ProjectFolder(Project));
        File.WriteAllText(Path.Combine(ProfileLoader.ProjectFolder(Project), "notes.md"),
            "---\nname: Notes\ndescription: d\ntools: [FileSystem]\n---\nTake notes in {{targetFolder}}.");

        Assert.Equal("Notes", ContextRegistry.Create("notes", Project).Name);
    }

    [Fact]
    public void ABrokenFileIsSkippedWithTheReason()
    {
        Directory.CreateDirectory(ContextStore.Folder);
        File.WriteAllText(Path.Combine(ContextStore.Folder, "broken.yaml"), "name: x\npromt: |\n  typo\n");
        var errors = new List<string>();
        ContextRegistry.ProfileError = errors.Add;
        try
        {
            Assert.DoesNotContain(ContextRegistry.All(), c => c.Key == "broken");
            Assert.Contains(errors, e => e.Contains("broken.yaml") && e.Contains("promt") && e.Contains("line 2"));
        }
        finally
        {
            ContextRegistry.ProfileError = null;
        }
    }

    // ── Parsing and saving ───────────────────────────────────────────────────

    [Fact]
    public void ATypoInAToolNameIsCaughtWhenTheFileIsRead()
    {
        var ex = Assert.Throws<ArgumentException>(() => ContextStore.Parse(Minimal.Replace("Web_FetchUrl", "Web_Fetch"), "x"));
        Assert.Contains("Web_Fetch", ex.Message);
    }

    [Fact]
    public void APromptIsRequired()
        => Assert.Throws<FormatException>(() => ContextStore.Parse("name: x\ntools: [FileSystem]\n", "x"));

    [Fact]
    public void ExportingAndReadingBackGivesTheSameContext()
    {
        var original = ContextRegistry.Create("rust");
        var copy = ContextStore.Parse(ContextStore.ToYaml(original), "rust");

        Assert.Equal(original.Name, copy.Name);
        Assert.Equal(original.Description, copy.Description);
        Assert.Equal(original.BuildSystemPrompt(_root), copy.BuildSystemPrompt(_root));
        Assert.Equal(original.GetTools(_root).Select(t => t.Name), copy.GetTools(_root).Select(t => t.Name));
    }

    [Fact]
    public void SavingChecksTheFileFirst()
    {
        Assert.Throws<ArgumentException>(() => ContextStore.Save("bad", Minimal.Replace("FileSystem,", "Files,")));
        Assert.False(File.Exists(Path.Combine(ContextStore.Folder, "bad.yaml")));

        var path = ContextStore.Save("python", Minimal);
        Assert.Equal(Path.Combine(ContextStore.Folder, "python.yaml"), path);
        Assert.Equal("Python", ContextRegistry.Create("python").Name);
    }

    [Theory]
    [InlineData("contexts")]
    [InlineData("../escape")]
    [InlineData("with space")]
    [InlineData("")]
    public void NamesThatAreNotPlainFileNamesOrTheDesignerAreRefused(string key)
        => Assert.Throws<ArgumentException>(() => ContextStore.Save(key, Minimal));

    [Fact]
    public void SavingInTheProjectKeepsItOutOfOtherProjects()
    {
        ContextStore.Save("local", Minimal, Project);

        Assert.True(File.Exists(Path.Combine(ContextStore.ProjectFolder(Project), "local.yaml")));
        Assert.Throws<ArgumentException>(() => ContextRegistry.Create("local"));
    }

    // ── The designer's tools ─────────────────────────────────────────────────

    [Fact]
    public void TheDesignerGetsTheReasonWhenAContextIsWrong()
    {
        var plugin = new ContextsPlugin(Project);
        Assert.StartsWith("Not saved:", plugin.SaveContext("python", "name: x\n"));
        Assert.StartsWith("OK", plugin.SaveContext("python", Minimal));
        Assert.Contains("python - Python", plugin.ListContexts());
        Assert.Equal(Minimal.ReplaceLineEndings("\n"), plugin.ReadContext("python"));
        Assert.StartsWith("OK", plugin.DeleteContext("python"));
    }

    [Fact]
    public void TheDesignerCanListEveryToolWithWhatItDoes()
    {
        var tools = new ContextsPlugin(Project).ListTools();
        Assert.Contains("## FileSystem", tools);
        Assert.Contains("- Rust_RunCargo:", tools);
        Assert.Contains("- Contexts_SaveContext:", tools);
    }

    [Fact]
    public void TheDesignerCannotWriteProjectFiles()
    {
        var names = new ContextDesignerContext().GetTools(Project).Select(t => t.Name).ToList();
        Assert.Contains("Contexts_SaveContext", names);
        Assert.Contains("FileSystem_ReadFile", names);
        Assert.DoesNotContain(names, n => n is "FileSystem_WriteFile" or "FileSystem_EditFile" or "FileSystem_DeleteFile");
    }

    [Fact]
    public void TheDesignerPromptTellsWhereContextsAreAndWhichToolsExist()
    {
        var prompt = new ContextDesignerContext().BuildSystemPrompt(Project);
        Assert.Contains(ContextStore.Folder, prompt);
        Assert.Contains("{{targetFolder}}", prompt);                 // explained to the model, not replaced
        Assert.Contains("StaticServer", prompt);
    }
}

public sealed class ContextFolderTests
{
    [Fact]
    public void TheSettingWinsThenTheFolderOfTheConfigFileThenTheHomeFolder()
    {
        Assert.Equal(Path.GetFullPath(@"D:\elsewhere"), ContextStore.DefaultFolder(@"D:\elsewhere", @"D:\Configurations\RalfAI\config.json"));
        Assert.Equal(Path.Combine(@"D:\Configurations\RalfAI", "contexts"), ContextStore.DefaultFolder(null, @"D:\Configurations\RalfAI\config.json"));
        Assert.EndsWith(Path.Combine(".ralfai", "contexts"), ContextStore.DefaultFolder(null, null));
    }

    [Fact]
    public void TheConfigSettingMayNameTheFileOrItsFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"ralf-cfg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try
        {
            var file = Path.Combine(folder, "config.json");
            Assert.Equal(file, RalfAIConfig.ExternalPath(file));
            Assert.Equal(file, RalfAIConfig.ExternalPath(folder));                    // nothing there yet: the file it expects

            File.WriteAllText(Path.Combine(folder, "appsettings.json"), "{}");
            Assert.Equal(Path.Combine(folder, "appsettings.json"), RalfAIConfig.ExternalPath(folder + Path.DirectorySeparatorChar));

            File.WriteAllText(file, "{}");
            Assert.Equal(file, RalfAIConfig.ExternalPath($"\"{folder}\""));         // config.json first; quotes from a .cmd are tolerated
            Assert.Equal(Path.Combine(folder, "contexts"), ContextStore.DefaultFolder(null, RalfAIConfig.ExternalPath(folder)));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
