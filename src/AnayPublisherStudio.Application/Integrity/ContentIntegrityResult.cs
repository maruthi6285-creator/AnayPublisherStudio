namespace AnayPublisherStudio.Application.Integrity;

/// <summary>
/// Outcome of a content-integrity verification. When <see cref="IsIntact"/> is
/// false the publishing run has violated the absolute rule that author content
/// must never be modified, and the pipeline must fail loudly.
/// </summary>
public sealed record ContentIntegrityResult
{
    /// <summary>True when the author content is byte-for-byte unchanged.</summary>
    public bool IsIntact { get; init; }

    /// <summary>Fingerprint captured immediately after parsing.</summary>
    public string ExpectedFingerprint { get; init; } = string.Empty;

    /// <summary>Fingerprint captured after layout/typography.</summary>
    public string ActualFingerprint { get; init; } = string.Empty;
}
