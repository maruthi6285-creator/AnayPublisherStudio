namespace AnayPublisherStudio.Domain.Layout;

/// <summary>How an image is placed relative to surrounding text (presentation).</summary>
public enum ImagePlacement
{
    /// <summary>Inline with text flow.</summary>
    Inline,
    /// <summary>Floating beside text.</summary>
    Floating,
    /// <summary>Anchored to a content position.</summary>
    Anchored,
    /// <summary>Full-bleed to page edge.</summary>
    FullBleed,
}
