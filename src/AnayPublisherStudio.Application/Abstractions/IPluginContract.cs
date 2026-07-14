namespace AnayPublisherStudio.Application.Abstractions;

/// <summary>
/// Base contract that all plugin entry types must implement.
/// Plugin authors reference this interface via the SDK assembly.
/// </summary>
public interface IPluginContract
{
    /// <summary>Unique plugin identifier.</summary>
    string Id { get; }

    /// <summary>Display name.</summary>
    string Name { get; }

    /// <summary>Semantic version of this plugin.</summary>
    string Version { get; }

    /// <summary>Called when the plugin is loaded, before any other interaction.</summary>
    Task<bool> InitializeAsync(CancellationToken ct = default);

    /// <summary>Called when the plugin is being unloaded.</summary>
    Task<bool> ShutdownAsync(CancellationToken ct = default);
}
