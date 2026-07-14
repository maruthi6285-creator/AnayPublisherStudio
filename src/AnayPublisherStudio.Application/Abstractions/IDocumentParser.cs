using AnayPublisherStudio.Domain.Model;

namespace AnayPublisherStudio.Application.Abstractions;

/// <summary>
/// Parses a source manuscript stream into the platform-neutral Book Object
/// Model. Implementations detect chapters, headings, images, tables,
/// footnotes, page breaks and styles.
/// </summary>
public interface IDocumentParser
{
    /// <summary>Parses a DOCX stream into a <see cref="BookDocument"/>.</summary>
    /// <param name="docxStream">Readable stream positioned at the start of the DOCX.</param>
    /// <returns>The fully populated book model.</returns>
    BookDocument Parse(Stream docxStream);
}
