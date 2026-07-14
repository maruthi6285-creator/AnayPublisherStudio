using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Infrastructure.Layout;
using Xunit;

namespace AnayPublisherStudio.Tests;

public class SpineCalculatorTests
{
    [Fact]
    public void Bw_White_105Pages_MatchesKdpTemplate()
    {
        var calc = new KdpSpineCalculator();
        var spine = calc.CalculateInches(105, PaperType.White, ColorMode.BlackWhite);
        // KDP template for 105 BW white pages = 0.236in.
        Assert.InRange(spine, 0.235, 0.237);
    }

    [Fact]
    public void ZeroPages_ReturnsZero()
        => Assert.Equal(0, new KdpSpineCalculator().CalculateInches(0, PaperType.White, ColorMode.BlackWhite));
}
