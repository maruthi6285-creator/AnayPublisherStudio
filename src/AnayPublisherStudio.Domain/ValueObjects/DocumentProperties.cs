namespace AnayPublisherStudio.Domain.ValueObjects;

/// <summary>
/// Read-only document properties captured from the source manuscript. These are
/// informational only and are never written back to the author's document.
/// </summary>
public sealed record DocumentProperties
{
    /// <summary>Application that created the DOCX, if declared.</summary>
    public string? Application { get; init; }

    /// <summary>Creation timestamp declared in the DOCX, if any.</summary>
    public DateTimeOffset? Created { get; init; }

    /// <summary>Last-modified timestamp declared in the DOCX, if any.</summary>
    public DateTimeOffset? Modified { get; init; }

    /// <summary>Word count as declared by the source, if any.</summary>
    public int? WordCount { get; init; }

    /// <summary>Revision label declared by the source, if any.</summary>
    public string? Revision { get; init; }
}
