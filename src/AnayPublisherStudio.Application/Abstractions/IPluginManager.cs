using AnayPublisherStudio.Domain.Plugins;

namespace AnayPublisherStudio.Application.Abstractions;

/// <summary>
/// Discovers and loads plugins dynamically (publisher, AI, export, templates,
/// validation, themes, typography, dictionaries, language packs).
/// </summary>
public interface IPluginManager
{
    /// <summary>Scans plugin directories and returns manifests.</summary>
    IReadOnlyList<PluginManifest> Discover();

    /// <summary>Loads a plugin assembly (if present) into an isolated context.</summary>
    Task<object?> LoadAsync(PluginManifest manifest, CancellationToken ct = default);

    /// <summary>All currently loaded manifests.</summary>
    IReadOnlyList<PluginManifest> Loaded { get; }
}
