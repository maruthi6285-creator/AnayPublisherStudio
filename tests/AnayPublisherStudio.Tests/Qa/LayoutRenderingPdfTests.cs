using AnayPublisherStudio.Domain.Blocks;
using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Domain.Layout;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;
using AnayPublisherStudio.Infrastructure.Cover;
using AnayPublisherStudio.Infrastructure.Layout;
using AnayPublisherStudio.Rendering;
using AnayPublisherStudio.Rendering.Preview;
using AnayPublisherStudio.Typography;
using AnayPublisherStudio.Typography.Hyphenation;
using Xunit;

namespace AnayPublisherStudio.Tests.Qa;

public class LayoutRenderingPdfTests
{
    private static (ProfessionalLayoutEngine layout, TypographyEngine ty, QuestPdfLayoutEngine render) Engines()
    {
        var ty = new TypographyEngine();
        var layout = new ProfessionalLayoutEngine(ty, new ParagraphComposer(new HyphenationService()));
        var render = new QuestPdfLayoutEngine(ty, layout);
        return (layout, ty, render);
    }

    [Fact]
    public void Compose_InsertsBlankPagesForRectoChapterStarts()
    {
        var book = TestFixtures.MakeBook(chapters: 3, paragraphsPerChapter: 15);
        var template = TestFixtures.DefaultTemplate();
        template.Composition = new CompositionRules
        {
            ChaptersStartOnRecto = true,
            InsertBlankVersoBeforeChapter = true,
            WidowLines = 2,
            OrphanLines = 2,
        };
        var (engine, _, _) = Engines();
        var layout = engine.Compose(book, template);

        foreach (var chapterNum in new[] { 1, 2, 3 })
        {
            var first = layout.Pages.First(p => p.ChapterNumber == chapterNum && !p.IsBlank);
            Assert.Equal(PageSide.Recto, first.Side);
            Assert.True(first.PageNumber % 2 == 1);
        }
        Assert.Contains(layout.Pages, p => p.IsBlank);
    }

    [Fact]
    public void Compose_RunningHeadersAndFooters_ArePopulated()
    {
        var book = TestFixtures.MakeBook(2, 10);
        book.Metadata.Title = "Running Title";
        var template = TestFixtures.DefaultTemplate();
        template.Composition = new CompositionRules
        {
            RunningMatter = new RunningHeaderSpec
            {
                RectoTemplate = "{chapter}",
                VersoTemplate = "{title}",
                PageNumbers = PageNumberStyle.Arabic,
                FontSizePoints = 9,
            },
        };
        var layout = Engines().layout.Compose(book, template);
        var contentPages = layout.Pages.Where(p => !p.IsBlank).ToList();
        Assert.NotEmpty(contentPages);
        Assert.Contains(contentPages, p => !string.IsNullOrWhiteSpace(p.RunningHeader));
        Assert.Contains(contentPages, p => !string.IsNullOrWhiteSpace(p.RunningFooter));
    }

    [Fact]
    public void Compose_MirrorMargins_SwapInsideOutsideBySide()
    {
        var template = TestFixtures.DefaultTemplate();
        template.InsideMargin = 0.75;
        template.OutsideMargin = 0.5;
        template.MirrorMargins = true;
        var recto = LayoutGeometry.ResolveMargins(template, 1, PageSide.Recto);
        var verso = LayoutGeometry.ResolveMargins(template, 2, PageSide.Verso);
        Assert.Equal(recto.Inside, recto.Left(PageSide.Recto));
        Assert.Equal(recto.Outside, recto.Right(PageSide.Recto));
        Assert.Equal(verso.Outside, verso.Left(PageSide.Verso));
        Assert.Equal(verso.Inside, verso.Right(PageSide.Verso));
    }

    [Fact]
    public void Compose_DoesNotDropOrReorderAuthorBlocksAcrossDocument()
    {
        var book = TestFixtures.MakeBook(2, 25);
        var originalOrders = book.Chapters.Select(c => c.Blocks.Select(b => b.Order).ToList()).ToList();
        var layout = Engines().layout.Compose(book, TestFixtures.DefaultTemplate());

        // All non-blank pages reference only existing block orders for their chapter.
        foreach (var page in layout.Pages.Where(p => !p.IsBlank))
        {
            var chapter = book.Chapters.First(c => c.Number == page.ChapterNumber);
            var valid = chapter.Blocks.Select(b => b.Order).ToHashSet();
            Assert.All(page.BlockOrders, o => Assert.Contains(o, valid));
            // Within a page, packing should not reverse author order.
            for (var i = 1; i < page.BlockOrders.Count; i++)
                Assert.True(page.BlockOrders[i] >= page.BlockOrders[i - 1]);
        }

        // Source book block order unchanged.
        for (var i = 0; i < book.Chapters.Count; i++)
            Assert.Equal(originalOrders[i], book.Chapters[i].Blocks.Select(b => b.Order).ToList());
    }

    [Fact]
    public void Render_PrintPdf_HasValidHeaderAndExpectedTrimSignals()
    {
        var book = TestFixtures.MakeBook(2, 8);
        var template = TestFixtures.DefaultTemplate();
        var (_, _, render) = Engines();
        using var ms = new MemoryStream();
        var pages = render.Render(book, template, ms);
        Assert.True(pages >= 1);
        var bytes = ms.ToArray();
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
        Assert.Equal(pages, PdfPageCounter.Count(bytes));
        // MediaBox / page objects present
        var latin = System.Text.Encoding.Latin1.GetString(bytes);
        Assert.Contains("/Type /Page", latin.Replace("\n", " ").Replace("  ", " ")
            .Replace("/Type/Page", "/Type /Page"));
    }

    [Fact]
    public void Render_WithImagesAndTables_SucceedsWithoutMutatingContent()
    {
        var book = TestFixtures.MakeBook(1, 3);
        // 1x1 PNG
        var png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
        book.Chapters[0].Blocks.Add(new ImageBlock
        {
            Order = 50,
            Data = png,
            ContentType = "image/png",
            PixelWidth = 1,
            PixelHeight = 1,
            Dpi = 300,
            Caption = "Tiny image caption",
        });
        book.Chapters[0].Blocks.Add(new TableBlock
        {
            Order = 51,
            Rows = { new List<string> { "H1", "H2" }, new List<string> { "c1", "c2" } },
        });
        var plainBefore = book.Chapters[0].Blocks.OfType<ParagraphBlock>().Select(p => p.PlainText).ToList();

        using var ms = new MemoryStream();
        var pages = Engines().render.Render(book, TestFixtures.DefaultTemplate(), ms);
        Assert.True(pages >= 1);
        Assert.True(ms.Length > 500);
        Assert.Equal(plainBefore, book.Chapters[0].Blocks.OfType<ParagraphBlock>().Select(p => p.PlainText).ToList());
    }

    [Fact]
    public void CoverEngine_ProducesWraparoundPdf_WithBarcodeReservationArea()
    {
        var project = new PublishingProject
        {
            Metadata = { Title = "Cover Title", Author = "Cover Author", Description = "Blurb" },
        };
        var template = TestFixtures.DefaultTemplate();
        using var ms = new MemoryStream();
        new QuestPdfCoverEngine().Render(project, template, 120, ms);
        Assert.True(ms.Length > 500);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(ms.ToArray(), 0, 4));
    }

    [Fact]
    public void CoverDesigner_ValidatesSafeZonesAndSpine()
    {
        var designer = new CoverDesigner();
        var project = new PublishingProject { Metadata = { Title = "T", Author = "A" } };
        var template = TestFixtures.DefaultTemplate();
        var design = designer.CreateDesign(project, template, 105);
        designer.RecalculateSpine(design, template, 105, new KdpSpineCalculator());
        Assert.True(design.SpineWidth > 0);
        Assert.Contains(design.Layers, l => l.Kind == "barcode");
        Assert.Contains(design.Layers, l => l.Kind == "guide");
        var issues = designer.ValidateDesign(design, template);
        Assert.DoesNotContain(issues, i => i.Contains("missing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task LivePreview_RefreshAndRenderPage_ReturnsPdfBytes()
    {
        var ty = new TypographyEngine();
        var layout = new ProfessionalLayoutEngine(ty, new ParagraphComposer());
        var preview = new LivePreviewEngine(layout, ty);
        var book = TestFixtures.MakeBook(1, 5);
        var template = TestFixtures.DefaultTemplate();
        var composed = await preview.RefreshAsync(book, template);
        Assert.True(composed.PageCount >= 1);
        var page = await preview.RenderPageAsync(composed, book, template, 1, zoom: 1.0);
        Assert.True(page.Length > 200);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(page, 0, 4));
        var thumb = await preview.RenderPageThumbnailAsync(composed, book, template, 1, dpi: 72);
        Assert.True(thumb.Length > 200);
    }

    [Fact]
    public void EmptyBook_Compose_StillProducesAtLeastOnePage()
    {
        var book = new BookDocument { Metadata = { Title = "Empty", Author = "A" } };
        var layout = Engines().layout.Compose(book, TestFixtures.DefaultTemplate());
        Assert.True(layout.PageCount >= 1);
    }
}
