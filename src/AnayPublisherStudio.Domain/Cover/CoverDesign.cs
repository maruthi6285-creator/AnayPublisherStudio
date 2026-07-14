namespace AnayPublisherStudio.Domain.Cover;

/// <summary>
/// Complete wraparound cover design model (front, spine, back, barcode, layers).
/// Presentation only — never mutates manuscript content.
/// </summary>
public sealed class CoverDesign
{
    /// <summary>Overall width in inches (back + spine + front + bleed).</summary>
    public double OverallWidth { get; set; }

    /// <summary>Overall height in inches.</summary>
    public double OverallHeight { get; set; }

    /// <summary>Trim width in inches.</summary>
    public double TrimWidth { get; set; }

    /// <summary>Trim height in inches.</summary>
    public double TrimHeight { get; set; }

    /// <summary>Spine width in inches.</summary>
    public double SpineWidth { get; set; }

    /// <summary>Bleed in inches on each outer edge.</summary>
    public double BleedInches { get; set; } = 0.125;

    /// <summary>Safe-zone inset from trim in inches.</summary>
    public double SafeZoneInches { get; set; } = 0.25;

    /// <summary>Barcode reserved width in inches.</summary>
    public double BarcodeWidth { get; set; } = 2.0;

    /// <summary>Barcode reserved height in inches.</summary>
    public double BarcodeHeight { get; set; } = 1.2;

    /// <summary>Ordered cover layers.</summary>
    public List<CoverLayer> Layers { get; set; } = new();

    /// <summary>X origin of the spine (inches from left of overall cover).</summary>
    public double SpineX => (OverallWidth - SpineWidth) / 2.0;

    /// <summary>X origin of the front cover (inches from left).</summary>
    public double FrontX => SpineX + SpineWidth;
}
