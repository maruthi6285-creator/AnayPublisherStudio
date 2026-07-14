namespace AnayPublisherStudio.Domain.Plugins;

/// <summary>Declarative plugin package metadata (plugin.json).</summary>
public sealed class PluginManifest
{
    /// <summary>Stable plugin id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Semantic version.</summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>Plugin category.</summary>
    public PluginKind Kind { get; set; } = PluginKind.Templates;

    /// <summary>Optional assembly file name relative to the plugin folder.</summary>
    public string? Assembly { get; set; }

    /// <summary>Optional entry type (full name) implementing a known contract.</summary>
    public string? EntryType { get; set; }

    /// <summary>Human description.</summary>
    public string? Description { get; set; }
}
