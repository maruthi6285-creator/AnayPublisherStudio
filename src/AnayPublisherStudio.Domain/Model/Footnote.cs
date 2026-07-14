namespace AnayPublisherStudio.Domain.Model;

/// <summary>A footnote referenced from the body text.</summary>
public sealed class Footnote
{
    /// <summary>Stable identifier referenced by <c>TextRun.FootnoteRef</c>.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Footnote body text.</summary>
    public string Text { get; set; } = string.Empty;
}
