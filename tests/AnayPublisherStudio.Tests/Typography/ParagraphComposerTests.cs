using AnayPublisherStudio.Typography;
using AnayPublisherStudio.Typography.Hyphenation;
using DomainTypography = AnayPublisherStudio.Domain.ValueObjects.Typography;
using Xunit;

namespace AnayPublisherStudio.Tests.Typography;

public class ParagraphComposerTests
{
    [Fact]
    public void MeasureLineCount_DoesNotAlterText()
    {
        var text = "The quick brown fox jumps over the lazy dog. " + new string('a', 200);
        var original = text;
        var composer = new ParagraphComposer(new HyphenationService());
        var ty = new DomainTypography { FontSizePoints = 11, LineHeight = 1.35, Hyphenation = true };
        var lines = composer.MeasureLineCount(text, ty, 4.9);
        Assert.True(lines >= 2);
        Assert.Equal(original, text);
    }

    [Fact]
    public void Hyphenation_ReturnsBreaks_WithoutMutation()
    {
        var word = "hyphenation";
        var svc = new HyphenationService();
        var breaks = svc.GetBreakPositions(word);
        Assert.NotEmpty(breaks);
        Assert.Equal("hyphenation", word);
    }
}
