using System.Text.RegularExpressions;
using AnayPublisherStudio.Application.Abstractions;

namespace AnayPublisherStudio.Typography.Hyphenation;

/// <summary>
/// Lightweight language-aware hyphenation service.
/// Provides break *opportunities* only — never inserts characters into author text.
/// </summary>
public sealed class HyphenationService : IHyphenationService
{
    // Minimal English patterns (presentation aid). Full TeX patterns can be loaded via plugins.
    private static readonly Regex Vowel = new("[aeiouyAEIOUY]", RegexOptions.Compiled);

    /// <inheritdoc/>
    public bool Supports(string language)
        => language.StartsWith("en", StringComparison.OrdinalIgnoreCase)
           || language.StartsWith("hi", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public IReadOnlyList<int> GetBreakPositions(string word, string language = "en-US")
    {
        if (string.IsNullOrWhiteSpace(word) || word.Length < 5)
            return Array.Empty<int>();

        var breaks = new List<int>();
        // Simple algorithm: allow breaks after a vowel+consonant pair, keeping
        // at least 2 chars on each side. Does not mutate the word.
        for (var i = 2; i < word.Length - 2; i++)
        {
            if (Vowel.IsMatch(word[i - 1].ToString()) && !Vowel.IsMatch(word[i].ToString()))
                breaks.Add(i);
        }
        return breaks;
    }
}
