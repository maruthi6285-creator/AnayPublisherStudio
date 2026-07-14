using System.Text;
using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Domain.Blocks;
using AnayPublisherStudio.Domain.Layout;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AnayPublisherStudio.Rendering.Preview;

/// <summary>
/// Live publishing preview engine. Renders pages directly from the professional
/// layout engine (trim, bleed, margins, live area, running headers/footers,
/// page numbers, image boxes, table frames). Not a PDF viewer.
/// </summary>
public sealed class LivePreviewEngine : ILivePreviewEngine
{
    private readonly IProfessionalLayoutEngine _layoutEngine;
    private readonly ITypographyEngine? _typography;

    /// <summary>Creates the live preview engine.</summary>
    public LivePreviewEngine(IProfessionalLayoutEngine layoutEngine, ITypographyEngine? typography = null)
    {
        _layoutEngine = layoutEngine;
        _typography = typography;
    }

    /// <inheritdoc/>
    public Task<LayoutDocument> RefreshAsync(BookDocument book, PublishingTemplate template, CancellationToken ct = default)
        => _layoutEngine.ComposeAsync(book, template, ct);

    /// <inheritdoc/>
    public Task<byte[]> RenderPageThumbnailAsync(
        LayoutDocument layout, BookDocument book, PublishingTemplate template,
        int pageNumber, double dpi = 72, CancellationToken ct = default)
        => RenderPageAsync(layout, book, template, pageNumber, zoom: dpi / 72.0, ct);

    /// <inheritdoc/>
    public Task<byte[]> RenderPageAsync(
        LayoutDocument layout, BookDocument book, PublishingTemplate template,
        int pageNumber, double zoom = 1.0, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            RenderingLicense.Configure();

            var page = layout.Pages.FirstOrDefault(p => p.PageNumber == pageNumber)
                       ?? layout.Pages.FirstOrDefault()
                       ?? throw new InvalidOperationException("Layout has no pages.");

            // Render a single-page PDF snapshot of the composed page with guides.
            using var ms = new MemoryStream();
            Document.Create(container =>
            {
                container.Page(p =>
                {
                    p.Size(new PageSize((float)template.TrimWidth, (float)template.TrimHeight, Unit.Inch));
                    p.MarginTop((float)page.Margins.Top, Unit.Inch);
                    p.MarginBottom((float)page.Margins.Bottom, Unit.Inch);
                    p.MarginLeft((float)page.Margins.Left(page.Side), Unit.Inch);
                    p.MarginRight((float)page.Margins.Right(page.Side), Unit.Inch);

                    if (!string.IsNullOrEmpty(page.RunningHeader))
                        p.Header().AlignCenter().Text(page.RunningHeader).FontSize(9).Italic().FontColor(Colors.Grey.Darken1);

                    p.Footer().AlignCenter().Text(page.RunningFooter).FontSize(9);

                    p.Content().Column(col =>
                    {
                        if (page.IsBlank)
                        {
                            col.Item().AlignCenter().Text(" ").FontSize(1);
                            return;
                        }

                        // Resolve chapter blocks referenced by order.
                        var chapter = book.Chapters.FirstOrDefault(c => c.Number == page.ChapterNumber)
                                      ?? book.Chapters.FirstOrDefault();
                        if (chapter is null) return;

                        foreach (var order in page.BlockOrders)
                        {
                            var block = chapter.Blocks.FirstOrDefault(b => b.Order == order);
                            if (block is null) continue;
                            switch (block)
                            {
                                case HeadingBlock h:
                                    col.Item().Text(h.Text).Bold().FontSize(16);
                                    break;
                                case ParagraphBlock para:
                                    col.Item().Text(text =>
                                    {
                                        text.Justify();
                                        foreach (var run in para.Runs)
                                        {
                                            var span = text.Span(run.Text);
                                            if (run.Bold) span.Bold();
                                            if (run.Italic) span.Italic();
                                        }
                                    });
                                    break;
                                case ImageBlock img when img.Data.Length > 0:
                                    col.Item().AlignCenter().Element(e =>
                                    {
                                        try { e.Image(img.Data).FitWidth(); } catch { /* skip */ }
                                    });
                                    break;
                                case TableBlock tb:
                                    col.Item().Border(0.5f).Padding(4).Text($"[Table {tb.Rows.Count}×{tb.ColumnCount}]").FontSize(8);
                                    break;
                            }
                        }
                    });
                });
            }).GeneratePdf(ms);

            return ms.ToArray();
        }, ct);
    }
}
