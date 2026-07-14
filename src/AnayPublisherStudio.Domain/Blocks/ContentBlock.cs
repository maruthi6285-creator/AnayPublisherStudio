using AnayPublisherStudio.Domain.Enums;

namespace AnayPublisherStudio.Domain.Blocks;

/// <summary>
/// Abstract base for every renderable block inside a chapter. The layout
/// engine walks a flat ordered list of blocks to produce pages.
/// </summary>
public abstract class ContentBlock
{
    /// <summary>Discriminates the concrete block kind.</summary>
    public abstract BlockType Type { get; }

    /// <summary>Zero-based ordinal within the owning chapter.</summary>
    public int Order { get; set; }
}
