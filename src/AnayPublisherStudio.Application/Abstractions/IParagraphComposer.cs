using AnayPublisherStudio.Domain.ValueObjects;

namespace AnayPublisherStudio.Application.Abstractions;

/// <summary>
/// Professional paragraph/line composer (justification, hyphenation points).
/// Operates on presentation metrics only; never rewrites author text.
/// </summary>
public interface IParagraphComposer
{
    /// <summary>
    /// Measures how many lines the given author text would occupy at the given
    /// frame width and typography. Text is measured as-is.
    /// </summary>
    int MeasureLineCount(string authorText, Typography typography, double frameWidthInches);

    /// <summary>
    /// Returns soft-wrap line lengths (character counts) without altering text.
    /// </summary>
    IReadOnlyList<int> ComposeLineBreaks(string authorText, Typography typography, double frameWidthInches);
}
