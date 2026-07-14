namespace AnayPublisherStudio.Domain.Layout;

/// <summary>
/// Fully composed book layout: pages, frames, running matter.
/// Presentation artefact; author content remains in <c>BookDocument</c>.
/// </summary>
public sealed class LayoutDocument
{
    /// <summary>Composed pages in reading order.</summary>
    public List<ComposedPage> Pages { get; init; } = new();

    /// <summary>Trim width in inches.</summary>
    public double TrimWidth { get; init; }

    /// <summary>Trim height in inches.</summary>
    public double TrimHeight { get; init; }

    /// <summary>Bleed in inches (0 if none).</summary>
    public double BleedInches { get; init; }

    /// <summary>Composition rules used to produce this layout.</summary>
    public CompositionRules Rules { get; init; } = new();

    /// <summary>Total page count.</summary>
    public int PageCount => Pages.Count;
}
