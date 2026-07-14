namespace AnayPublisherStudio.Application.Configuration;

/// <summary>
/// User-specific settings that persist across sessions.
/// Stored as JSON in the app data directory.
/// </summary>
public sealed class UserSettings
{
    /// <summary>Section name in usersettings.json.</summary>
    public const string FileName = "usersettings.json";

    /// <summary>Last opened manuscript paths.</summary>
    public List<string> RecentProjects { get; set; } = new();

    /// <summary>Last used template ID.</summary>
    public string LastTemplateId { get; set; } = "amazon-paperback-6x9";

    /// <summary>Last used output directory.</summary>
    public string LastOutputDirectory { get; set; } = string.Empty;

    /// <summary>Window layout state.</summary>
    public WindowSettings Window { get; set; } = new();

    /// <summary>User-selected theme override.</summary>
    public string? ThemeOverride { get; set; }

    /// <summary>User-selected accent color override.</summary>
    public string? AccentColorOverride { get; set; }

    /// <summary>Default export format preference.</summary>
    public string DefaultExportFormat { get; set; } = "PDF";

    /// <summary>Default language preference.</summary>
    public string Language { get; set; } = "en-US";

    /// <summary>Whether the user has completed onboarding.</summary>
    public bool OnboardingCompleted { get; set; } = false;

    /// <summary>Timestamp of last settings modification.</summary>
    public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Window layout persistence.
/// </summary>
public sealed class WindowSettings
{
    /// <summary>Window position X.</summary>
    public double Left { get; set; } = 100;

    /// <summary>Window position Y.</summary>
    public double Top { get; set; } = 100;

    /// <summary>Window width.</summary>
    public double Width { get; set; } = 1400;

    /// <summary>Window height.</summary>
    public double Height { get; set; } = 860;

    /// <summary>Whether the window is maximized.</summary>
    public bool IsMaximized { get; set; } = false;

    /// <summary>Left panel width.</summary>
    public double LeftPanelWidth { get; set; } = 260;

    /// <summary>Right panel width.</summary>
    public double RightPanelWidth { get; set; } = 320;

    /// <summary>Selected ribbon tab index.</summary>
    public int SelectedTabIndex { get; set; } = 0;
}
