using System.Text.Json;
using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Application.Exceptions;
using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Domain.Layout;
using AnayPublisherStudio.Domain.Model;

namespace AnayPublisherStudio.Application.Pipeline;

/// <summary>
/// Default <see cref="IExportService"/> that wires the engines together into
/// the end-to-end publish flow: parse -> fingerprint -> layout -> spine ->
/// cover -> integrity-verify -> validate. Presentation engines may change
/// margins/typography/headers only; author content is never rewritten.
/// Depends only on Application abstractions so implementations are swappable.
/// </summary>
public sealed class PublishPipeline : IExportService
{
    private readonly IDocumentParser _parser;
    private readonly ITemplateProvider _templates;
    private readonly ISpineCalculator _spine;
    private readonly ILayoutEngine _layout;
    private readonly ICoverEngine _cover;
    private readonly IValidationEngine _validator;
    private readonly IContentIntegrityGuard? _integrity;
    private readonly IProfessionalLayoutEngine? _professional;
    private readonly IArtifactExporter? _artifacts;
    private readonly ICoverDesigner? _coverDesigner;
    private readonly Func<BookDocument, string, string>? _docxFallback;

    /// <summary>Creates the pipeline from its engine dependencies.</summary>
    public PublishPipeline(
        IDocumentParser parser,
        ITemplateProvider templates,
        ISpineCalculator spine,
        ILayoutEngine layout,
        ICoverEngine cover,
        IValidationEngine validator)
        : this(parser, templates, spine, layout, cover, validator, null, null, null, null, null)
    {
    }

    /// <summary>
    /// Creates the pipeline with an optional content-integrity guard that
    /// enforces the absolute rule that author content is never modified.
    /// </summary>
    public PublishPipeline(
        IDocumentParser parser,
        ITemplateProvider templates,
        ISpineCalculator spine,
        ILayoutEngine layout,
        ICoverEngine cover,
        IValidationEngine validator,
        IContentIntegrityGuard? integrity)
        : this(parser, templates, spine, layout, cover, validator, integrity, null, null, null, null)
    {
    }

    /// <summary>Full constructor with professional layout, multi-format export, and cover designer.</summary>
    public PublishPipeline(
        IDocumentParser parser,
        ITemplateProvider templates,
        ISpineCalculator spine,
        ILayoutEngine layout,
        ICoverEngine cover,
        IValidationEngine validator,
        IContentIntegrityGuard? integrity,
        IProfessionalLayoutEngine? professional,
        IArtifactExporter? artifacts,
        ICoverDesigner? coverDesigner)
        : this(parser, templates, spine, layout, cover, validator, integrity, professional, artifacts, coverDesigner, null)
    {
    }

    /// <summary>Full constructor with DOCX fallback for PDF license failures.</summary>
    public PublishPipeline(
        IDocumentParser parser,
        ITemplateProvider templates,
        ISpineCalculator spine,
        ILayoutEngine layout,
        ICoverEngine cover,
        IValidationEngine validator,
        IContentIntegrityGuard? integrity,
        IProfessionalLayoutEngine? professional,
        IArtifactExporter? artifacts,
        ICoverDesigner? coverDesigner,
        Func<BookDocument, string, string>? docxFallback)
    {
        _parser = parser;
        _templates = templates;
        _spine = spine;
        _layout = layout;
        _cover = cover;
        _validator = validator;
        _integrity = integrity;
        _professional = professional;
        _artifacts = artifacts;
        _coverDesigner = coverDesigner;
        _docxFallback = docxFallback;
    }

    /// <inheritdoc/>
    public async Task<PublishResult> PublishAsync(PublishingProject project, string outputDirectory, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(project.ManuscriptPath) || !File.Exists(project.ManuscriptPath))
            throw new FileNotFoundException("Manuscript not found.", project.ManuscriptPath);

        Directory.CreateDirectory(outputDirectory);

        var template = _templates.GetTemplate(project.TemplateId)
            ?? throw new InvalidOperationException($"Template '{project.TemplateId}' not found.");

        // 1. Parse manuscript into the Book Object Model.
        BookDocument book;
        await using (var fs = File.OpenRead(project.ManuscriptPath))
            book = _parser.Parse(fs);

        // Carry user-edited metadata onto the parsed book.
        // Metadata is author/project ownership; it is not presentation.
        if (!string.IsNullOrWhiteSpace(project.Metadata.Title)) book.Metadata.Title = project.Metadata.Title;
        if (!string.IsNullOrWhiteSpace(project.Metadata.Author)) book.Metadata.Author = project.Metadata.Author;

        var result = new PublishResult();

        // Fingerprint author content BEFORE any presentation work.
        string? expectedFingerprint = null;
        if (_integrity is not null)
            expectedFingerprint = _integrity.ComputeFingerprint(book);

        // 1b. Professional composition (async-capable, incremental-friendly).
        LayoutDocument? layoutDoc = null;
        if (_professional is not null)
            layoutDoc = await _professional.ComposeAsync(book, template, ct);

        // 2. Layout + render interior PDF (also resolves spine input: page count).
        //    Falls back to DOCX if PDF license is exceeded.
        var printPath = Path.Combine(outputDirectory, "interior-print.pdf");
        try
        {
            await using (var pdf = File.Create(printPath))
                result.PageCount = _layout.Render(book, template, pdf);
            result.PrintPdfPath = printPath;
            result.Artifacts[ExportFormat.PrintPdf] = printPath;
        }
        catch (PdfLicenseException)
        {
            var docxPath = Path.Combine(outputDirectory, "manuscript.docx");
            if (_docxFallback is not null)
            {
                _docxFallback(book, docxPath);
                result.DocxPath = docxPath;
                result.Artifacts[ExportFormat.Docx] = docxPath;
            }
            result.PrintPdfPath = null;
            result.PageCount = (book.Chapters ?? []).Sum(c => (c.Blocks ?? []).Count);
            return result;
        }

        // Prefer composed page count when available and larger (blank recto pages).
        if (layoutDoc is not null && layoutDoc.PageCount > result.PageCount)
            result.PageCount = layoutDoc.PageCount;

        // 3. Recompute spine from the real page count, then render the cover.
        template.SpineWidth = _spine.CalculateInches(result.PageCount, template.Paper, template.Color);
        if (_coverDesigner is not null)
        {
            var design = _coverDesigner.CreateDesign(project, template, result.PageCount);
            _coverDesigner.RecalculateSpine(design, template, result.PageCount, _spine);
            template.SpineWidth = design.SpineWidth;
            template.OverallWidth = design.OverallWidth;
            template.OverallHeight = design.OverallHeight;
        }

        var coverPath = Path.Combine(outputDirectory, "cover.pdf");
        try
        {
            await using (var cov = File.Create(coverPath))
                _cover.Render(project, template, result.PageCount, cov);
            result.CoverPdfPath = coverPath;
            result.Artifacts[ExportFormat.CoverPdf] = coverPath;
        }
        catch (PdfLicenseException)
        {
            result.CoverPdfPath = null;
        }

        // 4. Verify author content is still intact after presentation.
        if (_integrity is not null && expectedFingerprint is not null)
        {
            result.ContentIntegrity = _integrity.Verify(expectedFingerprint, book);
            if (!result.ContentIntegrity.IsIntact)
                throw new InvalidOperationException(
                    "Content integrity violation: presentation engines modified author content. " +
                    $"Expected={result.ContentIntegrity.ExpectedFingerprint}, " +
                    $"Actual={result.ContentIntegrity.ActualFingerprint}");
        }

        // 5. Validate against publisher rules (preflight).
        result.Validation = _validator.Validate(book, template, result.PageCount);
        var reportPath = Path.Combine(outputDirectory, "validation-report.json");
        await File.WriteAllTextAsync(reportPath,
            JsonSerializer.Serialize(result.Validation, new JsonSerializerOptions { WriteIndented = true }), ct);
        result.ValidationReportPath = reportPath;
        result.Artifacts[ExportFormat.ValidationReport] = reportPath;

        // 6. Optional extended artifacts (EPUB/Kindle/archive) when exporter is registered.
        if (_artifacts is not null)
        {
            layoutDoc ??= new LayoutDocument
            {
                TrimWidth = template.TrimWidth,
                TrimHeight = template.TrimHeight,
                Pages = Enumerable.Range(1, Math.Max(1, result.PageCount))
                    .Select(n => new ComposedPage { PageNumber = n, Side = n % 2 == 1 ? PageSide.Recto : PageSide.Verso })
                    .ToList(),
            };

            var extra = await _artifacts.ExportAsync(
                project, book, template, layoutDoc,
                new[] { ExportFormat.Epub, ExportFormat.Kindle, ExportFormat.ProjectArchive, ExportFormat.DigitalPdf },
                outputDirectory, ct);

            foreach (var kv in extra)
            {
                result.Artifacts[kv.Key] = kv.Value;
                switch (kv.Key)
                {
                    case ExportFormat.Epub: result.EpubPath = kv.Value; break;
                    case ExportFormat.Kindle: result.KindlePath = kv.Value; break;
                    case ExportFormat.ProjectArchive: result.ProjectArchivePath = kv.Value; break;
                    case ExportFormat.DigitalPdf: result.DigitalPdfPath = kv.Value; break;
                    case ExportFormat.Docx: result.DocxPath = kv.Value; break;
                }
            }
        }

        return result;
    }
}
