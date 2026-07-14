using System.Diagnostics;
using AnayPublisherStudio.Domain.Blocks;
using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;
using AnayPublisherStudio.Infrastructure.Layout;
using AnayPublisherStudio.Typography;
using Xunit;

namespace AnayPublisherStudio.Tests.Performance;

public class LayoutPerformanceTests
{
    private static BookDocument Generate(int chapters, int paragraphsPerChapter)
    {
        var book = new BookDocument();
        book.Metadata.Title = "Perf";
        for (var c = 1; c <= chapters; c++)
        {
            var ch = new Chapter { Number = c, Title = $"Chapter {c}" };
            ch.Blocks.Add(new HeadingBlock { Level = HeadingLevel.H1, Text = $"Chapter {c}", Order = 0 });
            for (var p = 0; p < paragraphsPerChapter; p++)
            {
                ch.Blocks.Add(new ParagraphBlock
                {
                    Order = p + 1,
                    Runs = { new TextRun { Text = $"P{p} " + new string('a', 180) } },
                });
            }
            book.Chapters.Add(ch);
        }
        return book;
    }

    [Theory]
    [InlineData(5, 40)]   // ~50 pages class
    [InlineData(15, 40)]  // ~100+ pages class
    [InlineData(40, 50)]  // ~300 pages class
    public void Compose_Completes_WithinBudget(int chapters, int paragraphs)
    {
        var engine = new ProfessionalLayoutEngine(new TypographyEngine(), new ParagraphComposer());
        var template = new PublishingTemplate
        {
            TrimWidth = 6, TrimHeight = 9,
            InsideMargin = 0.6, OutsideMargin = 0.5, TopMargin = 0.75, BottomMargin = 0.75,
            BodyFontSize = 11, LineHeight = 1.35,
        };

        var book = Generate(chapters, paragraphs);
        var sw = Stopwatch.StartNew();
        var layout = engine.Compose(book, template);
        sw.Stop();

        Assert.True(layout.PageCount > 0);
        // Composition must stay responsive even for large manuscripts.
        Assert.True(sw.ElapsedMilliseconds < 15_000, $"Compose took {sw.ElapsedMilliseconds}ms for {layout.PageCount} pages");
    }
}
