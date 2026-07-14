namespace AnayPublisherStudio.Domain.Plugins;

/// <summary>Categories of dynamically loadable plugins.</summary>
public enum PluginKind
{
    /// <summary>Publisher profile / platform pack.</summary>
    Publisher,
    /// <summary>AI assistant provider.</summary>
    Ai,
    /// <summary>Export format plugin.</summary>
    Export,
    /// <summary>Template package plugin.</summary>
    Templates,
    /// <summary>Validation rule pack.</summary>
    Validation,
    /// <summary>UI theme pack.</summary>
    Themes,
    /// <summary>Typography / OpenType features pack.</summary>
    Typography,
    /// <summary>Hyphenation / spelling dictionaries.</summary>
    Dictionaries,
    /// <summary>Language pack.</summary>
    LanguagePacks,
}
