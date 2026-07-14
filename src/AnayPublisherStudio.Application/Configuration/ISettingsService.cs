namespace AnayPublisherStudio.Application.Configuration;

/// <summary>
/// Service for loading, saving, and accessing application and user settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>Gets the current application options (read-only).</summary>
    ApplicationOptions Options { get; }

    /// <summary>Gets the current user settings.</summary>
    UserSettings UserSettings { get; }

    /// <summary>Saves user settings to disk.</summary>
    Task SaveUserSettingsAsync(CancellationToken ct = default);

    /// <summary>Reloads application options from configuration files.</summary>
    Task ReloadAsync(CancellationToken ct = default);

    /// <summary>Updates a user setting and persists it.</summary>
    Task UpdateUserSettingsAsync(Action<UserSettings> update, CancellationToken ct = default);
}
