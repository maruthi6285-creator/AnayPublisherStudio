using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;

namespace AnayPublisherStudio.Application.Abstractions;

/// <summary>
/// Flows the book model into paginated interior pages, resolving margins,
/// running headers/footers, page numbers, chapter starts and the TOC.
/// </summary>
public interface ILayoutEngine
{
    /// <summary>
    /// Produces the interior print PDF and returns the resulting page count.
    /// </summary>
    /// <param name="book">Book model (its TOC is populated in place).</param>
    /// <param name="template">Active publishing template.</param>
    /// <param name="output">Writable destination stream for the PDF.</param>
    int Render(BookDocument book, PublishingTemplate template, Stream output);
}
