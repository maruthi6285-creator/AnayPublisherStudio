using AnayPublisherStudio.Domain.ValueObjects;

namespace AnayPublisherStudio.Domain.Model;

/// <summary>
/// The complete Book Object Model produced by the document parser and consumed
/// by the template, layout, rendering, validation, and export engines.
/// </summary>
public sealed class BookDocument
{
    /// <summary>Publication metadata.</summary>
    public BookMetadata Metadata { get; set; } = new();

    /// <summary>Ordered chapters.</summary>
    public List<Chapter> Chapters { get; set; } = new();

    /// <summary>Footnotes referenced across the manuscript.</summary>
    public List<Footnote> Footnotes { get; set; } = new();

    /// <summary>Endnotes referenced across the manuscript (author content).</summary>
    public List<Endnote> Endnotes { get; set; } = new();

    /// <summary>Generated table of contents (populated after layout).</summary>
    public List<TocEntry> TableOfContents { get; set; } = new();

    /// <summary>
    /// Read-only properties captured from the source document (informational).
    /// Not part of the author content fingerprint.
    /// </summary>
    public DocumentProperties? Properties { get; set; }

    /// <summary>Total number of content blocks across all chapters.</summary>
    public int TotalBlocks => Chapters.Sum(c => c.Blocks.Count);
}
