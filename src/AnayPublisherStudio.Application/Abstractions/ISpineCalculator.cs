using AnayPublisherStudio.Domain.Enums;

namespace AnayPublisherStudio.Application.Abstractions;

/// <summary>
/// Computes spine width from page count, paper stock and color mode using the
/// per-page thickness rules published by print-on-demand vendors.
/// </summary>
public interface ISpineCalculator
{
    /// <summary>Returns spine width in inches.</summary>
    double CalculateInches(int pageCount, PaperType paper, ColorMode color);
}
