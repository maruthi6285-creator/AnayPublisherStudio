using AnayPublisherStudio.Domain.Enums;

namespace AnayPublisherStudio.Domain.Blocks;

/// <summary>An explicit page break carried over from the source document.</summary>
public sealed class PageBreakBlock : ContentBlock
{
    /// <inheritdoc/>
    public override BlockType Type => BlockType.PageBreak;
}
