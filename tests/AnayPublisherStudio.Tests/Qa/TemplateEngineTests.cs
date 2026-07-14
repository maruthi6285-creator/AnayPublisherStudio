using AnayPublisherStudio.Domain.ValueObjects;
using AnayPublisherStudio.Infrastructure.Templates;
using Xunit;

namespace AnayPublisherStudio.Tests.Qa;

public class TemplateEngineTests
{
    private JsonTemplateProvider CreateProvider()
    {
        Assert.True(Directory.Exists(TestFixtures.TemplatesRoot),
            $"Templates root missing: {TestFixtures.TemplatesRoot}");
        return new JsonTemplateProvider(TestFixtures.TemplatesRoot);
    }

    [Fact]
    public void ListTemplates_DiscoversAllElevenPublisherPackages()
    {
        var provider = CreateProvider();
        var list = provider.ListTemplates();
        Assert.True(list.Count >= 11, $"Expected >=11 templates, got {list.Count}");
        Assert.Contains(list, t => t.Id == "amazon-paperback-6x9");
        Assert.Contains(list, t => t.Id.Contains("ingramspark", StringComparison.OrdinalIgnoreCase)
                                   || t.Platform.Contains("Ingram", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("amazon-paperback-6x9", 6, 9)]
    [InlineData("ingramspark-paperback-6x9", 6, 9)]
    [InlineData("lulu-paperback-6x9", 6, 9)]
    [InlineData("bn-paperback-6x9", 6, 9)]
    [InlineData("notionpress-paperback-6x9", 6, 9)]
    [InlineData("blurb-paperback-8x10", 8, 10)]
    [InlineData("university-thesis-a4", 8.27, 11.69)]
    [InlineData("magazine-letter", 8.5, 11)]
    [InlineData("journal-a5", 5.83, 8.27)]
    [InlineData("children-book-8x8", 8, 8)]
    [InlineData("comic-6.625x10.25", 6.625, 10.25)]
    public void GetTemplate_LoadsTrimAndCoreGeometry(string id, double w, double h)
    {
        var tpl = CreateProvider().GetTemplate(id);
        Assert.NotNull(tpl);
        Assert.Equal(w, tpl!.TrimWidth, 3);
        Assert.Equal(h, tpl.TrimHeight, 3);
        Assert.True(tpl.InsideMargin > 0);
        Assert.True(tpl.OutsideMargin > 0);
        Assert.True(tpl.TopMargin > 0);
        Assert.True(tpl.BottomMargin > 0);
        Assert.True(tpl.OverallWidth >= tpl.TrimWidth * 2);
        Assert.True(tpl.BarcodeWidth > 0 && tpl.BarcodeHeight > 0);
        Assert.False(string.IsNullOrWhiteSpace(tpl.BodyFont));
        Assert.False(string.IsNullOrWhiteSpace(tpl.HeadingFont));
    }

    [Fact]
    public void AmazonTemplate_HasMirrorMarginsAndGutterTable()
    {
        var tpl = CreateProvider().GetTemplate("amazon-paperback-6x9");
        Assert.NotNull(tpl);
        Assert.True(tpl!.MirrorMargins);
        Assert.NotEmpty(tpl.GutterByPageCount);
        Assert.Contains(tpl.GutterByPageCount, g => g.MaxPages == 150 && g.Inside >= 0.375);
        Assert.True(tpl.Composition.ChaptersStartOnRecto);
        Assert.True(tpl.Composition.WidowLines >= 1);
        Assert.True(tpl.Composition.OrphanLines >= 1);
    }

    [Fact]
    public void TemplatePackageService_ListsInstalledAndLoadsPackageDescriptor()
    {
        var provider = CreateProvider();
        var svc = new TemplatePackageService(TestFixtures.TemplatesRoot, provider);
        var installed = svc.ListInstalledPackages();
        Assert.NotEmpty(installed);
        Assert.Contains(installed, id => id.Contains("amazon", StringComparison.OrdinalIgnoreCase)
                                         || id == "amazon-paperback-6x9");

        var pkg = svc.GetPackage("amazon-paperback-6x9");
        Assert.NotNull(pkg);
        Assert.Equal("amazon-paperback-6x9", pkg!.Id);
        Assert.True(Directory.Exists(pkg.RootPath));
        Assert.NotNull(pkg.Template);
        // SDK files may be present as raw JSON blobs
        Assert.True(pkg.PublisherJson is not null || pkg.LayoutJson is not null || pkg.StylesJson is not null
                    || File.Exists(Path.Combine(pkg.RootPath, "template.json")));
    }

    [Fact]
    public void UnknownTemplate_ReturnsNull()
    {
        Assert.Null(CreateProvider().GetTemplate("does-not-exist-xyz"));
    }

    [Fact]
    public void AllTemplates_HavePositiveLiveArea()
    {
        foreach (var tpl in CreateProvider().ListTemplates())
        {
            var liveW = tpl.TrimWidth - tpl.InsideMargin - tpl.OutsideMargin;
            var liveH = tpl.TrimHeight - tpl.TopMargin - tpl.BottomMargin;
            Assert.True(liveW > 1.0, $"{tpl.Id} live width {liveW}");
            Assert.True(liveH > 1.0, $"{tpl.Id} live height {liveH}");
        }
    }
}
