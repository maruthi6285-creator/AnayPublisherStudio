using AnayPublisherStudio.Domain.Enums;

namespace AnayPublisherStudio.Domain.Blocks;

/// <summary>A heading detected from Word heading styles or outline levels.</summary>
public sealed class HeadingBlock : ContentBlock
{
    /// <inheritdoc/>
    public override BlockType Type => BlockType.Heading;

    /// <summary>Heading level (H1..H6).</summary>
    public HeadingLevel Level { get; set; } = HeadingLevel.H1;

    /// <summary>Heading text.</summary>
    public string Text { get; set; } = string.Empty;
}
