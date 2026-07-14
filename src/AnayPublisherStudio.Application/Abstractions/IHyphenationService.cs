namespace AnayPublisherStudio.Application.Abstractions;

/// <summary>
/// Language-aware hyphenation dictionaries. Presentation only —
/// never changes author wording; only suggests break opportunities.
/// </summary>
public interface IHyphenationService
{
    /// <summary>Returns soft-hyphen break positions (character indices) for a word.</summary>
    IReadOnlyList<int> GetBreakPositions(string word, string language = "en-US");

    /// <summary>True when a dictionary is available for the language.</summary>
    bool Supports(string language);
}
