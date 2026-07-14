namespace AnayPublisherStudio.Domain.Layout;

/// <summary>Running header/footer presentation specification.</summary>
public sealed record RunningHeaderSpec
{
    /// <summary>Odd-page (recto) header text template tokens: {title}, {chapter}, {author}.</summary>
    public string RectoTemplate { get; init; } = "{chapter}";

    /// <summary>Even-page (verso) header text template.</summary>
    public string VersoTemplate { get; init; } = "{title}";

    /// <summary>Footer page-number style.</summary>
    public PageNumberStyle PageNumbers { get; init; } = PageNumberStyle.Arabic;

    /// <summary>Font size in points for running matter.</summary>
    public double FontSizePoints { get; init; } = 9;
}
