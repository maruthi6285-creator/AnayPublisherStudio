namespace AnayPublisherStudio.Domain.Model;

/// <summary>A single table-of-contents entry with a resolved page number.</summary>
public sealed class TocEntry
{
    /// <summary>Entry title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Nesting depth (1 = chapter, 2 = section, ...).</summary>
    public int Level { get; set; } = 1;

    /// <summary>Resolved page number, populated by the layout engine.</summary>
    public int PageNumber { get; set; }
}
