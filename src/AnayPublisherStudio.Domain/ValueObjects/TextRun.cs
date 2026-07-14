namespace AnayPublisherStudio.Domain.ValueObjects;

/// <summary>
/// An inline span of text carrying character-level formatting.
/// A <see cref="Blocks.ParagraphBlock"/> is composed of one or more runs.
/// </summary>
public sealed class TextRun
{
    /// <summary>The literal text content of the run.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Whether the run is bold.</summary>
    public bool Bold { get; set; }

    /// <summary>Whether the run is italic.</summary>
    public bool Italic { get; set; }

    /// <summary>Whether the run is underlined.</summary>
    public bool Underline { get; set; }

    /// <summary>Optional hyperlink target; null when the run is not a link.</summary>
    public string? Hyperlink { get; set; }

    /// <summary>Optional footnote reference identifier attached to this run.</summary>
    public string? FootnoteRef { get; set; }
}
