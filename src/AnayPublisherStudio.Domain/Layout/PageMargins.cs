namespace AnayPublisherStudio.Domain.Layout;

/// <summary>
/// Resolved page margins (inches) for a specific page side, including dynamic gutter.
/// Presentation only.
/// </summary>
public sealed record PageMargins(
    double Inside,
    double Outside,
    double Top,
    double Bottom)
{
    /// <summary>Left margin for the given page side.</summary>
    public double Left(PageSide side) => side == PageSide.Recto ? Inside : Outside;

    /// <summary>Right margin for the given page side.</summary>
    public double Right(PageSide side) => side == PageSide.Recto ? Outside : Inside;
}
