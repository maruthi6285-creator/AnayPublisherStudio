using AnayPublisherStudio.Domain.Blocks;
using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;
using AnayPublisherStudio.Infrastructure.Integrity;
using AnayPublisherStudio.Typography;
using Xunit;

namespace AnayPublisherStudio.Tests;

/// <summary>
/// Guards the absolute rule: author content is never modified by presentation.
/// </summary>
public class ContentIntegrityGuardTests
{
    private static BookDocument SampleBook()
    {
        var book = new BookDocument();
        book.Metadata.Title = "T";
        book.Chapters.Add(new Chapter
        {
            Number = 1,
            Title = "One",
            Blocks =
            {
                new HeadingBlock { Level = HeadingLevel.H1, Text = "One", Order = 0 },
                new ParagraphBlock
                {
                    Order = 1,
                    Runs = { new TextRun { Text = "Hello world.", Bold = false } },
                },
                new TableBlock
                {
                    Order = 2,
                    Rows = { new List<string> { "A", "B" }, new List<string> { "1", "2" } },
                },
            },
        });
        book.Footnotes.Add(new Footnote { Id = "1", Text = "A note." });
        return book;
    }

    [Fact]
    public void Fingerprint_IsStable_ForUnchangedContent()
    {
        var guard = new ContentIntegrityGuard();
        var book = SampleBook();
        var a = guard.ComputeFingerprint(book);
        var b = guard.ComputeFingerprint(book);
        Assert.Equal(a, b);
        Assert.False(string.IsNullOrWhiteSpace(a));
    }

    [Fact]
    public void Fingerprint_IgnoresGeneratedToc()
    {
        var guard = new ContentIntegrityGuard();
        var book = SampleBook();
        var before = guard.ComputeFingerprint(book);
        book.TableOfContents.Add(new TocEntry { Title = "One", Level = 1, PageNumber = 3 });
        var after = guard.ComputeFingerprint(book);
        Assert.Equal(before, after);
    }

    [Fact]
    public void Fingerprint_Changes_WhenAuthorTextIsAltered()
    {
        var guard = new ContentIntegrityGuard();
        var book = SampleBook();
        var before = guard.ComputeFingerprint(book);
        var para = (ParagraphBlock)book.Chapters[0].Blocks[1];
        para.Runs[0].Text = "Hello world!"; // author content change
        var after = guard.ComputeFingerprint(book);
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void Fingerprint_Changes_WhenChapterOrderChanges()
    {
        var guard = new ContentIntegrityGuard();
        var book = SampleBook();
        book.Chapters.Add(new Chapter
        {
            Number = 2,
            Title = "Two",
            Blocks = { new ParagraphBlock { Runs = { new TextRun { Text = "Second." } } } },
        });
        var before = guard.ComputeFingerprint(book);
        (book.Chapters[0], book.Chapters[1]) = (book.Chapters[1], book.Chapters[0]);
        var after = guard.ComputeFingerprint(book);
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void Verify_ReportsIntact_WhenUnchanged()
    {
        var guard = new ContentIntegrityGuard();
        var book = SampleBook();
        var fp = guard.ComputeFingerprint(book);
        var result = guard.Verify(fp, book);
        Assert.True(result.IsIntact);
    }
}

public class TypographyEngineTests
{
    private readonly TypographyEngine _engine = new();
    private readonly PublishingTemplate _template = new()
    {
        BodyFont = "Georgia",
        HeadingFont = "Georgia",
        BodyFontSize = 11,
        LineHeight = 1.35,
        FirstLineIndent = 0.25,
    };

    [Fact]
    public void OpeningParagraph_HasNoFirstLineIndent()
    {
        var open = _engine.ResolveBody(_template, isChapterOpening: true);
        var body = _engine.ResolveBody(_template, isChapterOpening: false);
        Assert.Equal(0, open.FirstLineIndentInches);
        Assert.Equal(0.25, body.FirstLineIndentInches);
        Assert.True(open.DropCapLines > 0);
        Assert.Equal(0, body.DropCapLines);
    }

    [Fact]
    public void HeadingSizes_DecreaseWithLevel()
    {
        var h1 = _engine.ResolveHeading(_template, HeadingLevel.H1);
        var h2 = _engine.ResolveHeading(_template, HeadingLevel.H2);
        var h3 = _engine.ResolveHeading(_template, HeadingLevel.H3);
        Assert.True(h1.FontSizePoints > h2.FontSizePoints);
        Assert.True(h2.FontSizePoints > h3.FontSizePoints);
        Assert.True(h1.SmallCaps);
    }

    [Fact]
    public void Caption_IsSmallerThanBody()
    {
        var body = _engine.ResolveBody(_template, false);
        var cap = _engine.ResolveCaption(_template);
        Assert.True(cap.FontSizePoints < body.FontSizePoints);
    }
}
