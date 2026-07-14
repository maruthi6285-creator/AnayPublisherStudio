namespace AnayPublisherStudio.Domain.Model;

/// <summary>Author-supplied endnote (content; never rewritten by presentation).</summary>
public sealed class Endnote
{
    /// <summary>Stable endnote identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Author endnote text (immutable content).</summary>
    public string Text { get; set; } = string.Empty;
}
