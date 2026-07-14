using AnayPublisherStudio.Domain.ValueObjects;

namespace AnayPublisherStudio.Domain.Layout;

/// <summary>
/// Pure geometry helpers for professional layout (presentation only).
/// Shared by composition and rendering without cross-layer coupling.
/// </summary>
public static class LayoutGeometry
{
    /// <summary>Resolves mirror margins and dynamic gutter from template + page number.</summary>
    public static PageMargins ResolveMargins(PublishingTemplate template, int pageNumber, PageSide side)
    {
        var inside = template.InsideMargin;
        if (template.GutterByPageCount is { Count: > 0 })
        {
            var rule = template.GutterByPageCount
                .OrderBy(g => g.MaxPages)
                .FirstOrDefault(g => pageNumber <= g.MaxPages)
                ?? template.GutterByPageCount.OrderBy(g => g.MaxPages).Last();
            if (rule is not null)
                inside = Math.Max(inside, rule.Inside);
        }

        if (template.MirrorMargins)
            inside = Math.Max(inside, template.OutsideMargin);

        return new PageMargins(inside, template.OutsideMargin, template.TopMargin, template.BottomMargin);
    }

    /// <summary>Text frame calculation from page size and margins.</summary>
    public static TextFrame CalculateTextFrame(PublishingTemplate template, PageMargins margins, PageSide side)
    {
        var left = margins.Left(side);
        var right = margins.Right(side);
        var width = Math.Max(0.5, template.TrimWidth - left - right);
        var height = Math.Max(0.5, template.TrimHeight - margins.Top - margins.Bottom);
        return new TextFrame(left, margins.Top, width, height);
    }

    /// <summary>
    /// Re-resolves gutters for the whole document once final page count is known.
    /// </summary>
    public static void ApplyDynamicGutter(LayoutDocument layout, PublishingTemplate template)
    {
        if (template.GutterByPageCount is not { Count: > 0 }) return;
        var total = layout.PageCount;
        var rule = template.GutterByPageCount.OrderBy(g => g.MaxPages)
            .FirstOrDefault(g => total <= g.MaxPages)
            ?? template.GutterByPageCount.OrderBy(g => g.MaxPages).Last();
        if (rule is null) return;

        for (var i = 0; i < layout.Pages.Count; i++)
        {
            var p = layout.Pages[i];
            var margins = new PageMargins(
                Math.Max(template.InsideMargin, rule.Inside),
                template.OutsideMargin,
                template.TopMargin,
                template.BottomMargin);
            layout.Pages[i] = new ComposedPage
            {
                PageNumber = p.PageNumber,
                Side = p.Side,
                IsBlank = p.IsBlank,
                Margins = margins,
                LiveArea = CalculateTextFrame(template, margins, p.Side),
                ChapterNumber = p.ChapterNumber,
                ChapterTitle = p.ChapterTitle,
                BlockOrders = p.BlockOrders,
                RunningHeader = p.RunningHeader,
                RunningFooter = p.RunningFooter,
            };
        }
    }
}
