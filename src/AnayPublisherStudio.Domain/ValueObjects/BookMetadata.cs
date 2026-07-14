namespace AnayPublisherStudio.Domain.ValueObjects;

/// <summary>
/// Publication metadata describing the book. Drives the copyright page,
/// KDP listing fields, and validation checks.
/// </summary>
public sealed class BookMetadata
{
    /// <summary>Book title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Primary author name.</summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>Publisher / imprint name.</summary>
    public string Publisher { get; set; } = string.Empty;

    /// <summary>ISBN-13 (optional; KDP can assign one).</summary>
    public string? Isbn { get; set; }

    /// <summary>BCP-47 language tag (e.g. "en-US").</summary>
    public string Language { get; set; } = "en-US";

    /// <summary>Primary BISAC / store category.</summary>
    public string? Category { get; set; }

    /// <summary>Store keywords used to improve discoverability.</summary>
    public List<string> Keywords { get; set; } = new();

    /// <summary>Marketing description / blurb.</summary>
    public string? Description { get; set; }

    /// <summary>Copyright year.</summary>
    public int? CopyrightYear { get; set; }

    /// <summary>Edition label (e.g. "First Edition").</summary>
    public string? Edition { get; set; }
}
