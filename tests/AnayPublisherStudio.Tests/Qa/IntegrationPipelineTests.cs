using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Composition;
using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Infrastructure.Integrity;
using AnayPublisherStudio.Infrastructure.Layout;
using AnayPublisherStudio.Infrastructure.Parsing;
using AnayPublisherStudio.Infrastructure.Templates;
using AnayPublisherStudio.Rendering;
using AnayPublisherStudio.Typography;
using AnayPublisherStudio.Typography.Hyphenation;
using AnayPublisherStudio.Validation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AnayPublisherStudio.Tests.Qa;

public class IntegrationPipelineTests
{
    private static ServiceProvider BuildServices(string templatesRoot, string dbPath)
        => new ServiceCollection()
            .AddAnayPublisherStudio(templatesRoot, dbPath)
            .BuildServiceProvider();

    [Fact]
    public async Task EndToEnd_ParseLayoutRenderValidateExport_PreservesIntegrity()
    {
        Assert.True(Directory.Exists(TestFixtures.TemplatesRoot), TestFixtures.TemplatesRoot);
        var outDir = Path.Combine(Path.GetTempPath(), "aps-qa-e2e-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outDir);
        var manuscript = Path.Combine(outDir, "manuscript.docx");
        await using (var src = TestFixtures.BuildDocx(chapters: 4, paragraphsPerChapter: 20, includeTable: true))
        await using (var dst = File.Create(manuscript))
            await src.CopyToAsync(dst);

        try
        {
            await using var sp = BuildServices(TestFixtures.TemplatesRoot, Path.Combine(outDir, "projects.db"));
            var exporter = sp.GetRequiredService<IExportService>();
            var project = new PublishingProject
            {
                Name = "QA E2E",
                ManuscriptPath = manuscript,
                TemplateId = "amazon-paperback-6x9",
                Metadata = { Title = "Sample Book", Author = "Test Author" },
            };

            var result = await exporter.PublishAsync(project, outDir);

            Assert.True(File.Exists(result.PrintPdfPath));
            Assert.True(File.Exists(result.CoverPdfPath));
            Assert.True(File.Exists(result.ValidationReportPath));
            Assert.True(result.PageCount > 0);
            Assert.NotNull(result.ContentIntegrity);
            Assert.True(result.ContentIntegrity!.IsIntact, "Author content integrity must pass after full pipeline.");

            Assert.True(result.Artifacts.Count >= 1);
            var printBytes = await File.ReadAllBytesAsync(result.PrintPdfPath!);
            Assert.True(printBytes.Length > 1000);
            Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(printBytes, 0, 4));
            Assert.True(PdfPageCounter.Count(printBytes) >= 1);

            var coverBytes = await File.ReadAllBytesAsync(result.CoverPdfPath!);
            Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(coverBytes, 0, 4));
        }
        finally
        {
            try { Directory.Delete(outDir, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void ManualPipeline_ParseComposeRenderValidate_MatchesIntegrity()
    {
        using var ms = TestFixtures.BuildDocx(chapters: 3, paragraphsPerChapter: 10);
        var book = new DocxDocumentParser().Parse(ms);
        var guard = new ContentIntegrityGuard();
        var expected = guard.ComputeFingerprint(book);

        var template = new JsonTemplateProvider(TestFixtures.TemplatesRoot).GetTemplate("amazon-paperback-6x9")
                       ?? TestFixtures.DefaultTemplate();
        var typography = new TypographyEngine();
        var professional = new ProfessionalLayoutEngine(typography, new ParagraphComposer(new HyphenationService()));
        var layout = professional.Compose(book, template);
        Assert.True(layout.PageCount > 0);

        using var pdf = new MemoryStream();
        var pages = new QuestPdfLayoutEngine(typography, professional).Render(book, template, pdf);
        Assert.True(pages >= 1);
        Assert.True(pdf.Length > 500);

        var report = new KdpValidationEngine().Validate(book, template, pages);
        Assert.NotEmpty(report.Findings);

        Assert.True(guard.Verify(expected, book).IsIntact);
        // TOC is presentation-only and should not break integrity
        Assert.NotEmpty(book.TableOfContents);
    }

    [Fact]
    public async Task Publish_MissingManuscript_Throws()
    {
        var outDir = Path.Combine(Path.GetTempPath(), "aps-qa-miss-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outDir);
        try
        {
            await using var sp = BuildServices(TestFixtures.TemplatesRoot, Path.Combine(outDir, "db.sqlite"));
            var exporter = sp.GetRequiredService<IExportService>();
            var project = new PublishingProject
            {
                ManuscriptPath = Path.Combine(outDir, "nope.docx"),
                TemplateId = "amazon-paperback-6x9",
            };
            await Assert.ThrowsAsync<FileNotFoundException>(() => exporter.PublishAsync(project, outDir));
        }
        finally
        {
            try { Directory.Delete(outDir, true); } catch { }
        }
    }

    [Fact]
    public async Task Publish_UnknownTemplate_Throws()
    {
        var outDir = Path.Combine(Path.GetTempPath(), "aps-qa-tpl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outDir);
        var manuscript = Path.Combine(outDir, "m.docx");
        await using (var src = TestFixtures.BuildDocx())
        await using (var dst = File.Create(manuscript))
            await src.CopyToAsync(dst);
        try
        {
            await using var sp = BuildServices(TestFixtures.TemplatesRoot, Path.Combine(outDir, "db.sqlite"));
            var exporter = sp.GetRequiredService<IExportService>();
            var project = new PublishingProject
            {
                ManuscriptPath = manuscript,
                TemplateId = "not-a-real-template",
            };
            await Assert.ThrowsAsync<InvalidOperationException>(() => exporter.PublishAsync(project, outDir));
        }
        finally
        {
            try { Directory.Delete(outDir, true); } catch { }
        }
    }

    [Fact]
    public void DiComposition_ResolvesAllCoreEngines()
    {
        var outDir = Path.Combine(Path.GetTempPath(), "aps-qa-di-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outDir);
        try
        {
            using var sp = BuildServices(TestFixtures.TemplatesRoot, Path.Combine(outDir, "db.sqlite"));
            Assert.NotNull(sp.GetRequiredService<IDocumentParser>());
            Assert.NotNull(sp.GetRequiredService<ITemplateProvider>());
            Assert.NotNull(sp.GetRequiredService<ISpineCalculator>());
            Assert.NotNull(sp.GetRequiredService<ITypographyEngine>());
            Assert.NotNull(sp.GetRequiredService<IContentIntegrityGuard>());
            Assert.NotNull(sp.GetRequiredService<IProfessionalLayoutEngine>());
            Assert.NotNull(sp.GetRequiredService<ILayoutEngine>());
            Assert.NotNull(sp.GetRequiredService<ICoverEngine>());
            Assert.NotNull(sp.GetRequiredService<ICoverDesigner>());
            Assert.NotNull(sp.GetRequiredService<IValidationEngine>());
            Assert.NotNull(sp.GetRequiredService<IAiAssistant>());
            Assert.NotNull(sp.GetRequiredService<IProjectRepository>());
            Assert.NotNull(sp.GetRequiredService<ILivePreviewEngine>());
            Assert.NotNull(sp.GetRequiredService<ITemplatePackageService>());
            Assert.NotNull(sp.GetRequiredService<IPluginManager>());
            Assert.NotNull(sp.GetRequiredService<IArtifactExporter>());
            Assert.NotNull(sp.GetRequiredService<IExportService>());
            Assert.NotNull(sp.GetRequiredService<IHyphenationService>());
            Assert.NotNull(sp.GetRequiredService<IParagraphComposer>());
        }
        finally
        {
            try { Directory.Delete(outDir, true); } catch { }
        }
    }
}
