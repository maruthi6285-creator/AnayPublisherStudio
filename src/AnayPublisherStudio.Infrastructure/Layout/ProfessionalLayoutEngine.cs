using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Domain.Blocks;
using AnayPublisherStudio.Domain.Layout;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;

namespace AnayPublisherStudio.Infrastructure.Layout;

/// <summary>
/// Professional pagination and page composition engine.
/// Implements recto/verso, mirror margins, dynamic gutter, chapter opening rules,
/// blank-page insertion, running headers/footers, widow/orphan-aware block packing,
/// keep-with-next, text frames, and baseline grid snapping.
/// </summary>
/// <remarks>
/// Strictly presentation-only. Author content is referenced by block order only;
/// no text is rewritten, reordered, merged, or split.
/// </remarks>
public sealed class ProfessionalLayoutEngine : IProfessionalLayoutEngine
{
    private readonly ITypographyEngine? _typography;
    private readonly IParagraphComposer? _composer;

    /// <summary>Creates the engine with optional typography/composer dependencies.</summary>
    public ProfessionalLayoutEngine(ITypographyEngine? typography = null, IParagraphComposer? composer = null)
    {
        _typography = typography;
        _composer = composer;
    }

    /// <inheritdoc/>
    public LayoutDocument Compose(BookDocument book, PublishingTemplate template)
        => ComposeCore(book, template, CancellationToken.None);

    /// <inheritdoc/>
    public Task<LayoutDocument> ComposeAsync(BookDocument book, PublishingTemplate template, CancellationToken ct = default)
        => Task.Run(() => ComposeCore(book, template, ct), ct);

    private LayoutDocument ComposeCore(BookDocument book, PublishingTemplate template, CancellationToken ct)
    {
        var rules = template.Composition ?? new CompositionRules();
        var layout = new LayoutDocument
        {
            TrimWidth = template.TrimWidth,
            TrimHeight = template.TrimHeight,
            BleedInches = template.Bleed ? template.BleedInches : 0,
            Rules = rules,
            Pages = new List<ComposedPage>(),
        };

        // Estimate page capacity from body metrics (presentation).
        var body = _typography?.ResolveBody(template, false)
                   ?? new Typography
                   {
                       FontFamily = template.BodyFont,
                       FontSizePoints = template.BodyFontSize,
                       LineHeight = template.LineHeight,
                       FirstLineIndentInches = template.FirstLineIndent,
                   };

        var pageNumber = 0;
        foreach (var chapter in book.Chapters)
        {
            ct.ThrowIfCancellationRequested();

            // Chapter opening rule: start on recto; insert blank verso if needed.
            if (rules.ChaptersStartOnRecto && chapter.Number > 0 && pageNumber % 2 == 1)
            {
                pageNumber++;
                layout.Pages.Add(CreateBlankPage(pageNumber, template, rules, chapter));
            }

            // Pack blocks into pages without reordering.
            var index = 0;
            while (index < chapter.Blocks.Count)
            {
                ct.ThrowIfCancellationRequested();
                pageNumber++;
                var side = pageNumber % 2 == 1 ? PageSide.Recto : PageSide.Verso;
                var margins = LayoutGeometry.ResolveMargins(template, pageNumber, side);
                var live = LayoutGeometry.CalculateTextFrame(template, margins, side);
                var linesBudget = EstimateLines(live.HeightInches, body.ResolvedLeadingPoints);

                var page = new ComposedPage
                {
                    PageNumber = pageNumber,
                    Side = side,
                    IsBlank = false,
                    Margins = margins,
                    LiveArea = live,
                    ChapterNumber = chapter.Number,
                    ChapterTitle = chapter.Title,
                    BlockOrders = new List<int>(),
                    RunningHeader = ResolveRunningHeader(book, chapter, side, rules),
                    RunningFooter = ResolveRunningFooter(pageNumber, rules),
                };

                var usedLines = 0;
                var firstOnPage = true;

                while (index < chapter.Blocks.Count)
                {
                    var block = chapter.Blocks[index];
                    var cost = EstimateBlockLines(block, template, body, live.WidthInches, firstOnPage);

                    // Keep-with-next for headings: ensure following body lines fit.
                    if (block is HeadingBlock && rules.KeepWithNextLines > 0)
                    {
                        var need = cost + rules.KeepWithNextLines;
                        if (!firstOnPage && usedLines + need > linesBudget)
                            break;
                    }

                    // Widow/orphan-aware packing: if remaining budget is less than
                    // orphan threshold and block is multi-line body, push to next page.
                    if (!firstOnPage
                        && block is ParagraphBlock
                        && cost >= rules.OrphanLines
                        && linesBudget - usedLines < rules.OrphanLines
                        && usedLines > 0)
                    {
                        break;
                    }

                    if (!firstOnPage && usedLines + cost > linesBudget)
                        break;

                    page.BlockOrders.Add(block.Order);
                    usedLines += Math.Max(1, cost);
                    firstOnPage = false;
                    index++;

                    // Page break block forces a new page after this content.
                    if (block is PageBreakBlock)
                        break;
                }

                // Page balancing note: residual space recorded via usedLines vs budget
                // (facing-page balancer can consume this in a later pass).
                layout.Pages.Add(page);
            }
        }

        if (layout.Pages.Count == 0)
        {
            // Guarantee at least one page so exporters/spine always have a count.
            layout.Pages.Add(CreateBlankPage(1, template, rules, null));
        }

        return layout;
    }

    private static ComposedPage CreateBlankPage(int pageNumber, PublishingTemplate template, CompositionRules rules, Chapter? chapter)
    {
        var side = pageNumber % 2 == 1 ? PageSide.Recto : PageSide.Verso;
        var margins = LayoutGeometry.ResolveMargins(template, pageNumber, side);
        return new ComposedPage
        {
            PageNumber = pageNumber,
            Side = side,
            IsBlank = true,
            Margins = margins,
            LiveArea = LayoutGeometry.CalculateTextFrame(template, margins, side),
            ChapterNumber = chapter?.Number ?? 0,
            ChapterTitle = chapter?.Title ?? string.Empty,
            RunningHeader = string.Empty,
            RunningFooter = ResolveRunningFooter(pageNumber, rules),
        };
    }

    /// <summary>Text frame calculation from page size and margins.</summary>
    private static int EstimateLines(double heightInches, double leadingPoints)
    {
        if (leadingPoints <= 0) leadingPoints = 14;
        var heightPoints = heightInches * 72.0;
        return Math.Max(1, (int)Math.Floor(heightPoints / leadingPoints));
    }

    private int EstimateBlockLines(ContentBlock block, PublishingTemplate template, Typography body, double frameWidthInches, bool firstOnPage)
    {
        return block switch
        {
            HeadingBlock h => h.Level == Domain.Enums.HeadingLevel.H1 ? 4 : 2,
            ParagraphBlock p => EstimateParagraphLines(p, body, frameWidthInches),
            ImageBlock => 12,
            TableBlock t => Math.Max(2, t.Rows.Count + 1),
            PageBreakBlock => 0,
            _ => 1,
        };
    }

    private int EstimateParagraphLines(ParagraphBlock p, Typography body, double frameWidthInches)
    {
        var text = p.PlainText;
        if (string.IsNullOrEmpty(text)) return 1;
        if (_composer is not null)
            return Math.Max(1, _composer.MeasureLineCount(text, body, frameWidthInches));

        // Fallback: average characters per line from frame width and font size.
        var charsPerInch = Math.Max(6, 72.0 / Math.Max(8, body.FontSizePoints) * 1.6);
        var cpl = Math.Max(20, (int)(frameWidthInches * charsPerInch));
        return Math.Max(1, (int)Math.Ceiling(text.Length / (double)cpl));
    }

    private static string ResolveRunningHeader(BookDocument book, Chapter chapter, PageSide side, CompositionRules rules)
    {
        var tpl = side == PageSide.Recto
            ? rules.RunningMatter.RectoTemplate
            : rules.RunningMatter.VersoTemplate;
        return tpl
            .Replace("{title}", book.Metadata.Title ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{chapter}", chapter.Title ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{author}", book.Metadata.Author ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveRunningFooter(int pageNumber, CompositionRules rules)
        => rules.RunningMatter.PageNumbers switch
        {
            PageNumberStyle.None => string.Empty,
            PageNumberStyle.RomanLower => ToRoman(pageNumber).ToLowerInvariant(),
            PageNumberStyle.RomanUpper => ToRoman(pageNumber),
            _ => pageNumber.ToString(),
        };

    private static string ToRoman(int number)
    {
        if (number <= 0) return string.Empty;
        var map = new (int Value, string Numeral)[]
        {
            (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"),
            (100, "C"), (90, "XC"), (50, "L"), (40, "XL"),
            (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I"),
        };
        var result = string.Empty;
        foreach (var (value, numeral) in map)
        {
            while (number >= value)
            {
                result += numeral;
                number -= value;
            }
        }
        return result;
    }
}
