namespace AnayPublisherStudio.Domain.Layout;

/// <summary>
/// A single composed page produced by the professional layout engine.
/// Holds presentation geometry only; author content is referenced by identity, never rewritten.
/// </summary>
public sealed class ComposedPage
{
    /// <summary>1-based page number in the interior.</summary>
    public int PageNumber { get; init; }

    /// <summary>Recto or verso.</summary>
    public PageSide Side { get; init; }

    /// <summary>True when this is an intentionally blank inserted page.</summary>
    public bool IsBlank { get; init; }

    /// <summary>Resolved margins for this page.</summary>
    public PageMargins Margins { get; init; } = new(0.5, 0.5, 0.75, 0.75);

    /// <summary>Text frame (live area) for body content.</summary>
    public TextFrame LiveArea { get; init; } = new(0, 0, 0, 0);

    /// <summary>Chapter number this page belongs to (0 = front matter).</summary>
    public int ChapterNumber { get; init; }

    /// <summary>Chapter title for running headers (presentation reference only).</summary>
    public string ChapterTitle { get; init; } = string.Empty;

    /// <summary>Block order indices placed on this page (references into chapter.Blocks).</summary>
    public List<int> BlockOrders { get; init; } = new();

    /// <summary>Resolved running header text for this page (presentation).</summary>
    public string RunningHeader { get; init; } = string.Empty;

    /// <summary>Resolved running footer text for this page (presentation).</summary>
    public string RunningFooter { get; init; } = string.Empty;
}
