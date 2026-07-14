using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Application.Configuration;
using AnayPublisherStudio.Application.Validation;
using AnayPublisherStudio.Domain.Blocks;
using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace AnayPublisherStudio.Validation;

/// <summary>
/// Professional preflight validation engine (Adobe Preflight–class checks).
/// Runs platform-agnostic and publisher-profile checks over the book model,
/// template, and page count. Each rule is independently testable.
/// </summary>
public sealed class KdpValidationEngine : IValidationEngine
{
    private readonly ValidationSettings _settings;

    /// <summary>Creates a KdpValidationEngine with optional configuration.</summary>
    public KdpValidationEngine(IOptions<ValidationSettings>? settings = null)
    {
        _settings = settings?.Value ?? new ValidationSettings();
    }

    /// <inheritdoc/>
    public ValidationReport Validate(BookDocument book, PublishingTemplate template, int pageCount)
    {
        var report = new ValidationReport();
        CheckMetadata(book, report);
        CheckPageCount(pageCount, report);
        CheckMargins(template, pageCount, report);
        CheckTrimBleedLiveArea(template, report);
        CheckImages(book, template, report);
        CheckStructure(book, report);
        CheckBlankPages(pageCount, report);
        CheckFonts(template, report);
        CheckHyperlinks(book, report);
        CheckToc(book, report);
        CheckSpineAndBarcode(template, pageCount, report);
        CheckPublisherProfile(template, pageCount, report);
        return report;
    }

    private void CheckMetadata(BookDocument book, ValidationReport r)
    {
        if (string.IsNullOrWhiteSpace(book.Metadata.Title))
            r.Add("Metadata.Title", ValidationSeverity.Error, "Book title is required for KDP.");
        if (string.IsNullOrWhiteSpace(book.Metadata.Author))
            r.Add("Metadata.Author", ValidationSeverity.Warning, "Author name is empty.");
        if (book.Metadata.Keywords.Count > 7)
            r.Add("Metadata.Keywords", ValidationSeverity.Warning, "KDP accepts at most 7 keywords.");
        if (!string.IsNullOrEmpty(book.Metadata.Isbn) && book.Metadata.Isbn.Replace("-", "").Length is not (10 or 13))
            r.Add("Metadata.Isbn", ValidationSeverity.Warning, "ISBN should be 10 or 13 digits.");
    }

    private void CheckPageCount(int pages, ValidationReport r)
    {
        if (pages < _settings.KdpMinPages)
            r.Add("PageCount", ValidationSeverity.Error, $"Interior has {pages} pages; KDP requires at least {_settings.KdpMinPages}.");
        else if (pages > _settings.KdpMaxPages)
            r.Add("PageCount", ValidationSeverity.Error, $"Interior has {pages} pages; exceeds KDP max of {_settings.KdpMaxPages}.");
        else
            r.Add("PageCount", ValidationSeverity.Info, $"Interior page count {pages} is within KDP limits.");
    }

    private static void CheckMargins(PublishingTemplate t, int pageCount, ValidationReport r)
    {
        var minInside = 0.375;
        if (t.GutterByPageCount is { Count: > 0 })
        {
            var rule = t.GutterByPageCount.OrderBy(g => g.MaxPages)
                .FirstOrDefault(g => pageCount <= g.MaxPages)
                ?? t.GutterByPageCount.OrderBy(g => g.MaxPages).Last();
            if (rule is not null) minInside = rule.Inside;
        }

        if (t.InsideMargin < minInside)
            r.Add("Margins.Inside", ValidationSeverity.Error,
                $"Inside/gutter margin {t.InsideMargin}in is below the {minInside}in minimum for {pageCount} pages.");
        if (t.OutsideMargin < 0.25)
            r.Add("Margins.Outside", ValidationSeverity.Warning,
                $"Outside margin {t.OutsideMargin}in is below the recommended 0.25in.");
        if (t.TopMargin < 0.25)
            r.Add("Margins.Top", ValidationSeverity.Warning, $"Top margin {t.TopMargin}in is below 0.25in.");
        if (t.BottomMargin < 0.25)
            r.Add("Margins.Bottom", ValidationSeverity.Warning, $"Bottom margin {t.BottomMargin}in is below 0.25in.");
    }

    private static void CheckTrimBleedLiveArea(PublishingTemplate t, ValidationReport r)
    {
        if (t.TrimWidth <= 0 || t.TrimHeight <= 0)
            r.Add("Trim", ValidationSeverity.Error, "Trim size must be positive.");
        if (t.Bleed && t.BleedInches < 0.125)
            r.Add("Bleed", ValidationSeverity.Warning, "Bleed is enabled but less than 0.125in.");
        var liveW = t.TrimWidth - t.InsideMargin - t.OutsideMargin;
        var liveH = t.TrimHeight - t.TopMargin - t.BottomMargin;
        if (liveW < 2 || liveH < 2)
            r.Add("LiveArea", ValidationSeverity.Error,
                $"Live area {liveW:0.###}×{liveH:0.###}in is too small for readable text.");
        else
            r.Add("LiveArea", ValidationSeverity.Info, $"Live area {liveW:0.###}×{liveH:0.###}in.");
    }

    private static void CheckImages(BookDocument book, PublishingTemplate t, ValidationReport r)
    {
        var images = book.Chapters.SelectMany(c => c.Blocks).OfType<ImageBlock>().ToList();
        foreach (var img in images)
        {
            if (img.Dpi > 0 && img.Dpi < t.MinImageDpi)
                r.Add("ImageDpi", ValidationSeverity.Error,
                    $"An image is {img.Dpi:0} DPI at print size; requires {t.MinImageDpi:0} DPI.");
            if (img.Data.Length == 0)
                r.Add("Images", ValidationSeverity.Warning, "An image block has no embedded data.");
            // Color space: without decoding every image, flag unknown content types.
            if (!string.IsNullOrEmpty(img.ContentType)
                && !img.ContentType.Contains("jpeg", StringComparison.OrdinalIgnoreCase)
                && !img.ContentType.Contains("png", StringComparison.OrdinalIgnoreCase)
                && !img.ContentType.Contains("tiff", StringComparison.OrdinalIgnoreCase))
            {
                r.Add("ImageColorSpace", ValidationSeverity.Warning,
                    $"Image content type '{img.ContentType}' may not be print-safe.");
            }
        }
        if (images.Count == 0)
            r.Add("Images", ValidationSeverity.Info, "No embedded images detected.");
    }

    private static void CheckStructure(BookDocument book, ValidationReport r)
    {
        if (book.Chapters.All(c => c.Number == 0))
            r.Add("Structure.Chapters", ValidationSeverity.Warning,
                "No Heading-1 chapters detected; the whole manuscript is treated as front matter.");
        if (book.TotalBlocks == 0)
            r.Add("Structure.Content", ValidationSeverity.Error, "Manuscript contains no readable content.");
    }

    private static void CheckBlankPages(int pageCount, ValidationReport r)
    {
        // Informational: odd page counts are normal; even preferred for some printers.
        if (pageCount > 0 && pageCount % 2 != 0)
            r.Add("BlankPages", ValidationSeverity.Info,
                "Page count is odd; a trailing blank may be inserted by the printer.");
    }

    private static void CheckFonts(PublishingTemplate t, ValidationReport r)
    {
        if (string.IsNullOrWhiteSpace(t.BodyFont))
            r.Add("Fonts.Body", ValidationSeverity.Error, "Body font is not specified.");
        if (string.IsNullOrWhiteSpace(t.HeadingFont))
            r.Add("Fonts.Heading", ValidationSeverity.Warning, "Heading font is not specified.");
        r.Add("Fonts.Embedded", ValidationSeverity.Info,
            "Ensure fonts are embedded/subsetted in the final print PDF for compliance.");
    }

    private static void CheckHyperlinks(BookDocument book, ValidationReport r)
    {
        var links = book.Chapters.SelectMany(c => c.Blocks).OfType<ParagraphBlock>()
            .SelectMany(p => p.Runs)
            .Select(run => run.Hyperlink)
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .ToList();
        foreach (var link in links)
        {
            if (!Uri.TryCreate(link, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                r.Add("Hyperlinks", ValidationSeverity.Warning, $"Hyperlink may be invalid: {link}");
            }
        }
        if (links.Count == 0)
            r.Add("Hyperlinks", ValidationSeverity.Info, "No hyperlinks detected.");
    }

    private static void CheckToc(BookDocument book, ValidationReport r)
    {
        if (book.TableOfContents.Count == 0 && book.Chapters.Any(c => c.Number > 0))
            r.Add("TOC", ValidationSeverity.Info, "TOC not yet resolved; layout will generate presentation TOC.");
        else if (book.TableOfContents.Any(e => e.PageNumber <= 0))
            r.Add("TOC", ValidationSeverity.Warning, "TOC contains entries without resolved page numbers.");
    }

    private static void CheckSpineAndBarcode(PublishingTemplate t, int pageCount, ValidationReport r)
    {
        if (t.BarcodeWidth < 1.5 || t.BarcodeHeight < 1.0)
            r.Add("Barcode", ValidationSeverity.Warning, "Barcode reservation is smaller than typical requirements.");
        if (pageCount >= 79 && t.SpineWidth < 0.06)
            r.Add("Spine", ValidationSeverity.Warning, "Spine width may be too small for the page count.");
        if (t.OverallWidth < t.TrimWidth * 2)
            r.Add("Cover.Overall", ValidationSeverity.Error, "Overall cover width is less than two trim widths.");
    }

    private void CheckPublisherProfile(PublishingTemplate t, int pageCount, ValidationReport r)
    {
        // Profile-specific soft checks driven by template data (no hard-coded platforms).
        var platform = t.Platform ?? string.Empty;
        if (_settings.EnableIngramSparkChecks
            && platform.Contains("Ingram", StringComparison.OrdinalIgnoreCase)
            && pageCount < _settings.IngramSparkMinPages)
        {
            r.Add("Ingram.PageCount", ValidationSeverity.Error,
                $"IngramSpark requires at least {_settings.IngramSparkMinPages} pages.");
        }
        if (platform.Contains("Amazon", StringComparison.OrdinalIgnoreCase) || platform.Contains("KDP", StringComparison.OrdinalIgnoreCase))
            r.Add("KDP.Compliance", ValidationSeverity.Info, "KDP profile checks applied (margins, pages, barcode, images).");
        if (platform.Contains("Thesis", StringComparison.OrdinalIgnoreCase) && t.TrimWidth < 8)
            r.Add("Thesis.Trim", ValidationSeverity.Warning, "University thesis templates often use A4/Letter trim.");
        r.Add("PDF.Compliance", ValidationSeverity.Info, "Export PDF/X or PDF/A when the target requires archival/print standards.");
    }
}
