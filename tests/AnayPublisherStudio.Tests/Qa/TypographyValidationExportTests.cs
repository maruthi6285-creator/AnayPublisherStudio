using AnayPublisherStudio.Domain.Blocks;
using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Domain.Layout;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;
using DomainTypography = AnayPublisherStudio.Domain.ValueObjects.Typography;
using AnayPublisherStudio.Infrastructure.Export;
using AnayPublisherStudio.Infrastructure.Integrity;
using AnayPublisherStudio.Infrastructure.Layout;
using AnayPublisherStudio.Rendering;
using AnayPublisherStudio.Typography;
using AnayPublisherStudio.Typography.Hyphenation;
using AnayPublisherStudio.Validation;
using Xunit;

namespace AnayPublisherStudio.Tests.Qa;

public class TypographyValidationExportTests
{
    [Fact]
    public void Typography_OpeningParagraph_DropCapAndNoIndent()
    {
        var engine = new TypographyEngine();
        var t = TestFixtures.DefaultTemplate();
        var open = engine.ResolveBody(t, true);
        var body = engine.ResolveBody(t, false);
        Assert.Equal(0, open.FirstLineIndentInches);
        Assert.True(open.DropCapLines > 0);
        Assert.True(body.FirstLineIndentInches > 0);
        Assert.Equal(0, body.DropCapLines);
        Assert.True(open.Ligatures && open.Kerning);
        Assert.True(open.OpticalAlignment || open.HangingPunctuation);
        Assert.True(open.Hyphenation);
        Assert.InRange(open.MinWordSpacing, 0.5, 1.0);
        Assert.InRange(open.MaxWordSpacing, 1.0, 2.0);
    }

    [Fact]
    public void Typography_Headings_ScaleAndSmallCapsOnH1()
    {
        var engine = new TypographyEngine();
        var t = TestFixtures.DefaultTemplate();
        var h1 = engine.ResolveHeading(t, HeadingLevel.H1);
        var h2 = engine.ResolveHeading(t, HeadingLevel.H2);
        var h3 = engine.ResolveHeading(t, HeadingLevel.H3);
        Assert.True(h1.FontSizePoints > h2.FontSizePoints);
        Assert.True(h2.FontSizePoints > h3.FontSizePoints);
        Assert.True(h1.SmallCaps || h1.TrueSmallCaps);
        Assert.False(h1.Hyphenation);
    }

    [Fact]
    public void Typography_FootnoteAndRunningMatter_Defaults()
    {
        AnayPublisherStudio.Application.Abstractions.ITypographyEngine engine = new TypographyEngine();
        var t = TestFixtures.DefaultTemplate();
        var note = engine.ResolveFootnote(t);
        var run = engine.ResolveRunningMatter(t);
        Assert.True(note.FontSizePoints <= t.BodyFontSize);
        Assert.True(run.FontSizePoints > 0);
    }

    [Fact]
    public void ParagraphComposer_AndHyphenation_DoNotMutateInput()
    {
        var text = "hyphenation demonstration " + new string('b', 300);
        var original = text;
        var composer = new ParagraphComposer(new HyphenationService());
        var ty = new DomainTypography
        {
            FontSizePoints = 11,
            LineHeight = 1.35,
            Hyphenation = true,
            Language = "en-US",
        };
        var lines = composer.ComposeLineBreaks(text, ty, 4.9);
        Assert.True(lines.Count >= 2);
        Assert.Equal(original, text);
        Assert.True(new HyphenationService().Supports("en-US"));
        Assert.NotEmpty(new HyphenationService().GetBreakPositions("hyphenation"));
    }

    [Fact]
    public void Validation_FlagsLowDpiImages_AndMarginIssues()
    {
        var book = TestFixtures.MakeBook(1, 1);
        book.Chapters[0].Blocks.Add(new ImageBlock
        {
            Order = 9,
            Data = new byte[] { 1, 2, 3 },
            Dpi = 72,
            ContentType = "image/png",
            PixelWidth = 100,
            PixelHeight = 100,
        });
        var template = TestFixtures.DefaultTemplate();
        template.InsideMargin = 0.2; // below minimum
        template.MinImageDpi = 300;
        var report = new KdpValidationEngine().Validate(book, template, pageCount: 120);
        Assert.Contains(report.Findings, f => f.Check == "ImageDpi" && f.Severity == ValidationSeverity.Error);
        Assert.Contains(report.Findings, f => f.Check.StartsWith("Margins"));
        Assert.False(report.IsPublishable);
    }

    [Fact]
    public void Validation_IngramProfile_PageCountRule()
    {
        var book = TestFixtures.MakeBook(1, 1);
        var template = TestFixtures.DefaultTemplate();
        template.Platform = "IngramSpark";
        var report = new KdpValidationEngine().Validate(book, template, pageCount: 10);
        Assert.Contains(report.Findings, f => f.Check.Contains("Ingram", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validation_Hyperlinks_AndMetadataIsbn()
    {
        var book = TestFixtures.MakeBook(1, 1);
        var para = book.Chapters[0].Blocks.OfType<ParagraphBlock>().First();
        para.Runs.Add(new TextRun { Text = "link", Hyperlink = "not-a-url" });
        book.Metadata.Isbn = "123";
        book.Metadata.Keywords = Enumerable.Range(1, 9).Select(i => $"k{i}").ToList();
        var report = new KdpValidationEngine().Validate(book, TestFixtures.DefaultTemplate(), 50);
        Assert.Contains(report.Findings, f => f.Check == "Hyperlinks");
        Assert.Contains(report.Findings, f => f.Check == "Metadata.Isbn" || f.Check == "Metadata.Keywords");
    }

    [Fact]
    public async Task Export_AllMajorFormats_PreservesAuthorTextAndIntegrity()
    {
        var book = TestFixtures.MakeBook(2, 5);
        var expectedText = book.Chapters.SelectMany(c => c.Blocks).OfType<ParagraphBlock>().Select(p => p.PlainText).ToList();
        var project = new PublishingProject { Metadata = book.Metadata };
        var template = TestFixtures.DefaultTemplate();
        var layout = new LayoutDocument
        {
            TrimWidth = 6,
            TrimHeight = 9,
            Pages = { new ComposedPage { PageNumber = 1, Side = PageSide.Recto } },
        };
        var ty = new TypographyEngine();
        var professional = new ProfessionalLayoutEngine(ty, new ParagraphComposer());
        var exporter = new ArtifactExporter(
            new QuestPdfLayoutEngine(ty, professional),
            new QuestPdfCoverEngine(),
            new KdpValidationEngine(),
            new ContentIntegrityGuard());

        var dir = Path.Combine(Path.GetTempPath(), "aps-qa-export-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var formats = new[]
            {
                ExportFormat.PrintPdf,
                ExportFormat.DigitalPdf,
                ExportFormat.PdfX,
                ExportFormat.PdfA,
                ExportFormat.CoverPdf,
                ExportFormat.Epub,
                ExportFormat.Kindle,
                ExportFormat.Docx,
                ExportFormat.ValidationReport,
                ExportFormat.ProjectArchive,
            };
            var result = await exporter.ExportAsync(project, book, template, layout, formats, dir);
            Assert.True(result.ContainsKey(ExportFormat.PrintPdf));
            Assert.True(result.ContainsKey(ExportFormat.Epub));
            Assert.True(result.ContainsKey(ExportFormat.CoverPdf));
            Assert.True(result.ContainsKey(ExportFormat.ProjectArchive));
            Assert.True(File.Exists(result[ExportFormat.PrintPdf]));
            Assert.True(File.Exists(result[ExportFormat.Epub]));
            Assert.True(File.Exists(result[ExportFormat.ProjectArchive]));

            var afterText = book.Chapters.SelectMany(c => c.Blocks).OfType<ParagraphBlock>().Select(p => p.PlainText).ToList();
            Assert.Equal(expectedText, afterText);
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [Fact]
    public void SpineCalculator_MatchesKnownKdpValues()
    {
        var calc = new KdpSpineCalculator();
        Assert.InRange(calc.CalculateInches(105, PaperType.White, ColorMode.BlackWhite), 0.235, 0.237);
        Assert.True(calc.CalculateInches(300, PaperType.Cream, ColorMode.BlackWhite) >
                    calc.CalculateInches(300, PaperType.White, ColorMode.BlackWhite));
        Assert.Equal(0, calc.CalculateInches(0, PaperType.White, ColorMode.BlackWhite));
    }
}
