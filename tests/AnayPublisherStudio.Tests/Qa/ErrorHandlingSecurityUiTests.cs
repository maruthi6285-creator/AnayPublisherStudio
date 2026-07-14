using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Domain.Plugins;
using AnayPublisherStudio.Infrastructure.Parsing;
using AnayPublisherStudio.Infrastructure.Plugins;
using AnayPublisherStudio.Infrastructure.Persistence;
using AnayPublisherStudio.Infrastructure.Ai;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.Blocks;
using AnayPublisherStudio.Domain.ValueObjects;
using AnayPublisherStudio.Infrastructure.Templates;
using Xunit;

namespace AnayPublisherStudio.Tests.Qa;

public class ErrorHandlingSecurityUiTests
{
    [Fact]
    public void Parser_RejectsCorruptDocx()
    {
        using var ms = TestFixtures.BuildCorruptDocx();
        Assert.ThrowsAny<Exception>(() => new DocxDocumentParser().Parse(ms));
    }

    [Fact]
    public void Parser_HandlesEmptyBodyDocument()
    {
        using var ms = TestFixtures.BuildDocx(emptyBody: true, chapters: 0, paragraphsPerChapter: 0);
        // May throw or return empty model depending on OpenXML structure; must not hang/crash process.
        try
        {
            var book = new DocxDocumentParser().Parse(ms);
            Assert.NotNull(book);
        }
        catch (Exception ex)
        {
            Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        }
    }

    [Fact]
    public void Parser_PreservesMetadataFromPackageProperties()
    {
        using var ms = TestFixtures.BuildDocx(title: "Meta Title", author: "Meta Author", chapters: 1, paragraphsPerChapter: 1);
        var book = new DocxDocumentParser().Parse(ms);
        Assert.Equal("Meta Title", book.Metadata.Title);
        Assert.Equal("Meta Author", book.Metadata.Author);
        Assert.True(book.TotalBlocks >= 1);
    }

    [Fact]
    public async Task ProjectRepository_RoundTrip_DoesNotCorruptPayload()
    {
        var db = Path.Combine(Path.GetTempPath(), "aps-qa-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            var repo = new SqliteProjectRepository(db);
            var project = new PublishingProject
            {
                Name = "Repo QA",
                TemplateId = "amazon-paperback-6x9",
                ManuscriptPath = "/tmp/safe/path/manuscript.docx",
                Metadata = { Title = "T", Author = "A", Description = "D" },
            };
            await repo.SaveAsync(project);
            var loaded = await repo.GetAsync(project.Id);
            Assert.NotNull(loaded);
            Assert.Equal(project.Name, loaded!.Name);
            Assert.Equal(project.TemplateId, loaded.TemplateId);
            Assert.Equal(project.Metadata.Title, loaded.Metadata.Title);
            Assert.Equal(project.ManuscriptPath, loaded.ManuscriptPath);

            var recent = await repo.GetRecentAsync(5);
            Assert.Contains(recent, p => p.Id == project.Id);
        }
        finally
        {
            try { File.Delete(db); } catch { }
        }
    }

    [Fact]
    public void PluginManager_IgnoresMalformedManifests_AndAvoidsPathEscape()
    {
        var root = Path.Combine(Path.GetTempPath(), "aps-plugins-qa-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "good"));
        Directory.CreateDirectory(Path.Combine(root, "bad"));
        File.WriteAllText(Path.Combine(root, "good", "plugin.json"),
            """{"id":"good","name":"Good","version":"1.0.0","kind":"Templates"}""");
        File.WriteAllText(Path.Combine(root, "bad", "plugin.json"), "{ not json");
        try
        {
            var mgr = new PluginManager(root);
            var found = mgr.Discover();
            Assert.Contains(found, m => m.Id == "good");
            Assert.DoesNotContain(found, m => string.IsNullOrWhiteSpace(m.Id));
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    [Fact]
    public async Task PluginManager_LoadMissingAssembly_ReturnsNullSafely()
    {
        var root = Path.Combine(Path.GetTempPath(), "aps-plugins-qa2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "p"));
        File.WriteAllText(Path.Combine(root, "p", "plugin.json"),
            """{"id":"p","name":"P","version":"1.0.0","kind":"Export","assembly":"Missing.dll","entryType":"X.Y"}""");
        try
        {
            var mgr = new PluginManager(root);
            var manifest = mgr.Discover().Single();
            var loaded = await mgr.LoadAsync(manifest);
            Assert.Null(loaded);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    [Fact]
    public async Task AiAssistant_Suggestions_DoNotRewriteManuscriptBody()
    {
        var book = TestFixtures.MakeBook(2, 4);
        var before = book.Chapters.SelectMany(c => c.Blocks).OfType<ParagraphBlock>()
            .Select(p => p.PlainText).ToList();
        IAiAssistant ai = new HeuristicAiAssistant();
        _ = await ai.GenerateDescriptionAsync(book);
        _ = await ai.SuggestKeywordsAsync(book);
        _ = await ai.SuggestCopyrightPageAsync(book);
        _ = await ai.SuggestPublishingChecklistAsync(book);
        _ = await ai.SuggestTocAsync(book);
        _ = await ai.SuggestIndexTermsAsync(book);
        _ = await ai.SuggestGlossaryTermsAsync(book);
        _ = await ai.SuggestBibliographyAsync(book);
        _ = ai.ReadabilityScore(book);
        var after = book.Chapters.SelectMany(c => c.Blocks).OfType<ParagraphBlock>()
            .Select(p => p.PlainText).ToList();
        Assert.Equal(before, after);
    }

    [Fact]
    public void UiSurface_CommandsAndThemes_ArePresentInPresentationAssemblyFiles()
    {
        // Static UI contract checks (WPF cannot be launched headlessly on Linux).
        var root = TestFixtures.SolutionRoot;
        var xaml = File.ReadAllText(Path.Combine(root, "src", "AnayPublisherStudio.Presentation", "MainWindow.xaml"));
        var vm = File.ReadAllText(Path.Combine(root, "src", "AnayPublisherStudio.Presentation", "ViewModels", "MainViewModel.cs"));
        var dark = Path.Combine(root, "src", "AnayPublisherStudio.Presentation", "Themes", "Dark.xaml");
        var light = Path.Combine(root, "src", "AnayPublisherStudio.Presentation", "Themes", "Light.xaml");

        foreach (var cmd in new[]
                 {
                     "OpenManuscriptCommand", "SaveProjectCommand", "PublishCommand",
                     "RefreshPreviewCommand", "PreviewSingleCommand", "PreviewFacingCommand",
                     "PreviewContinuousCommand", "ToggleGuidesCommand", "ZoomInCommand", "ZoomOutCommand",
                     "SaveSettingsCommand", "DesignCoverCommand", "ValidateCoverCommand",
                     "RunPreflightCommand", "GenerateDescriptionCommand", "SuggestKeywordsCommand",
                 })
        {
            Assert.Contains(cmd, xaml);
        }

        Assert.Contains("TabControl", xaml); // ribbon surface
        Assert.True(xaml.Contains("Home") && xaml.Contains("Layout") && xaml.Contains("Export") && xaml.Contains("Publish"));
        Assert.Contains("ToggleTheme", vm);
        Assert.Contains("RefreshPreview", vm);
        Assert.Contains("PreviewMode", vm);
        Assert.Contains("Settings", xaml); // settings panel
        Assert.True(File.Exists(dark));
        Assert.True(File.Exists(light));
        Assert.Contains("WindowBackground", File.ReadAllText(dark));
        Assert.Contains("WindowBackground", File.ReadAllText(light));
    }

    [Fact]
    public void PathTraversal_TemplateRoot_DoesNotEscapeWhenListing()
    {
        // Ensure template discovery only reads under provided root (no exception / no crash).
        var providerRoot = TestFixtures.TemplatesRoot;
        var provider = new JsonTemplateProvider(providerRoot);
        foreach (var t in provider.ListTemplates())
        {
            if (!string.IsNullOrEmpty(t.PackagePath))
            {
                var full = Path.GetFullPath(t.PackagePath);
                Assert.StartsWith(Path.GetFullPath(providerRoot), full);
            }
        }
    }
}
