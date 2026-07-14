using AnayPublisherStudio.Domain.Blocks;
using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;
using AnayPublisherStudio.Infrastructure.Integrity;
using AnayPublisherStudio.Infrastructure.Layout;
using AnayPublisherStudio.Rendering;
using AnayPublisherStudio.Typography;
using AnayPublisherStudio.Validation;
using Xunit;

namespace AnayPublisherStudio.Tests.Qa;

public class ContentIntegrityExtendedTests
{
    private readonly ContentIntegrityGuard _guard = new();

    [Fact]
    public void Fingerprint_StableAcrossIndependentInstances()
    {
        var book = TestFixtures.MakeBook();
        var a = new ContentIntegrityGuard().ComputeFingerprint(book);
        var b = new ContentIntegrityGuard().ComputeFingerprint(book);
        Assert.Equal(a, b);
        Assert.Equal(64, a.Length); // SHA-256 hex
    }

    [Fact]
    public void Fingerprint_Changes_WhenSingleCharacterAltered()
    {
        var book = TestFixtures.MakeBook(1, 1);
        var before = _guard.ComputeFingerprint(book);
        var para = book.Chapters[0].Blocks.OfType<ParagraphBlock>().First();
        para.Runs[0].Text = para.Runs[0].Text + ".";
        var after = _guard.ComputeFingerprint(book);
        Assert.NotEqual(before, after);
        Assert.False(_guard.Verify(before, book).IsIntact);
    }

    [Fact]
    public void Fingerprint_Changes_WhenTableCellAltered()
    {
        var book = TestFixtures.MakeBook(1, 1);
        book.Chapters[0].Blocks.Add(new TableBlock
        {
            Order = 99,
            Rows = { new List<string> { "A", "B" }, new List<string> { "1", "2" } },
        });
        var before = _guard.ComputeFingerprint(book);
        ((TableBlock)book.Chapters[0].Blocks[^1]).Rows[0][0] = "Z";
        Assert.NotEqual(before, _guard.ComputeFingerprint(book));
    }

    [Fact]
    public void Fingerprint_Changes_WhenImageBytesChange()
    {
        var book = TestFixtures.MakeBook(1, 1);
        book.Chapters[0].Blocks.Add(new ImageBlock
        {
            Order = 50,
            Data = new byte[] { 1, 2, 3, 4 },
            ContentType = "image/png",
            PixelWidth = 10,
            PixelHeight = 10,
            Caption = "cap",
        });
        var before = _guard.ComputeFingerprint(book);
        ((ImageBlock)book.Chapters[0].Blocks[^1]).Data = new byte[] { 1, 2, 3, 5 };
        Assert.NotEqual(before, _guard.ComputeFingerprint(book));
    }

    [Fact]
    public void Fingerprint_Changes_WhenEndnoteAltered()
    {
        var book = TestFixtures.MakeBook(1, 1);
        book.Endnotes.Add(new Endnote { Id = "e1", Text = "note" });
        var before = _guard.ComputeFingerprint(book);
        book.Endnotes[0].Text = "note!";
        Assert.NotEqual(before, _guard.ComputeFingerprint(book));
    }

    [Fact]
    public void Fingerprint_IgnoresPresentationTypographyFields()
    {
        // Guard only covers author content; creating typography objects must not affect book fingerprint.
        var book = TestFixtures.MakeBook();
        var before = _guard.ComputeFingerprint(book);
        _ = new TypographyEngine().ResolveBody(TestFixtures.DefaultTemplate(), true);
        Assert.Equal(before, _guard.ComputeFingerprint(book));
    }

    [Fact]
    public void LayoutAndRender_DoNotMutateAuthorFingerprint()
    {
        var book = TestFixtures.MakeBook(3, 12);
        var expected = _guard.ComputeFingerprint(book);
        var template = TestFixtures.DefaultTemplate();
        var typography = new TypographyEngine();
        var professional = new ProfessionalLayoutEngine(typography, new ParagraphComposer());
        var layout = professional.Compose(book, template);
        Assert.True(layout.PageCount > 0);

        using var ms = new MemoryStream();
        var pages = new QuestPdfLayoutEngine(typography, professional).Render(book, template, ms);
        Assert.True(pages > 0);
        Assert.True(ms.Length > 1000);

        var result = _guard.Verify(expected, book);
        Assert.True(result.IsIntact, $"Integrity broken: expected={result.ExpectedFingerprint} actual={result.ActualFingerprint}");
    }

    [Fact]
    public void CoverRender_DoesNotMutateAuthorFingerprint()
    {
        var book = TestFixtures.MakeBook(1, 2);
        var expected = _guard.ComputeFingerprint(book);
        var project = new PublishingProject
        {
            Metadata = book.Metadata,
            TemplateId = "amazon-paperback-6x9",
        };
        var template = TestFixtures.DefaultTemplate();
        using var ms = new MemoryStream();
        new QuestPdfCoverEngine().Render(project, template, pageCount: 120, ms);
        Assert.True(ms.Length > 500);
        Assert.True(_guard.Verify(expected, book).IsIntact);
    }
}
