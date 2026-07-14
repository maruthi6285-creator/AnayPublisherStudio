using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;

namespace AnayPublisherStudio.Application.Abstractions;

/// <summary>
/// Builds a print-ready wraparound cover PDF (back + spine + front) sized to
/// the template's overall dimensions, honouring the barcode safe area.
/// </summary>
public interface ICoverEngine
{
    /// <summary>Generates the cover PDF into <paramref name="output"/>.</summary>
    void Render(PublishingProject project, PublishingTemplate template, int pageCount, Stream output);
}
