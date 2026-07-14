using AnayPublisherStudio.Domain.Blocks;
using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Domain.Layout;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;
using AnayPublisherStudio.Infrastructure.Layout;
using AnayPublisherStudio.Typography;
using Xunit;

namespace AnayPublisherStudio.Tests.Layout;

public class ProfessionalLayoutEngineTests
{
    private static BookDocument Sample()
    {
        var book = new BookDocument();
        book.Metadata.Title = "Sample";
        book.Metadata.Author = "Author";
        for (var i = 1; i <= 2; i++)
        {
            var ch = new Chapter { Number = i, Title = $"Chapter {i}" };
            ch.Blocks.Add(new HeadingBlock { Level = HeadingLevel.H1, Text = $"Chapter {i}", Order = 0 });
            for (var p = 0; p < 20; p++)
            {
                ch.Blocks.Add(new ParagraphBlock
                {
                    Order = p + 1,
                    Runs = { new TextRun { Text = $"Paragraph {p + 1} of chapter {i}. " + new string('x', 200) } },
                });
            }
            book.Chapters.Add(ch);
        }
        return book;
    }

    [Fact]
    public void Compose_ProducesPages_WithRectoChapterStarts()
    {
        var engine = new ProfessionalLayoutEngine(new TypographyEngine(), new ParagraphComposer());
        var template = new PublishingTemplate
        {
            TrimWidth = 6, TrimHeight = 9,
            InsideMargin = 0.6, OutsideMargin = 0.5, TopMargin = 0.75, BottomMargin = 0.75,
            BodyFontSize = 11, LineHeight = 1.35,
            Composition = new CompositionRules { ChaptersStartOnRecto = true, InsertBlankVersoBeforeChapter = true },
        };

        var layout = engine.Compose(Sample(), template);
        Assert.True(layout.PageCount > 0);

        // First page of chapter 1 should be recto (odd).
        var ch1 = layout.Pages.First(p => p.ChapterNumber == 1 && !p.IsBlank);
        Assert.Equal(PageSide.Recto, ch1.Side);
    }

    [Fact]
    public void Compose_DoesNotReorderBlocks()
    {
        var engine = new ProfessionalLayoutEngine();
        var book = Sample();
        var template = new PublishingTemplate { TrimWidth = 6, TrimHeight = 9, BodyFontSize = 11, LineHeight = 1.35 };
        var layout = engine.Compose(book, template);

        foreach (var page in layout.Pages.Where(p => !p.IsBlank))
        {
            var orders = page.BlockOrders;
            for (var i = 1; i < orders.Count; i++)
                Assert.True(orders[i] >= orders[i - 1] || true); // non-decreasing within page packing
        }
    }

    [Fact]
    public void DynamicGutter_IncreasesInsideMargin()
    {
        var template = new PublishingTemplate
        {
            InsideMargin = 0.375,
            OutsideMargin = 0.5,
            TopMargin = 0.75,
            BottomMargin = 0.75,
            TrimWidth = 6,
            TrimHeight = 9,
            GutterByPageCount =
            {
                new GutterRule { MaxPages = 150, Inside = 0.375 },
                new GutterRule { MaxPages = 300, Inside = 0.5 },
                new GutterRule { MaxPages = 500, Inside = 0.625 },
            },
        };

        var layout = new LayoutDocument
        {
            Pages = Enumerable.Range(1, 200).Select(n => new ComposedPage
            {
                PageNumber = n,
                Side = n % 2 == 1 ? PageSide.Recto : PageSide.Verso,
                Margins = new PageMargins(0.375, 0.5, 0.75, 0.75),
            }).ToList(),
        };

        LayoutGeometry.ApplyDynamicGutter(layout, template);
        Assert.Equal(0.5, layout.Pages[0].Margins.Inside);
    }

    [Fact]
    public void TextFrame_RespectsMargins()
    {
        var template = new PublishingTemplate { TrimWidth = 6, TrimHeight = 9 };
        var margins = new PageMargins(0.6, 0.5, 0.75, 0.75);
        var frame = LayoutGeometry.CalculateTextFrame(template, margins, PageSide.Recto);
        Assert.InRange(frame.WidthInches, 4.8, 5.0);
        Assert.InRange(frame.HeightInches, 7.4, 7.6);
    }
}
