using AnayPublisherStudio.Domain.Layout;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;

namespace AnayPublisherStudio.Application.Abstractions;

/// <summary>
/// Live publishing preview that renders directly from the layout engine
/// (not a PDF viewer). Supports zoom, continuous/single/facing pages, guides.
/// </summary>
public interface ILivePreviewEngine
{
    /// <summary>Builds or refreshes the composed layout for preview.</summary>
    Task<LayoutDocument> RefreshAsync(BookDocument book, PublishingTemplate template, CancellationToken ct = default);

    /// <summary>Renders a single page thumbnail as PNG bytes (presentation snapshot).</summary>
    Task<byte[]> RenderPageThumbnailAsync(LayoutDocument layout, BookDocument book, PublishingTemplate template, int pageNumber, double dpi = 72, CancellationToken ct = default);

    /// <summary>Renders a page image for the live surface at the given zoom.</summary>
    Task<byte[]> RenderPageAsync(LayoutDocument layout, BookDocument book, PublishingTemplate template, int pageNumber, double zoom = 1.0, CancellationToken ct = default);
}
