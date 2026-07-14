namespace AnayPublisherStudio.Domain.Layout;

/// <summary>
/// Professional page-composition rules. Presentation only — never alters author text.
/// </summary>
public sealed record CompositionRules
{
    /// <summary>Minimum lines allowed alone at the bottom of a page (widow control).</summary>
    public int WidowLines { get; init; } = 2;

    /// <summary>Minimum lines allowed alone at the top of a page (orphan control).</summary>
    public int OrphanLines { get; init; } = 2;

    /// <summary>True when chapters always start on a recto (right-hand) page.</summary>
    public bool ChaptersStartOnRecto { get; init; } = true;

    /// <summary>True when a blank verso is inserted so chapters open on recto.</summary>
    public bool InsertBlankVersoBeforeChapter { get; init; } = true;

    /// <summary>Keep-with-next for headings (lines of following body to keep).</summary>
    public int KeepWithNextLines { get; init; } = 2;

    /// <summary>True when multi-line headings must not break across pages.</summary>
    public bool KeepHeadingsTogether { get; init; } = true;

    /// <summary>Baseline grid step in points (0 = off).</summary>
    public double BaselineGridPoints { get; init; } = 0;

    /// <summary>Page balancing tolerance in points for facing pages.</summary>
    public double PageBalanceTolerancePoints { get; init; } = 12;

    /// <summary>Running header/footer presentation.</summary>
    public RunningHeaderSpec RunningMatter { get; init; } = new();
}
