using AnayPublisherStudio.Domain.Cover;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;

namespace AnayPublisherStudio.Application.Abstractions;

/// <summary>
/// Professional cover designer: layers, guides, spine calc, barcode reservation,
/// KDP-safe zones. Builds a <see cref="CoverDesign"/> and can export print-ready PDF
/// via the existing <see cref="ICoverEngine"/> path.
/// </summary>
public interface ICoverDesigner
{
    /// <summary>Creates a cover design model from project + template + page count.</summary>
    CoverDesign CreateDesign(PublishingProject project, PublishingTemplate template, int pageCount);

    /// <summary>Recalculates spine width and overall dimensions for a design.</summary>
    void RecalculateSpine(CoverDesign design, PublishingTemplate template, int pageCount, ISpineCalculator spine);

    /// <summary>Validates design against template safe zones / barcode / bleed.</summary>
    IReadOnlyList<string> ValidateDesign(CoverDesign design, PublishingTemplate template);
}
