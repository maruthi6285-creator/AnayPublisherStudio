using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Domain.ValueObjects;

namespace AnayPublisherStudio.Domain.Blocks;

/// <summary>A body paragraph composed of formatted <see cref="TextRun"/>s.</summary>
public sealed class ParagraphBlock : ContentBlock
{
    /// <inheritdoc/>
    public override BlockType Type => BlockType.Paragraph;

    /// <summary>Ordered inline runs.</summary>
    public List<TextRun> Runs { get; set; } = new();

    /// <summary>Named paragraph style from the source document.</summary>
    public string? StyleName { get; set; }

    /// <summary>Alignment resolved from the style or template.</summary>
    public TextAlignment Alignment { get; set; } = TextAlignment.Justify;

    /// <summary>True when this paragraph is a block quote.</summary>
    public bool IsQuote { get; set; }

    /// <summary>Convenience accessor for the concatenated plain text.</summary>
    public string PlainText => string.Concat(Runs.Select(r => r.Text));
}
