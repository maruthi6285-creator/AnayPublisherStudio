using System.Diagnostics;
using AnayPublisherStudio.Domain.ValueObjects;
using AnayPublisherStudio.Infrastructure.Integrity;
using AnayPublisherStudio.Infrastructure.Layout;
using AnayPublisherStudio.Rendering;
using AnayPublisherStudio.Typography;
using AnayPublisherStudio.Typography.Hyphenation;
using AnayPublisherStudio.Validation;
using Xunit;
using Xunit.Abstractions;

namespace AnayPublisherStudio.Tests.Qa;

public class PerformanceExtendedTests
{
    private readonly ITestOutputHelper _output;
    public PerformanceExtendedTests(ITestOutputHelper output) => _output = output;

    public static IEnumerable<object[]> PageTargets()
    {
        yield return new object[] { 100 };
        yield return new object[] { 300 };
        yield return new object[] { 500 };
        yield return new object[] { 1000 };
    }

    [Theory]
    [MemberData(nameof(PageTargets))]
    public void Compose_ScalesToTargetPageClass(int targetPages)
    {
        var book = TestFixtures.MakeLargeBook(targetPages);
        var template = TestFixtures.DefaultTemplate();
        var engine = new ProfessionalLayoutEngine(new TypographyEngine(), new ParagraphComposer(new HyphenationService()));

        var sw = Stopwatch.StartNew();
        var before = GC.GetTotalMemory(forceFullCollection: true);
        var layout = engine.Compose(book, template);
        var after = GC.GetTotalMemory(forceFullCollection: false);
        sw.Stop();

        _output.WriteLine($"Compose target~{targetPages}: pages={layout.PageCount}, ms={sw.ElapsedMilliseconds}, deltaMB={(after - before) / 1024.0 / 1024.0:0.00}");
        Assert.True(layout.PageCount > 0);
        // Soft budget: composition should remain interactive-class even at 1000 pages.
        var budgetMs = targetPages switch
        {
            <= 100 => 5_000,
            <= 300 => 10_000,
            <= 500 => 20_000,
            _ => 45_000,
        };
        Assert.True(sw.ElapsedMilliseconds < budgetMs,
            $"Compose for ~{targetPages} pages took {sw.ElapsedMilliseconds}ms (budget {budgetMs}ms), produced {layout.PageCount} pages");
    }

    [Theory]
    [InlineData(100)]
    [InlineData(300)]
    public void RenderPdf_CompletesWithinBudget_AndKeepsIntegrity(int targetPages)
    {
        var book = TestFixtures.MakeLargeBook(targetPages);
        var guard = new ContentIntegrityGuard();
        var expected = guard.ComputeFingerprint(book);
        var template = TestFixtures.DefaultTemplate();
        var ty = new TypographyEngine();
        var professional = new ProfessionalLayoutEngine(ty, new ParagraphComposer(new HyphenationService()));
        var renderer = new QuestPdfLayoutEngine(ty, professional);

        var sw = Stopwatch.StartNew();
        using var ms = new MemoryStream();
        var pages = renderer.Render(book, template, ms);
        sw.Stop();

        _output.WriteLine($"Render target~{targetPages}: pages={pages}, ms={sw.ElapsedMilliseconds}, pdfKB={ms.Length / 1024.0:0.0}");
        Assert.True(pages >= 1);
        Assert.True(ms.Length > 1000);
        Assert.True(guard.Verify(expected, book).IsIntact);
        var budget = targetPages <= 100 ? 30_000 : 90_000;
        Assert.True(sw.ElapsedMilliseconds < budget, $"Render took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void Validation_OnLargeModel_IsFast()
    {
        var book = TestFixtures.MakeLargeBook(500);
        var template = TestFixtures.DefaultTemplate();
        var sw = Stopwatch.StartNew();
        var report = new KdpValidationEngine().Validate(book, template, pageCount: 500);
        sw.Stop();
        _output.WriteLine($"Validate 500p model: ms={sw.ElapsedMilliseconds}, findings={report.Findings.Count}");
        Assert.True(sw.ElapsedMilliseconds < 5_000);
        Assert.NotEmpty(report.Findings);
    }
}
