using AnayPublisherStudio.Application.Abstractions;
using DomainTypography = AnayPublisherStudio.Domain.ValueObjects.Typography;

namespace AnayPublisherStudio.Typography;

/// <summary>
/// Professional paragraph / line composer. Measures and proposes soft wrap
/// points without changing author wording. Supports justification metrics,
/// non-breaking space awareness, and hyphenation opportunities.
/// </summary>
public sealed class ParagraphComposer : IParagraphComposer
{
    private readonly IHyphenationService? _hyphenation;

    /// <summary>Creates the composer with optional hyphenation.</summary>
    public ParagraphComposer(IHyphenationService? hyphenation = null)
    {
        _hyphenation = hyphenation;
    }

    /// <inheritdoc/>
    public int MeasureLineCount(string authorText, DomainTypography typography, double frameWidthInches)
    {
        var breaks = ComposeLineBreaks(authorText, typography, frameWidthInches);
        return Math.Max(1, breaks.Count);
    }

    /// <inheritdoc/>
    public IReadOnlyList<int> ComposeLineBreaks(string authorText, DomainTypography typography, double frameWidthInches)
    {
        if (string.IsNullOrEmpty(authorText))
            return new[] { 0 };

        var em = Math.Max(8.0, typography.FontSizePoints);
        var tracking = 1.0 + typography.TrackingEm;
        var avgCharInches = (em * 0.5 * tracking) / 72.0;
        var maxChars = Math.Max(10, (int)Math.Floor(frameWidthInches / Math.Max(0.01, avgCharInches)));

        var lines = new List<int>();
        var i = 0;
        while (i < authorText.Length)
        {
            var remaining = authorText.Length - i;
            if (remaining <= maxChars)
            {
                lines.Add(remaining);
                break;
            }

            var window = authorText.AsSpan(i, maxChars);
            var breakAt = -1;
            for (var j = window.Length - 1; j > maxChars / 3; j--)
            {
                var ch = window[j];
                if (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r')
                {
                    breakAt = j + 1;
                    break;
                }
            }

            if (breakAt < 0 && typography.Hyphenation && _hyphenation is not null)
            {
                var wordStart = 0;
                for (var j = window.Length - 1; j >= 0; j--)
                {
                    if (window[j] == ' ') { wordStart = j + 1; break; }
                }
                var word = window[wordStart..].ToString();
                var positions = _hyphenation.GetBreakPositions(word, typography.Language);
                if (positions.Count > 0)
                    breakAt = wordStart + positions[^1];
            }

            if (breakAt <= 0) breakAt = maxChars;
            lines.Add(breakAt);
            i += breakAt;
        }

        return lines;
    }
}
