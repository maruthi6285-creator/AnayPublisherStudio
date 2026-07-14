using AnayPublisherStudio.Domain.Blocks;

namespace AnayPublisherStudio.Domain.Model;

/// <summary>
/// A chapter: a titled, ordered sequence of content blocks. Chapters begin on
/// a new (typically recto) page in the print layout.
/// </summary>
public sealed class Chapter
{
    /// <summary>Chapter title used for the running header and TOC.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>One-based chapter number.</summary>
    public int Number { get; set; }

    /// <summary>Ordered renderable blocks belonging to the chapter.</summary>
    public List<ContentBlock> Blocks { get; set; } = new();
}
