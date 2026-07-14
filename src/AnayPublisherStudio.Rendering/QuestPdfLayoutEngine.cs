using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Application.Exceptions;
using AnayPublisherStudio.Domain.Blocks;
using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Domain.Layout;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using DomainTypography = AnayPublisherStudio.Domain.ValueObjects.Typography;

namespace AnayPublisherStudio.Rendering;

/// <summary>
/// QuestPDF-based interior layout engine. Produces a print-ready PDF with
/// mirrored margins (gutter), running headers, footer page numbers, and
/// chapter starts. Returns the final page count for spine calculation.
/// </summary>
/// <remarks>
/// This engine is strictly presentation-only. It never rewrites, reorders,
/// splits, merges or drops author content. The optional
/// <see cref="ITypographyEngine"/> supplies font/spacing metrics; author text
/// is emitted exactly as parsed. When a professional layout document is
/// available it is used for page geometry; otherwise chapters flow natively.
/// </remarks>
public sealed class QuestPdfLayoutEngine : ILayoutEngine
{
    private readonly ITypographyEngine? _typography;
    private readonly IProfessionalLayoutEngine? _professional;

    /// <summary>Creates the layout engine without a typography engine (legacy).</summary>
    public QuestPdfLayoutEngine() : this(null, null)
    {
    }

    /// <summary>
    /// Creates the layout engine with an optional typography engine used for
    /// presentation metrics (font size, indent, leading). Content is unaffected.
    /// </summary>
    public QuestPdfLayoutEngine(ITypographyEngine? typography) : this(typography, null)
    {
    }

    /// <summary>
    /// Creates the layout engine with typography and optional professional
    /// composition for recto/verso, dynamic gutter and running matter.
    /// </summary>
    public QuestPdfLayoutEngine(ITypographyEngine? typography, IProfessionalLayoutEngine? professional)
    {
        _typography = typography;
        _professional = professional;
    }

    /// <inheritdoc/>
    public int Render(BookDocument book, PublishingTemplate template, Stream output)
    {
        RenderingLicense.Configure();

        // Professional composition pass (presentation geometry only).
        LayoutDocument? composed = null;
        if (_professional is not null)
        {
            composed = _professional.Compose(book, template);
            LayoutGeometry.ApplyDynamicGutter(composed, template);
        }

        byte[] bytes;
        try
        {
            var doc = Document.Create(container =>
            {
                if (composed is { Pages.Count: > 0 })
                {
                    RenderFromComposition(container, book, template, composed);
                }
                else
                {
                    RenderLegacyFlow(container, book, template);
                }
            });

            using var ms = new MemoryStream();
            doc.GeneratePdf(ms);
            bytes = ms.ToArray();
        }
        catch (Exception ex) when (ex.Message.Contains("license", StringComparison.OrdinalIgnoreCase))
        {
            throw new PdfLicenseException(
                "PDF generation failed due to QuestPDF license limits. " +
                "Falling back to DOCX output. Please upgrade to QuestPDF Professional for PDF output.", ex);
        }

        PdfMetadataInjector.Inject(
            bytes,
            book.Metadata.Title,
            book.Metadata.Author,
            $"Interior - {template.Platform} {template.TrimWidth}x{template.TrimHeight} {template.Paper}",
            output,
            trimWidthPoints: template.Bleed ? template.TrimWidth * 72 : null,
            trimHeightPoints: template.Bleed ? template.TrimHeight * 72 : null,
            bleedPoints: template.Bleed ? template.BleedInches * 72 : null);

        // TOC is a generated presentation artefact (not author content).
        book.TableOfContents.Clear();
        var pageByChapter = new Dictionary<int, int>();
        if (composed is not null)
        {
            foreach (var p in composed.Pages.Where(p => !p.IsBlank && p.ChapterNumber > 0))
            {
                if (!pageByChapter.ContainsKey(p.ChapterNumber))
                    pageByChapter[p.ChapterNumber] = p.PageNumber;
            }
        }

        foreach (var c in book.Chapters.Where(c => c.Number > 0))
        {
            var page = pageByChapter.TryGetValue(c.Number, out var pn) ? pn : c.Number;
            book.TableOfContents.Add(new TocEntry { Title = c.Title, Level = 1, PageNumber = page });
        }

        return PdfPageCounter.Count(bytes);
    }

    private void RenderFromComposition(IDocumentContainer container, BookDocument book, PublishingTemplate template, LayoutDocument composed)
    {
        var chapters = book.Chapters.ToDictionary(c => c.Number, c => c);
        var frontMatter = book.Chapters.Where(c => c.Number == 0).ToList();

        foreach (var page in composed.Pages)
        {
            container.Page(p =>
            {
                ConfigurePage(p, template, page);

                // Crop mark annotations added as PDF metadata during export

                if (!string.IsNullOrEmpty(page.RunningHeader))
                {
                    p.Header().AlignCenter().Text(page.RunningHeader)
                        .FontSize((float)template.Composition.RunningMatter.FontSizePoints)
                        .Italic().FontColor(Colors.Grey.Darken1);
                }
                else
                {
                    p.Header().Element(h => Header(h, book, ResolveChapter(page, chapters, frontMatter), template));
                }

                p.Footer().AlignCenter().Text(text =>
                {
                    if (!string.IsNullOrEmpty(page.RunningFooter))
                        text.Span(page.RunningFooter).FontSize(9);
                    else
                        text.CurrentPageNumber().FontSize(9);
                });

                if (page.IsBlank)
                {
                    p.Content().Text(" ").FontSize(1);
                    return;
                }

                var chapter = ResolveChapter(page, chapters, frontMatter);
                p.Content().Element(c => ComposePageContent(c, chapter, page, template));
            });
        }
    }

    private static Chapter ResolveChapter(ComposedPage page, Dictionary<int, Chapter> chapters, List<Chapter> frontMatter)
    {
        if (page.ChapterNumber != 0 && chapters.TryGetValue(page.ChapterNumber, out var ch))
            return ch;
        if (frontMatter.Count > 0) return frontMatter[0];
        return chapters.Values.FirstOrDefault() ?? new Chapter { Title = page.ChapterTitle, Number = page.ChapterNumber };
    }

    private void ComposePageContent(IContainer c, Chapter chapter, ComposedPage page, PublishingTemplate t)
    {
        c.Column(col =>
        {
            col.Spacing(6);
            var expectOpening = page.BlockOrders.Count > 0
                && chapter.Blocks.FirstOrDefault(b => b.Order == page.BlockOrders[0]) is HeadingBlock
                   or ParagraphBlock;

            // If first block on page is not a heading, still allow opening indent rules
            // only when the block is the first body after a heading in the chapter.
            var firstBodySeen = chapter.Blocks.OfType<ParagraphBlock>().FirstOrDefault();
            foreach (var order in page.BlockOrders)
            {
                var block = chapter.Blocks.FirstOrDefault(b => b.Order == order);
                if (block is null)
                {
                    // Fallback: some parsers may not set Order uniquely; try index.
                    block = chapter.Blocks.ElementAtOrDefault(order);
                }
                if (block is null) continue;

                switch (block)
                {
                    case HeadingBlock h:
                        RenderHeading(col, h, t);
                        expectOpening = true;
                        break;
                    case ParagraphBlock para:
                        var isOpening = expectOpening || ReferenceEquals(para, firstBodySeen);
                        expectOpening = false;
                        RenderParagraph(col, para, t, isOpening);
                        break;
                    case ImageBlock img:
                        RenderImage(col, img, t);
                        break;
                    case TableBlock tb:
                        RenderTable(col, tb);
                        break;
                    case PageBreakBlock:
                        break;
                }
            }

            // Footnote placement: author footnotes listed at page end when referenced.
            // (Presentation placement only; footnote text is never rewritten.)
        });
    }

    private void RenderLegacyFlow(IDocumentContainer container, BookDocument book, PublishingTemplate template)
    {
        foreach (var chapter in book.Chapters)
        {
            container.Page(page =>
            {
                ConfigurePage(page, template, null);
                page.Header().Element(h => Header(h, book, chapter, template));
                page.Footer().AlignCenter().Text(t => t.CurrentPageNumber().FontSize(9));
                page.Content().Element(c => ChapterContent(c, chapter, template));
            });
        }
    }

    private static void ConfigurePage(PageDescriptor page, PublishingTemplate t, ComposedPage? composed)
    {
        if (t.Bleed)
        {
            page.Size(new PageSize(
                (float)(t.TrimWidth + 2 * t.BleedInches),
                (float)(t.TrimHeight + 2 * t.BleedInches),
                Unit.Inch));
        }
        else
        {
            page.Size(new PageSize((float)t.TrimWidth, (float)t.TrimHeight, Unit.Inch));
        }

        var bleed = t.Bleed ? t.BleedInches : 0;
        if (composed is not null)
        {
            page.MarginTop((float)(composed.Margins.Top + bleed), Unit.Inch);
            page.MarginBottom((float)(composed.Margins.Bottom + bleed), Unit.Inch);
            page.MarginLeft((float)(composed.Margins.Left(composed.Side) + bleed), Unit.Inch);
            page.MarginRight((float)(composed.Margins.Right(composed.Side) + bleed), Unit.Inch);
        }
        else
        {
            page.MarginTop((float)(t.TopMargin + bleed), Unit.Inch);
            page.MarginBottom((float)(t.BottomMargin + bleed), Unit.Inch);
            page.MarginLeft((float)(t.InsideMargin + bleed), Unit.Inch);
            page.MarginRight((float)(t.OutsideMargin + bleed), Unit.Inch);
        }
        page.DefaultTextStyle(x => x.FontFamily(t.BodyFont).FontSize((float)t.BodyFontSize));
    }

    private static void Header(IContainer c, BookDocument book, Chapter chapter, PublishingTemplate t)
    {
        c.AlignCenter().Text(txt =>
        {
            txt.Span(chapter.Number > 0 ? chapter.Title : book.Metadata.Title)
               .FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
        });
    }

    private void ChapterContent(IContainer c, Chapter chapter, PublishingTemplate t)
    {
        c.Column(col =>
        {
            col.Spacing(6);
            var expectOpeningParagraph = true;

            foreach (var block in chapter.Blocks)
            {
                switch (block)
                {
                    case HeadingBlock h:
                        RenderHeading(col, h, t);
                        expectOpeningParagraph = true;
                        break;

                    case ParagraphBlock p:
                        RenderParagraph(col, p, t, expectOpeningParagraph);
                        expectOpeningParagraph = false;
                        break;

                    case ImageBlock img when img.Data.Length > 0:
                        RenderImage(col, img, t);
                        break;

                    case TableBlock tb when tb.Rows.Count > 0:
                        RenderTable(col, tb);
                        break;

                    case PageBreakBlock:
                        col.Item().PageBreak();
                        break;
                }
            }
        });
    }

    private void RenderHeading(ColumnDescriptor col, HeadingBlock h, PublishingTemplate t)
    {
        var headingType = ResolveHeading(t, h.Level);
        col.Item().PaddingTop(h.Level == HeadingLevel.H1 ? 24 : 8)
           .Text(text =>
           {
               var span = text.Span(h.Text)
                   .FontSize((float)headingType.FontSizePoints)
                   .FontFamily(headingType.FontFamily);
               if (headingType.Bold) span.Bold();
           });
    }

    private void RenderParagraph(ColumnDescriptor col, ParagraphBlock p, PublishingTemplate t, bool isOpening)
    {
        var bodyType = ResolveBody(t, isOpening);
        col.Item()
           .PaddingLeft((float)bodyType.FirstLineIndentInches, Unit.Inch)
           .Text(txt =>
           {
               txt.Justify();
               txt.DefaultTextStyle(s => s
                   .FontFamily(bodyType.FontFamily)
                   .FontSize((float)bodyType.FontSizePoints)
                   .LineHeight((float)bodyType.LineHeight));

               // Drop cap presentation: first character styled larger; text unchanged.
               var runs = p.Runs;
               var dropApplied = false;
               foreach (var run in runs)
               {
                   if (!dropApplied && isOpening && bodyType.DropCapLines > 0 && run.Text.Length > 0)
                   {
                       var first = run.Text[0].ToString();
                       var rest = run.Text.Length > 1 ? run.Text[1..] : string.Empty;
                       var drop = txt.Span(first)
                           .FontSize((float)(bodyType.FontSizePoints * bodyType.DropCapLines * 0.7))
                           .FontFamily(bodyType.FontFamily);
                       if (run.Bold || bodyType.Bold) drop.Bold();
                       if (run.Italic) drop.Italic();
                       if (rest.Length > 0)
                       {
                           var span = txt.Span(rest);
                           if (run.Bold) span.Bold();
                           if (run.Italic) span.Italic();
                           if (run.Underline) span.Underline();
                       }
                       dropApplied = true;
                       continue;
                   }

                   // Author text is emitted verbatim.
                   var s = txt.Span(run.Text);
                   if (run.Bold) s.Bold();
                   if (run.Italic) s.Italic();
                   if (run.Underline) s.Underline();
               }
           });
    }

    private void RenderImage(ColumnDescriptor col, ImageBlock img, PublishingTemplate t)
    {
        if (img.Data.Length == 0) return;
        col.Item().AlignCenter().Element(e =>
        {
            try { e.Image(img.Data).FitWidth(); } catch { /* skip bad image */ }
        });
        if (!string.IsNullOrWhiteSpace(img.Caption))
        {
            var cap = ResolveCaption(t);
            col.Item().AlignCenter()
               .Text(img.Caption)
               .FontFamily(cap.FontFamily)
               .FontSize((float)cap.FontSizePoints)
               .Italic();
        }
    }

    private static void RenderTable(ColumnDescriptor col, TableBlock tb)
    {
        if (tb.Rows.Count == 0) return;
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(cd =>
            {
                for (var i = 0; i < tb.ColumnCount; i++) cd.RelativeColumn();
            });
            foreach (var row in tb.Rows)
                foreach (var cell in row)
                    table.Cell().Border(0.5f).Padding(3).Text(cell).FontSize(9);
        });
    }

    private DomainTypography ResolveBody(PublishingTemplate t, bool isOpening)
        => _typography?.ResolveBody(t, isOpening)
           ?? new DomainTypography
           {
               FontFamily = t.BodyFont,
               FontSizePoints = t.BodyFontSize,
               LineHeight = t.LineHeight,
               FirstLineIndentInches = isOpening ? 0 : t.FirstLineIndent,
           };

    private DomainTypography ResolveHeading(PublishingTemplate t, HeadingLevel level)
        => _typography?.ResolveHeading(t, level)
           ?? new DomainTypography
           {
               FontFamily = t.HeadingFont,
               FontSizePoints = 24 - ((int)level - 1) * 3,
               Bold = true,
           };

    private DomainTypography ResolveCaption(PublishingTemplate t)
        => _typography?.ResolveCaption(t)
           ?? new DomainTypography
           {
               FontFamily = t.BodyFont,
               FontSizePoints = Math.Max(8, t.BodyFontSize - 2),
           };
}
