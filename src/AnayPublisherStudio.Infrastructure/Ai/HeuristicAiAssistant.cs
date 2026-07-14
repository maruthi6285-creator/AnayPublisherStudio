using System.Text.RegularExpressions;
using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Domain.Blocks;
using AnayPublisherStudio.Domain.Model;

namespace AnayPublisherStudio.Infrastructure.Ai;

/// <summary>
/// Provider-free <see cref="IAiAssistant"/> using lightweight heuristics so the
/// app is fully functional offline. A cloud LLM implementation can replace it
/// via DI without changing any caller.
/// </summary>
public sealed class HeuristicAiAssistant : IAiAssistant
{
    /// <inheritdoc/>
    public Task<string> GenerateDescriptionAsync(BookDocument book, CancellationToken ct = default)
    {
        var first = book.Chapters
            .SelectMany(c => c.Blocks).OfType<ParagraphBlock>()
            .Select(p => p.PlainText).FirstOrDefault(t => t.Length > 60) ?? string.Empty;
        var blurb = first.Length > 320 ? first[..320].TrimEnd() + "…" : first;
        return Task.FromResult(blurb);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> SuggestKeywordsAsync(BookDocument book, CancellationToken ct = default)
    {
        var words = Regex.Matches(AllText(book).ToLowerInvariant(), "[a-z]{5,}")
            .Select(m => m.Value)
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(7)
            .ToList();
        return Task.FromResult<IReadOnlyList<string>>(words);
    }

    /// <inheritdoc/>
    public double ReadabilityScore(BookDocument book)
    {
        var text = AllText(book);
        var sentences = Math.Max(1, Regex.Matches(text, "[.!?]").Count);
        var words = Math.Max(1, Regex.Matches(text, "\\b\\w+\\b").Count);
        var syllables = Math.Max(1, Regex.Matches(text.ToLowerInvariant(), "[aeiouy]+").Count);
        // Flesch Reading Ease.
        var score = 206.835 - 1.015 * (words / (double)sentences) - 84.6 * (syllables / (double)words);
        return Math.Clamp(Math.Round(score, 1), 0, 100);
    }

    private static string AllText(BookDocument book) => string.Join(" ",
        book.Chapters.SelectMany(c => c.Blocks).OfType<ParagraphBlock>().Select(p => p.PlainText));
}
