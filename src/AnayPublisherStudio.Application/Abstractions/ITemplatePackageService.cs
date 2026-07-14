using AnayPublisherStudio.Domain.ValueObjects;

namespace AnayPublisherStudio.Application.Abstractions;

/// <summary>
/// Publishing Template SDK: installable packages with template.json, styles.json,
/// fonts.json, layout.json, cover.json, preview.png, publisher.json.
/// </summary>
public interface ITemplatePackageService
{
    /// <summary>Lists installed template package ids.</summary>
    IReadOnlyList<string> ListInstalledPackages();

    /// <summary>Installs a template package from a folder or zip path.</summary>
    Task InstallAsync(string packagePath, CancellationToken ct = default);

    /// <summary>Uninstalls a package by id.</summary>
    Task UninstallAsync(string packageId, CancellationToken ct = default);

    /// <summary>Loads the full package descriptor for a template id.</summary>
    TemplatePackage? GetPackage(string templateId);
}

/// <summary>Descriptor for an installed template package (SDK layout).</summary>
public sealed class TemplatePackage
{
    /// <summary>Template id.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Root folder of the package.</summary>
    public string RootPath { get; init; } = string.Empty;

    /// <summary>Resolved publishing template.</summary>
    public PublishingTemplate Template { get; init; } = new();

    /// <summary>Optional preview image path.</summary>
    public string? PreviewImagePath { get; init; }

    /// <summary>Raw publisher.json content when present.</summary>
    public string? PublisherJson { get; init; }

    /// <summary>Raw layout.json content when present.</summary>
    public string? LayoutJson { get; init; }

    /// <summary>Raw cover.json content when present.</summary>
    public string? CoverJson { get; init; }

    /// <summary>Raw styles.json content when present.</summary>
    public string? StylesJson { get; init; }

    /// <summary>Raw fonts.json content when present.</summary>
    public string? FontsJson { get; init; }
}
