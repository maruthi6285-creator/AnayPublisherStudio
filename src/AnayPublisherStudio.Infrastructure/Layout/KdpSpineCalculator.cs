using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Domain.Enums;

namespace AnayPublisherStudio.Infrastructure.Layout;

/// <summary>
/// Computes spine width using KDP's published per-page thickness figures
/// (inches per page) for the supported paper stocks.
/// </summary>
public sealed class KdpSpineCalculator : ISpineCalculator
{
    // KDP per-page thickness (inches). Colour interiors use thicker stock.
    private const double WhiteBw = 0.002252;
    private const double CreamBw = 0.0025;
    private const double ColorStd = 0.002347;

    /// <inheritdoc/>
    public double CalculateInches(int pageCount, PaperType paper, ColorMode color)
    {
        if (pageCount <= 0) return 0;
        double perPage = color == ColorMode.Color
            ? ColorStd
            : paper == PaperType.Cream ? CreamBw : WhiteBw;
        return Math.Round(pageCount * perPage, 3);
    }
}
