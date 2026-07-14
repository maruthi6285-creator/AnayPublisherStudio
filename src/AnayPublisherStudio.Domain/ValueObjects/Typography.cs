namespace AnayPublisherStudio.Domain.ValueObjects;

/// <summary>
/// Resolved, presentation-only typography settings for a run of content.
/// Produced by the typography engine from the active template; it never carries
/// or alters author text. All values describe *how* content is presented, never
/// *what* the content is.
/// </summary>
/// <remarks>
/// This type is immutable: engines compose new instances via <c>with</c> rather
/// than mutating a shared instance, keeping the layout pass side-effect free.
/// </remarks>
public sealed record Typography
{
    /// <summary>Font family name (e.g. "Georgia").</summary>
    public string FontFamily { get; init; } = "Georgia";

    /// <summary>Font size in points.</summary>
    public double FontSizePoints { get; init; } = 11;

    /// <summary>Line-height (leading) as a multiple of the font size.</summary>
    public double LineHeight { get; init; } = 1.35;

    /// <summary>Letter tracking in ems (0 = none). Positive loosens, negative tightens.</summary>
    public double TrackingEm { get; init; }

    /// <summary>First-line indent in inches (0 = flush).</summary>
    public double FirstLineIndentInches { get; init; }

    /// <summary>Whether OpenType standard ligatures are enabled.</summary>
    public bool Ligatures { get; init; } = true;

    /// <summary>Whether kerning is applied.</summary>
    public bool Kerning { get; init; } = true;

    /// <summary>Whether the text is rendered in small caps.</summary>
    public bool SmallCaps { get; init; }

    /// <summary>True small caps (OpenType smcp) rather than scaled capitals.</summary>
    public bool TrueSmallCaps { get; init; }

    /// <summary>Number of lines a drop cap spans (0 = no drop cap).</summary>
    public int DropCapLines { get; init; }

    /// <summary>Whether optical margin alignment (hanging punctuation) is enabled.</summary>
    public bool OpticalAlignment { get; init; }

    /// <summary>Whether hanging punctuation is enabled.</summary>
    public bool HangingPunctuation { get; init; }

    /// <summary>Whether the run should be bold (from typography, not content).</summary>
    public bool Bold { get; init; }

    /// <summary>Word spacing factor (1.0 = default).</summary>
    public double WordSpacing { get; init; } = 1.0;

    /// <summary>Minimum word spacing for justification (factor).</summary>
    public double MinWordSpacing { get; init; } = 0.8;

    /// <summary>Maximum word spacing for justification (factor).</summary>
    public double MaxWordSpacing { get; init; } = 1.33;

    /// <summary>Whether hyphenation is allowed for this role.</summary>
    public bool Hyphenation { get; init; } = true;

    /// <summary>BCP-47 language for language-aware typography.</summary>
    public string Language { get; init; } = "en-US";

    /// <summary>Optional embedded font file path (presentation asset).</summary>
    public string? EmbeddedFontPath { get; init; }

    /// <summary>Fallback font families when glyphs are missing.</summary>
    public IReadOnlyList<string> FontFallback { get; init; } = Array.Empty<string>();

    /// <summary>Leading in points (absolute). 0 means derive from LineHeight * FontSize.</summary>
    public double LeadingPoints { get; init; }

    /// <summary>Resolved leading in points.</summary>
    public double ResolvedLeadingPoints =>
        LeadingPoints > 0 ? LeadingPoints : FontSizePoints * LineHeight;
}
