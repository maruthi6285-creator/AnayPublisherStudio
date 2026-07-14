using AnayPublisherStudio.Domain.Layout;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;

namespace AnayPublisherStudio.Application.Abstractions;

/// <summary>
/// Professional page composition engine. Builds a <see cref="LayoutDocument"/>
/// with pagination, recto/verso, gutters, running matter, widow/orphan control,
/// and text frames. Presentation only — never modifies author content.
/// </summary>
public interface IProfessionalLayoutEngine
{
    /// <summary>
    /// Composes the book into pages without writing a PDF.
    /// Safe for live preview and incremental pagination.
    /// </summary>
    LayoutDocument Compose(BookDocument book, PublishingTemplate template);

    /// <summary>
    /// Composes asynchronously (for large manuscripts and background rendering).
    /// </summary>
    Task<LayoutDocument> ComposeAsync(BookDocument book, PublishingTemplate template, CancellationToken ct = default);
}
