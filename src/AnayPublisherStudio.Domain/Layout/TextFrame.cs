namespace AnayPublisherStudio.Domain.Layout;

/// <summary>
/// Immutable text-frame geometry in inches. Presentation only; never carries author text.
/// </summary>
public sealed record TextFrame(
    double XInches,
    double YInches,
    double WidthInches,
    double HeightInches)
{
    /// <summary>Live-area width.</summary>
    public double LiveWidth => WidthInches;

    /// <summary>Live-area height.</summary>
    public double LiveHeight => HeightInches;
}
