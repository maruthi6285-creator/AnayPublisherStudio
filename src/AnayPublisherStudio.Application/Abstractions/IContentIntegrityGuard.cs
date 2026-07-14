using AnayPublisherStudio.Application.Integrity;
using AnayPublisherStudio.Domain.Model;

namespace AnayPublisherStudio.Application.Abstractions;

/// <summary>
/// Enforces the platform's absolute rule: the publishing engine may change
/// presentation but must never modify author content. Callers fingerprint the
/// parsed book, run the presentation pipeline, then verify the fingerprint is
/// unchanged.
/// </summary>
public interface IContentIntegrityGuard
{
    /// <summary>
    /// Computes a stable fingerprint over author content only (chapters, blocks,
    /// runs, tables, footnotes, images). Generated presentation data such as the
    /// table of contents is deliberately excluded.
    /// </summary>
    string ComputeFingerprint(BookDocument book);

    /// <summary>
    /// Re-fingerprints <paramref name="book"/> and compares it with a previously
    /// captured fingerprint.
    /// </summary>
    ContentIntegrityResult Verify(string expectedFingerprint, BookDocument book);
}
