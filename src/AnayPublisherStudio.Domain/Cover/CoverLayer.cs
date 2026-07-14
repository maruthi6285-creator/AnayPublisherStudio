namespace AnayPublisherStudio.Domain.Cover;

/// <summary>A single layer on the wraparound cover (presentation).</summary>
public sealed class CoverLayer
{
    /// <summary>Layer name.</summary>
    public string Name { get; set; } = "Layer";

    /// <summary>Layer kind: text, image, shape, barcode, guide.</summary>
    public string Kind { get; set; } = "text";

    /// <summary>X position in inches from the left of the overall cover.</summary>
    public double XInches { get; set; }

    /// <summary>Y position in inches from the top of the overall cover.</summary>
    public double YInches { get; set; }

    /// <summary>Width in inches.</summary>
    public double WidthInches { get; set; }

    /// <summary>Height in inches.</summary>
    public double HeightInches { get; set; }

    /// <summary>Z-order (higher draws later).</summary>
    public int ZIndex { get; set; }

    /// <summary>Optional image path for image layers.</summary>
    public string? ImagePath { get; set; }

    /// <summary>Optional text for text layers (metadata/presentation, not manuscript body).</summary>
    public string? Text { get; set; }

    /// <summary>Font family for text layers.</summary>
    public string FontFamily { get; set; } = "Georgia";

    /// <summary>Font size in points.</summary>
    public double FontSizePoints { get; set; } = 24;

    /// <summary>Whether the layer is locked against accidental edits.</summary>
    public bool Locked { get; set; }

    /// <summary>Whether the layer is visible.</summary>
    public bool Visible { get; set; } = true;
}
