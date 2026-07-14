using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Domain.Layout;

namespace AnayPublisherStudio.Domain.Blocks;

/// <summary>An embedded image extracted from the manuscript.</summary>
public sealed class ImageBlock : ContentBlock
{
    /// <inheritdoc/>
    public override BlockType Type => BlockType.Image;

    /// <summary>Raw image bytes (as embedded in the DOCX).</summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>Original content type, e.g. "image/png".</summary>
    public string ContentType { get; set; } = "image/png";

    /// <summary>Pixel width, when known.</summary>
    public int PixelWidth { get; set; }

    /// <summary>Pixel height, when known.</summary>
    public int PixelHeight { get; set; }

    /// <summary>Effective horizontal resolution in dots-per-inch.</summary>
    public double Dpi { get; set; }

    /// <summary>Optional caption text (author content).</summary>
    public string? Caption { get; set; }

    /// <summary>Presentation placement hint (does not alter image bytes or caption).</summary>
    public ImagePlacement Placement { get; set; } = ImagePlacement.Inline;
}
