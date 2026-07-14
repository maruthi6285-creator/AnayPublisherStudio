using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Domain.Layout;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;

namespace AnayPublisherStudio.Application.Abstractions;

/// <summary>
/// Multi-format export engine: Print PDF, Digital PDF, PDF/X, PDF/A, EPUB,
/// Kindle, DOCX, project archive, images, validation report.
/// Presentation only — author content is never rewritten.
/// </summary>
public interface IArtifactExporter
{
    /// <summary>Supported formats for this exporter implementation.</summary>
    IReadOnlyList<ExportFormat> SupportedFormats { get; }

    /// <summary>
    /// Exports the requested formats into <paramref name="outputDirectory"/>.
    /// Returns a map of format → output path.
    /// </summary>
    Task<IReadOnlyDictionary<ExportFormat, string>> ExportAsync(
        PublishingProject project,
        BookDocument book,
        PublishingTemplate template,
        LayoutDocument layout,
        IEnumerable<ExportFormat> formats,
        string outputDirectory,
        CancellationToken ct = default);
}
