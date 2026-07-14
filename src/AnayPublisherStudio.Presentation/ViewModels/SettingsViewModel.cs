using System.Collections.ObjectModel;
using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Application.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;

namespace AnayPublisherStudio.Presentation.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IThemeService _theme;
    private readonly ILocalizationService _localization;

    public ObservableCollection<string> Themes { get; } = new() { "Light", "Dark", "HighContrast" };
    public ObservableCollection<string> Languages { get; }
    public ObservableCollection<string> ExportFormats { get; } = new() { "PDF", "EPUB", "MOBI" };
    public ObservableCollection<string> ColorModes { get; } = new() { "RGB", "CMYK" };

    [ObservableProperty] private string _defaultTemplateId;
    [ObservableProperty] private bool _autoSaveEnabled;
    [ObservableProperty] private int _autoSaveIntervalMinutes;
    [ObservableProperty] private int _maxRecentProjects;
    [ObservableProperty] private bool _checkForUpdates;
    [ObservableProperty] private string _uiCultureCode;

    [ObservableProperty] private string _defaultLanguage;
    [ObservableProperty] private bool _enableLigatures;
    [ObservableProperty] private bool _enableKerning;
    [ObservableProperty] private bool _enableHyphenation;
    [ObservableProperty] private bool _enableOpticalAlignment;
    [ObservableProperty] private int _dropCapLines;

    [ObservableProperty] private bool _enableKdpChecks;
    [ObservableProperty] private bool _enableIngramSparkChecks;
    [ObservableProperty] private int _kdpMinPages;
    [ObservableProperty] private int _kdpMaxPages;
    [ObservableProperty] private int _ingramSparkMinPages;
    [ObservableProperty] private bool _treatWarningsAsErrors;

    [ObservableProperty] private int _defaultImageDpi;
    [ObservableProperty] private bool _embedFonts;
    [ObservableProperty] private bool _subsetFonts;
    [ObservableProperty] private int _imageQuality;
    [ObservableProperty] private string _colorMode;

    [ObservableProperty] private string _defaultExportFormat;
    [ObservableProperty] private bool _enableContentIntegrity;
    [ObservableProperty] private bool _alwaysGenerateCover;
    [ObservableProperty] private bool _alwaysGenerateReport;

    [ObservableProperty] private string _activeTheme;
    [ObservableProperty] private string _accentColor;
    [ObservableProperty] private string _themeFontFamily;
    [ObservableProperty] private double _themeFontSize;
    [ObservableProperty] private bool _enableAnimations;

    [ObservableProperty] private string _logMinimumLevel;
    [ObservableProperty] private int _logMaxFileSizeMb;
    [ObservableProperty] private bool _enablePerformanceTracing;

    [ObservableProperty] private bool _backupEnabled;
    [ObservableProperty] private int _backupIntervalMinutes;
    [ObservableProperty] private int _maxBackupsPerProject;

    public SettingsViewModel(ISettingsService settings, IThemeService theme, ILocalizationService localization)
    {
        _settings = settings;
        _theme = theme;
        _localization = localization;
        Languages = new ObservableCollection<string>(localization.SupportedCultures);

        var opts = settings.Options;

        _defaultTemplateId = opts.App.DefaultTemplateId;
        _autoSaveEnabled = opts.App.AutoSaveEnabled;
        _autoSaveIntervalMinutes = opts.App.AutoSaveIntervalMinutes;
        _maxRecentProjects = opts.App.MaxRecentProjects;
        _checkForUpdates = opts.App.CheckForUpdates;
        _uiCultureCode = opts.App.UICulture;

        _defaultLanguage = opts.Typography.DefaultLanguage;
        _enableLigatures = opts.Typography.EnableLigatures;
        _enableKerning = opts.Typography.EnableKerning;
        _enableHyphenation = opts.Typography.EnableHyphenation;
        _enableOpticalAlignment = opts.Typography.EnableOpticalAlignment;
        _dropCapLines = opts.Typography.DropCapLines;

        _enableKdpChecks = opts.Validation.EnableKdpChecks;
        _enableIngramSparkChecks = opts.Validation.EnableIngramSparkChecks;
        _kdpMinPages = opts.Validation.KdpMinPages;
        _kdpMaxPages = opts.Validation.KdpMaxPages;
        _ingramSparkMinPages = opts.Validation.IngramSparkMinPages;
        _treatWarningsAsErrors = opts.Validation.TreatWarningsAsErrors;

        _defaultImageDpi = opts.Rendering.DefaultImageDpi;
        _embedFonts = opts.Rendering.EmbedFonts;
        _subsetFonts = opts.Rendering.SubsetFonts;
        _imageQuality = opts.Rendering.ImageQuality;
        _colorMode = opts.Rendering.ColorMode;

        _defaultExportFormat = opts.Publishing.DefaultExportFormat;
        _enableContentIntegrity = opts.Publishing.EnableContentIntegrity;
        _alwaysGenerateCover = opts.Publishing.AlwaysGenerateCover;
        _alwaysGenerateReport = opts.Publishing.AlwaysGenerateReport;

        _activeTheme = opts.Theme.ActiveTheme;
        _accentColor = opts.Theme.AccentColor;
        _themeFontFamily = opts.Theme.FontFamily;
        _themeFontSize = opts.Theme.FontSize;
        _enableAnimations = opts.Theme.EnableAnimations;

        _logMinimumLevel = opts.Logging.MinimumLevel;
        _logMaxFileSizeMb = opts.Logging.MaxFileSizeMb;
        _enablePerformanceTracing = opts.Logging.EnablePerformanceTracing;

        _backupEnabled = opts.Backup.Enabled;
        _backupIntervalMinutes = opts.Backup.IntervalMinutes;
        _maxBackupsPerProject = opts.Backup.MaxBackupsPerProject;
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        await _settings.UpdateUserSettingsAsync(us =>
        {
            us.ThemeOverride = ActiveTheme;
            us.AccentColorOverride = AccentColor;
            us.Language = DefaultLanguage;
            us.DefaultExportFormat = DefaultExportFormat;
            us.LastTemplateId = DefaultTemplateId;
        });

        var kind = ActiveTheme switch
        {
            "Dark" => ThemeKind.Dark,
            "HighContrast" => ThemeKind.HighContrast,
            _ => ThemeKind.Light,
        };
        _theme.ApplyTheme(kind, AccentColor);
        _localization.SetCulture(UiCultureCode);
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        var defaults = new ApplicationOptions();
        DefaultTemplateId = defaults.App.DefaultTemplateId;
        AutoSaveEnabled = defaults.App.AutoSaveEnabled;
        AutoSaveIntervalMinutes = defaults.App.AutoSaveIntervalMinutes;
        MaxRecentProjects = defaults.App.MaxRecentProjects;
        CheckForUpdates = defaults.App.CheckForUpdates;
        UiCultureCode = defaults.App.UICulture;

        DefaultLanguage = defaults.Typography.DefaultLanguage;
        EnableLigatures = defaults.Typography.EnableLigatures;
        EnableKerning = defaults.Typography.EnableKerning;
        EnableHyphenation = defaults.Typography.EnableHyphenation;
        EnableOpticalAlignment = defaults.Typography.EnableOpticalAlignment;
        DropCapLines = defaults.Typography.DropCapLines;

        EnableKdpChecks = defaults.Validation.EnableKdpChecks;
        EnableIngramSparkChecks = defaults.Validation.EnableIngramSparkChecks;
        KdpMinPages = defaults.Validation.KdpMinPages;
        KdpMaxPages = defaults.Validation.KdpMaxPages;
        IngramSparkMinPages = defaults.Validation.IngramSparkMinPages;
        TreatWarningsAsErrors = defaults.Validation.TreatWarningsAsErrors;

        DefaultImageDpi = defaults.Rendering.DefaultImageDpi;
        EmbedFonts = defaults.Rendering.EmbedFonts;
        SubsetFonts = defaults.Rendering.SubsetFonts;
        ImageQuality = defaults.Rendering.ImageQuality;
        ColorMode = defaults.Rendering.ColorMode;

        DefaultExportFormat = defaults.Publishing.DefaultExportFormat;
        EnableContentIntegrity = defaults.Publishing.EnableContentIntegrity;
        AlwaysGenerateCover = defaults.Publishing.AlwaysGenerateCover;
        AlwaysGenerateReport = defaults.Publishing.AlwaysGenerateReport;

        ActiveTheme = defaults.Theme.ActiveTheme;
        AccentColor = defaults.Theme.AccentColor;
        ThemeFontFamily = defaults.Theme.FontFamily;
        ThemeFontSize = defaults.Theme.FontSize;
        EnableAnimations = defaults.Theme.EnableAnimations;

        LogMinimumLevel = defaults.Logging.MinimumLevel;
        LogMaxFileSizeMb = defaults.Logging.MaxFileSizeMb;
        EnablePerformanceTracing = defaults.Logging.EnablePerformanceTracing;

        BackupEnabled = defaults.Backup.Enabled;
        BackupIntervalMinutes = defaults.Backup.IntervalMinutes;
        MaxBackupsPerProject = defaults.Backup.MaxBackupsPerProject;
    }
}
