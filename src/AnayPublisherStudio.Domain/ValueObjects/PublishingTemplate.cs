using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Domain.Layout;

namespace AnayPublisherStudio.Domain.ValueObjects;

/// <summary>
/// A data-driven publishing target. Loaded from a template folder
/// (<c>template.json</c> + assets) so that platform-specific values such as
/// KDP trim sizes and spine widths are never hard-coded in the engines.
/// </summary>
public sealed class PublishingTemplate
{
    /// <summary>Stable identifier, e.g. "amazon-paperback-6x9".</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable name, e.g. "Amazon KDP".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Target platform label (Amazon, IngramSpark, Lulu, ...).</summary>
    public string Platform { get; set; } = "Amazon";

    /// <summary>Trim width in inches.</summary>
    public double TrimWidth { get; set; } = 6;

    /// <summary>Trim height in inches.</summary>
    public double TrimHeight { get; set; } = 9;

    /// <summary>Overall cover width (back + spine + front + bleed) in inches.</summary>
    public double OverallWidth { get; set; } = 12.486;

    /// <summary>Overall cover height in inches.</summary>
    public double OverallHeight { get; set; } = 9.250;

    /// <summary>Inside (gutter) margin in inches.</summary>
    public double InsideMargin { get; set; } = 0.6;

    /// <summary>Outside margin in inches.</summary>
    public double OutsideMargin { get; set; } = 0.5;

    /// <summary>Top margin in inches.</summary>
    public double TopMargin { get; set; } = 0.75;

    /// <summary>Bottom margin in inches.</summary>
    public double BottomMargin { get; set; } = 0.75;

    /// <summary>Whether interior pages bleed.</summary>
    public bool Bleed { get; set; }

    /// <summary>Bleed amount in inches when <see cref="Bleed"/> is true.</summary>
    public double BleedInches { get; set; } = 0.125;

    /// <summary>Whether interior margins mirror on odd/even pages.</summary>
    public bool MirrorMargins { get; set; } = true;

    /// <summary>Spine width in inches (depends on page count and paper).</summary>
    public double SpineWidth { get; set; } = 0.236;

    /// <summary>Barcode safe-area width in inches.</summary>
    public double BarcodeWidth { get; set; } = 2.0;

    /// <summary>Barcode safe-area height in inches.</summary>
    public double BarcodeHeight { get; set; } = 1.2;

    /// <summary>Paper stock.</summary>
    public PaperType Paper { get; set; } = PaperType.White;

    /// <summary>Interior color mode.</summary>
    public ColorMode Color { get; set; } = ColorMode.BlackWhite;

    /// <summary>Body font family.</summary>
    public string BodyFont { get; set; } = "Georgia";

    /// <summary>Heading font family.</summary>
    public string HeadingFont { get; set; } = "Georgia";

    /// <summary>Body font size in points.</summary>
    public double BodyFontSize { get; set; } = 11;

    /// <summary>Body line height multiple.</summary>
    public double LineHeight { get; set; } = 1.35;

    /// <summary>First-line indent in inches.</summary>
    public double FirstLineIndent { get; set; } = 0.25;

    /// <summary>Minimum acceptable image resolution in DPI (validation).</summary>
    public double MinImageDpi { get; set; } = 300;

    /// <summary>Professional composition rules (widow/orphan, recto starts, etc.).</summary>
    public CompositionRules Composition { get; set; } = new();

    /// <summary>Gutter table keyed by max page count (from margins.json).</summary>
    public List<GutterRule> GutterByPageCount { get; set; } = new();

    /// <summary>Safe-zone inset from trim for cover design (inches).</summary>
    public double CoverSafeZoneInches { get; set; } = 0.25;

    /// <summary>Optional template package root path (set by provider).</summary>
    public string? PackagePath { get; set; }
}

/// <summary>Dynamic gutter rule: inside margin for page counts up to MaxPages.</summary>
public sealed class GutterRule
{
    /// <summary>Upper page-count bound (inclusive).</summary>
    public int MaxPages { get; set; }

    /// <summary>Required inside/gutter margin in inches.</summary>
    public double Inside { get; set; }
}
