using AnayPublisherStudio.Domain.Blocks;
using AnayPublisherStudio.Domain.Model;

namespace AnayPublisherStudio.Application.Abstractions;

/// <summary>
/// AI-assisted authoring services. The default implementation is a safe no-op;
/// a provider-backed implementation can be registered without touching callers.
/// </summary>
/// <remarks>
/// AI must NEVER rewrite manuscript content automatically. It may only suggest,
/// explain, highlight, warn, recommend, or generate metadata/TOC/index/glossary
/// drafts that remain user-approved.
/// </remarks>
public interface IAiAssistant
{
    /// <summary>Suggests a marketing description for the book.</summary>
    Task<string> GenerateDescriptionAsync(BookDocument book, CancellationToken ct = default);

    /// <summary>Suggests up to seven KDP keywords.</summary>
    Task<IReadOnlyList<string>> SuggestKeywordsAsync(BookDocument book, CancellationToken ct = default);

    /// <summary>Estimates a readability score (0-100, higher = easier).</summary>
    double ReadabilityScore(BookDocument book);

    /// <summary>Suggests a copyright page draft (user must approve).</summary>
    Task<string> SuggestCopyrightPageAsync(BookDocument book, CancellationToken ct = default)
        => Task.FromResult(
            $"Copyright © {book.Metadata.CopyrightYear ?? DateTime.UtcNow.Year} {book.Metadata.Author}. All rights reserved.");

    /// <summary>Suggests a publishing checklist (user must approve).</summary>
    Task<IReadOnlyList<string>> SuggestPublishingChecklistAsync(BookDocument book, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>(new[]
        {
            "Verify title and author metadata",
            "Confirm trim size and paper stock",
            "Review interior margins and gutter",
            "Check image DPI ≥ template minimum",
            "Validate cover barcode safe area",
            "Run content integrity verification",
            "Export print PDF and cover PDF",
            "Review validation report before upload",
        });

    /// <summary>Suggests TOC titles from existing chapter titles (no rewrite of body).</summary>
    Task<IReadOnlyList<string>> SuggestTocAsync(BookDocument book, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>(
            book.Chapters.Where(c => c.Number > 0).Select(c => c.Title).ToList());

    /// <summary>Suggests index terms by simple frequency (suggestions only).</summary>
    Task<IReadOnlyList<string>> SuggestIndexTermsAsync(BookDocument book, CancellationToken ct = default)
    {
        var words = book.Chapters.SelectMany(c => c.Blocks).OfType<ParagraphBlock>()
            .SelectMany(p => p.PlainText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(w => w.Length >= 6)
            .GroupBy(w => w.TrimEnd('.', ',', ';', ':').ToLowerInvariant())
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(25)
            .ToList();
        return Task.FromResult<IReadOnlyList<string>>(words);
    }

    /// <summary>Suggests glossary terms (suggestions only).</summary>
    Task<IReadOnlyList<string>> SuggestGlossaryTermsAsync(BookDocument book, CancellationToken ct = default)
        => SuggestIndexTermsAsync(book, ct);

    /// <summary>Suggests bibliography lines from hyperlinks (suggestions only).</summary>
    Task<IReadOnlyList<string>> SuggestBibliographyAsync(BookDocument book, CancellationToken ct = default)
    {
        var links = book.Chapters.SelectMany(c => c.Blocks).OfType<ParagraphBlock>()
            .SelectMany(p => p.Runs)
            .Select(r => r.Hyperlink)
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        return Task.FromResult<IReadOnlyList<string>>(links);
    }
}
