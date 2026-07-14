using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Domain.Blocks;
using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;
using SixLabors.ImageSharp;
using DrawingBlip = DocumentFormat.OpenXml.Drawing.Blip;
using WpParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using WpRun = DocumentFormat.OpenXml.Wordprocessing.Run;
using WpTable = DocumentFormat.OpenXml.Wordprocessing.Table;

namespace AnayPublisherStudio.Infrastructure.Parsing;

/// <summary>
/// OpenXML-based implementation of <see cref="IDocumentParser"/>. Walks the
/// Word body in document order, classifying each element into the Book Object
/// Model. Heading-1 paragraphs start a new chapter.
/// </summary>
public sealed class DocxDocumentParser : IDocumentParser
{
    private const int EmusPerInch = 914400;

    /// <inheritdoc/>
    public BookDocument Parse(Stream docxStream)
    {
        var book = new BookDocument();
        using var doc = WordprocessingDocument.Open(docxStream, false);
        var main = doc.MainDocumentPart ?? throw new InvalidDataException("DOCX has no main document part.");

        book.Metadata.Title = doc.PackageProperties.Title ?? string.Empty;
        book.Metadata.Author = doc.PackageProperties.Creator ?? string.Empty;

        // Capture package properties read-only. The source stream is opened
        // with isEditable=false; nothing is ever written back to the DOCX.
        book.Properties = new DocumentProperties
        {
            Application = doc.PackageProperties.LastModifiedBy,
            Created = doc.PackageProperties.Created.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(doc.PackageProperties.Created.Value, DateTimeKind.Utc))
                : null,
            Modified = doc.PackageProperties.Modified.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(doc.PackageProperties.Modified.Value, DateTimeKind.Utc))
                : null,
            Revision = doc.PackageProperties.Revision,
        };

        ReadFootnotes(main, book);

        var body = main.Document.Body;
        if (body is null) return book;

        var current = new Chapter { Title = "Front Matter", Number = 0 };
        void Commit()
        {
            if (current.Blocks.Count > 0) book.Chapters.Add(current);
        }

        int order = 0;
        foreach (var element in body.ChildElements)
        {
            switch (element)
            {
                case WpParagraph p:
                    var level = HeadingLevelOf(p, main);
                    if (level == HeadingLevel.H1)
                    {
                        Commit();
                        current = new Chapter { Title = InnerText(p), Number = book.Chapters.Count + 1 };
                        order = 0;
                        current.Blocks.Add(new HeadingBlock { Level = HeadingLevel.H1, Text = InnerText(p), Order = order++ });
                        break;
                    }
                    if (level is not null)
                    {
                        current.Blocks.Add(new HeadingBlock { Level = level.Value, Text = InnerText(p), Order = order++ });
                        break;
                    }
                    if (HasPageBreak(p)) current.Blocks.Add(new PageBreakBlock { Order = order++ });
                    var img = ExtractImage(p, main);
                    if (img is not null) { img.Order = order++; current.Blocks.Add(img); break; }
                    var para = BuildParagraph(p);
                    if (para.Runs.Count > 0) { para.Order = order++; current.Blocks.Add(para); }
                    break;

                case WpTable t:
                    current.Blocks.Add(BuildTable(t, order++));
                    break;
            }
        }
        Commit();
        return book;
    }

    private static void ReadFootnotes(MainDocumentPart main, BookDocument book)
    {
        var fnPart = main.FootnotesPart;
        if (fnPart?.Footnotes is null) return;
        foreach (var fn in fnPart.Footnotes.Elements<DocumentFormat.OpenXml.Wordprocessing.Footnote>())
        {
            var id = fn.Id?.Value.ToString() ?? string.Empty;
            if (id is "0" or "-1" or "") continue;
            book.Footnotes.Add(new Domain.Model.Footnote { Id = id, Text = fn.InnerText });
        }
    }

    private static HeadingLevel? HeadingLevelOf(WpParagraph p, MainDocumentPart main)
    {
        var styleId = p.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (string.IsNullOrEmpty(styleId)) return null;
        var s = styleId.ToLowerInvariant();
        if (s.StartsWith("heading"))
        {
            var digits = new string(s.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out var n) && n is >= 1 and <= 6) return (HeadingLevel)n;
            if (s.Contains("title")) return HeadingLevel.H1;
        }
        if (s == "title") return HeadingLevel.H1;
        return null;
    }

    private static bool HasPageBreak(WpParagraph p)
        => p.Descendants<Break>().Any(b => b.Type is not null && b.Type.Value == BreakValues.Page);

    private static ParagraphBlock BuildParagraph(WpParagraph p)
    {
        var block = new ParagraphBlock
        {
            StyleName = p.ParagraphProperties?.ParagraphStyleId?.Val?.Value,
            Alignment = MapAlignment(p.ParagraphProperties?.Justification?.Val),
        };
        foreach (var r in p.Elements<WpRun>())
        {
            var text = string.Concat(r.Elements<Text>().Select(t => t.Text));
            if (string.IsNullOrEmpty(text)) continue;
            var rp = r.RunProperties;
            block.Runs.Add(new TextRun
            {
                Text = text,
                Bold = rp?.Bold is not null,
                Italic = rp?.Italic is not null,
                Underline = rp?.Underline is not null,
            });
        }
        return block;
    }

    private static Domain.Enums.TextAlignment MapAlignment(EnumValue<JustificationValues>? j)
    {
        if (j is null) return Domain.Enums.TextAlignment.Justify;
        if (j.Value == JustificationValues.Center) return Domain.Enums.TextAlignment.Center;
        if (j.Value == JustificationValues.Right) return Domain.Enums.TextAlignment.Right;
        if (j.Value == JustificationValues.Both) return Domain.Enums.TextAlignment.Justify;
        return Domain.Enums.TextAlignment.Left;
    }

    private static TableBlock BuildTable(WpTable t, int order)
    {
        var block = new TableBlock { Order = order };
        foreach (var row in t.Elements<TableRow>())
        {
            var cells = new List<string>();
            foreach (var cell in row.Elements<TableCell>())
                cells.Add(cell.InnerText);
            block.Rows.Add(cells);
        }
        return block;
    }

    private static ImageBlock? ExtractImage(WpParagraph p, MainDocumentPart main)
    {
        var blip = p.Descendants<DrawingBlip>().FirstOrDefault();
        var embed = blip?.Embed?.Value;
        if (string.IsNullOrEmpty(embed)) return null;
        if (main.GetPartById(embed) is not ImagePart imagePart) return null;

        using var s = imagePart.GetStream();
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        var bytes = ms.ToArray();

        var block = new ImageBlock { Data = bytes, ContentType = imagePart.ContentType };
        try
        {
            using var image = Image.Load(bytes);
            block.PixelWidth = image.Width;
            block.PixelHeight = image.Height;
            var extent = p.Descendants<DocumentFormat.OpenXml.Drawing.Extents>().FirstOrDefault();
            if (extent?.Cx is not null && extent.Cx.Value > 0)
            {
                var displayInches = extent.Cx.Value / (double)EmusPerInch;
                block.Dpi = image.Width / displayInches;
            }
        }
        catch { /* unknown image format: leave pixel dims at zero */ }
        return block;
    }

    private static string InnerText(WpParagraph p) => p.InnerText.Trim();
}
