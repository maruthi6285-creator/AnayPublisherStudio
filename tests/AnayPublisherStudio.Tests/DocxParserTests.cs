using AnayPublisherStudio.Infrastructure.Parsing;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

namespace AnayPublisherStudio.Tests;

public class DocxParserTests
{
    private static MemoryStream BuildDocx()
    {
        var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
        {
            var main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body(
                MakeHeading("Chapter One"),
                MakePara("First paragraph of the chapter.")));
            main.Document.Save();
        }
        ms.Position = 0;
        return ms;
    }

    private static Paragraph MakeHeading(string text) => new(
        new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" }),
        new Run(new Text(text)));

    private static Paragraph MakePara(string text) => new(new Run(new Text(text)));

    [Fact]
    public void Parse_DetectsChapterAndParagraph()
    {
        using var ms = BuildDocx();
        var book = new DocxDocumentParser().Parse(ms);
        Assert.Single(book.Chapters);
        Assert.Equal("Chapter One", book.Chapters[0].Title);
        Assert.Equal(1, book.Chapters[0].Number);
        Assert.True(book.TotalBlocks >= 2);
    }
}
