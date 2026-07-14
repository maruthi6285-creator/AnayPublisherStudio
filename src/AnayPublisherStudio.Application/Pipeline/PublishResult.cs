using AnayPublisherStudio.Application.Validation;
using AnayPublisherStudio.Domain.Enums;

namespace AnayPublisherStudio.Application.Pipeline;

/// <summary>Artifacts and status produced by a publish run.</summary>
public sealed class PublishResult
{
    /// <summary>Path to the interior print-ready PDF.</summary>
    public string? PrintPdfPath { get; set; }

    /// <summary>Path to the wraparound cover PDF.</summary>
    public string? CoverPdfPath { get; set; }

    /// <summary>Path to the written validation report.</summary>
    public string? ValidationReportPath { get; set; }

    /// <summary>Path to digital PDF when exported.</summary>
    public string? DigitalPdfPath { get; set; }

    /// <summary>Path to EPUB when exported.</summary>
    public string? EpubPath { get; set; }

    /// <summary>Path to Kindle package when exported.</summary>
    public string? KindlePath { get; set; }

    /// <summary>Path to project archive when exported.</summary>
    public string? ProjectArchivePath { get; set; }

    /// <summary>Path to DOCX export when produced.</summary>
    public string? DocxPath { get; set; }

    /// <summary>Additional format → path map for extended exports.</summary>
    public Dictionary<ExportFormat, string> Artifacts { get; } = new();

    /// <summary>Final interior page count.</summary>
    public int PageCount { get; set; }

    /// <summary>The validation report.</summary>
    public ValidationReport Validation { get; set; } = new();

    /// <summary>
    /// Result of the author-content integrity check (null if not run). When
    /// present and not intact, the run modified author content and must be
    /// treated as failed.
    /// </summary>
    public Integrity.ContentIntegrityResult? ContentIntegrity { get; set; }
}
