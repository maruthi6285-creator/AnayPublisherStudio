using AnayPublisherStudio.Application.Pipeline;
using AnayPublisherStudio.Domain.Model;

namespace AnayPublisherStudio.Application.Abstractions;

/// <summary>Top-level façade that runs the full publish pipeline.</summary>
public interface IExportService
{
    /// <summary>
    /// Parses the manuscript, applies the template, renders interior + cover
    /// PDFs and validates the result, writing artifacts to
    /// <paramref name="outputDirectory"/>.
    /// </summary>
    Task<PublishResult> PublishAsync(PublishingProject project, string outputDirectory, CancellationToken ct = default);
}
