using System.Text.Json;
using AnayPublisherStudio.Application.Configuration;
using Microsoft.Extensions.Options;

namespace AnayPublisherStudio.Infrastructure.Configuration;

/// <summary>
/// Loads and persists settings. Application options come from IOptions&lt;T&gt;
/// (appsettings.json), user settings are stored as JSON in the app data directory.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly IOptionsMonitor<ApplicationOptions> _options;
    private readonly string _userSettingsPath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private UserSettings _userSettings;

    /// <summary>Creates a new SettingsService.</summary>
    public SettingsService(
        IOptionsMonitor<ApplicationOptions> options,
        IOptionsMonitor<AppDataPaths> paths)
    {
        _options = options;
        _userSettingsPath = Path.Combine(paths.CurrentValue.AppData, UserSettings.FileName);
        _userSettings = LoadUserSettings();
    }

    /// <inheritdoc/>
    public ApplicationOptions Options => _options.CurrentValue;

    /// <inheritdoc/>
    public UserSettings UserSettings => _userSettings;

    /// <inheritdoc/>
    public async Task SaveUserSettingsAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _userSettings.LastModifiedUtc = DateTime.UtcNow;
            var dir = Path.GetDirectoryName(_userSettingsPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_userSettings, new JsonSerializerOptions
            {
                WriteIndented = true,
            });
            await File.WriteAllTextAsync(_userSettingsPath, json, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public Task ReloadAsync(CancellationToken ct = default)
    {
        _userSettings = LoadUserSettings();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task UpdateUserSettingsAsync(Action<UserSettings> update, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            update(_userSettings);
        }
        finally
        {
            _lock.Release();
        }
        await SaveUserSettingsAsync(ct);
    }

    private UserSettings LoadUserSettings()
    {
        if (!File.Exists(_userSettingsPath))
            return new UserSettings();

        try
        {
            var json = File.ReadAllText(_userSettingsPath);
            return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
        }
        catch
        {
            return new UserSettings();
        }
    }
}

/// <summary>
/// Strongly-typed app data directory paths.
/// </summary>
public sealed class AppDataPaths
{
    /// <summary>Root app data directory.</summary>
    public string AppData { get; set; } = string.Empty;

    /// <summary>Database file path.</summary>
    public string DatabasePath { get; set; } = string.Empty;

    /// <summary>Templates root directory.</summary>
    public string TemplatesRoot { get; set; } = string.Empty;

    /// <summary>Plugins directory.</summary>
    public string PluginsDirectory { get; set; } = string.Empty;

    /// <summary>Logs directory.</summary>
    public string LogsDirectory { get; set; } = string.Empty;

    /// <summary>Backups directory.</summary>
    public string BackupsDirectory { get; set; } = string.Empty;

    /// <summary>Cache directory.</summary>
    public string CacheDirectory { get; set; } = string.Empty;
}
