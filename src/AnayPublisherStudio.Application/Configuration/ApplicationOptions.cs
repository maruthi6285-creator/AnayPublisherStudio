namespace AnayPublisherStudio.Application.Configuration;

/// <summary>
/// Root configuration object bound to appsettings.json.
/// Every setting is configurable; nothing is hardcoded.
/// </summary>
public sealed class ApplicationOptions
{
    /// <summary>Section name in appsettings.json.</summary>
    public const string SectionName = "AnayPublisherStudio";

    /// <summary>Application-level settings.</summary>
    public AppSettings App { get; set; } = new();

    /// <summary>Templates configuration.</summary>
    public TemplatesSettings Templates { get; set; } = new();

    /// <summary>Rendering engine settings.</summary>
    public RenderingSettings Rendering { get; set; } = new();

    /// <summary>Typography settings.</summary>
    public TypographySettings Typography { get; set; } = new();

    /// <summary>Validation settings.</summary>
    public ValidationSettings Validation { get; set; } = new();

    /// <summary>Publishing settings.</summary>
    public PublishingSettings Publishing { get; set; } = new();

    /// <summary>Logging settings.</summary>
    public LoggingSettings Logging { get; set; } = new();

    /// <summary>Theme settings.</summary>
    public ThemeSettings Theme { get; set; } = new();

    /// <summary>Backup settings.</summary>
    public BackupSettings Backup { get; set; } = new();

    /// <summary>Plugin settings.</summary>
    public PluginSettings Plugins { get; set; } = new();
}

/// <summary>
/// General application settings.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Application data directory. Defaults to %AppData%/AnayPublisherStudio.</summary>
    public string AppDataDirectory { get; set; } = string.Empty;

    /// <summary>Default output directory for exports.</summary>
    public string DefaultOutputDirectory { get; set; } = string.Empty;

    /// <summary>Default template ID when none is specified.</summary>
    public string DefaultTemplateId { get; set; } = "amazon-paperback-6x9";

    /// <summary>Enable automatic updates check.</summary>
    public bool CheckForUpdates { get; set; } = true;

    /// <summary>Enable telemetry (anonymous usage data).</summary>
    public bool TelemetryEnabled { get; set; } = false;

    /// <summary>Maximum recent projects to show.</summary>
    public int MaxRecentProjects { get; set; } = 10;

    /// <summary>Enable auto-save for projects.</summary>
    public bool AutoSaveEnabled { get; set; } = true;

    /// <summary>Auto-save interval in minutes.</summary>
    public int AutoSaveIntervalMinutes { get; set; } = 5;

    /// <summary>Language code for the UI (e.g., "en-US", "fr-FR").</summary>
    public string UICulture { get; set; } = "en-US";
}

/// <summary>
/// Templates configuration.
/// </summary>
public sealed class TemplatesSettings
{
    /// <summary>Root directory containing template packages.</summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>Enable template package auto-discovery.</summary>
    public bool AutoDiscover { get; set; } = true;

    /// <summary>Custom template directories (additional search paths).</summary>
    public List<string> CustomPaths { get; set; } = new();
}

/// <summary>
/// Rendering engine settings.
/// </summary>
public sealed class RenderingSettings
{
    /// <summary>Default DPI for embedded images.</summary>
    public int DefaultImageDpi { get; set; } = 300;

    /// <summary>Minimum DPI for print-quality images.</summary>
    public int MinimumImageDpi { get; set; } = 200;

    /// <summary>Enable PDF/A compliance for exports.</summary>
    public bool EnablePdfA { get; set; } = false;

    /// <summary>Enable PDF/X compliance for exports.</summary>
    public bool EnablePdfX { get; set; } = false;

    /// <summary>Embed fonts in PDF output.</summary>
    public bool EmbedFonts { get; set; } = true;

    /// <summary>Subset embedded fonts (smaller file size).</summary>
    public bool SubsetFonts { get; set; } = true;

    /// <summary>Image compression quality (1-100).</summary>
    public int ImageQuality { get; set; } = 85;

    /// <summary>Default color mode: "RGB" or "CMYK".</summary>
    public string ColorMode { get; set; } = "RGB";
}

/// <summary>
/// Typography engine settings.
/// </summary>
public sealed class TypographySettings
{
    /// <summary>Default language for typography rules.</summary>
    public string DefaultLanguage { get; set; } = "en-US";

    /// <summary>Default font fallback chain.</summary>
    public List<string> DefaultFontFallback { get; set; } = new() { "Georgia", "Times New Roman", "serif" };

    /// <summary>Enable ligatures by default.</summary>
    public bool EnableLigatures { get; set; } = true;

    /// <summary>Enable kerning by default.</summary>
    public bool EnableKerning { get; set; } = true;

    /// <summary>Enable optical margin alignment.</summary>
    public bool EnableOpticalAlignment { get; set; } = true;

    /// <summary>Enable hanging punctuation.</summary>
    public bool EnableHangingPunctuation { get; set; } = true;

    /// <summary>Enable hyphenation by default.</summary>
    public bool EnableHyphenation { get; set; } = true;

    /// <summary>Drop cap line count for chapter openings.</summary>
    public int DropCapLines { get; set; } = 3;

    /// <summary>Minimum word spacing for justification.</summary>
    public double MinWordSpacing { get; set; } = 0.8;

    /// <summary>Maximum word spacing for justification.</summary>
    public double MaxWordSpacing { get; set; } = 1.33;
}

/// <summary>
/// Validation engine settings.
/// </summary>
public sealed class ValidationSettings
{
    /// <summary>Enable KDP preflight checks.</summary>
    public bool EnableKdpChecks { get; set; } = true;

    /// <summary>Enable IngramSpark preflight checks.</summary>
    public bool EnableIngramSparkChecks { get; set; } = true;

    /// <summary>Minimum page count for KDP.</summary>
    public int KdpMinPages { get; set; } = 24;

    /// <summary>Maximum page count for KDP.</summary>
    public int KdpMaxPages { get; set; } = 828;

    /// <summary>Minimum page count for IngramSpark.</summary>
    public int IngramSparkMinPages { get; set; } = 18;

    /// <summary>Treat warnings as errors during validation.</summary>
    public bool TreatWarningsAsErrors { get; set; } = false;

    /// <summary>Enable strict image validation.</summary>
    public bool StrictImageValidation { get; set; } = false;
}

/// <summary>
/// Publishing/export settings.
/// </summary>
public sealed class PublishingSettings
{
    /// <summary>Default export format: "PDF", "EPUB", "MOBI".</summary>
    public string DefaultExportFormat { get; set; } = "PDF";

    /// <summary>Enable content integrity checking.</summary>
    public bool EnableContentIntegrity { get; set; } = true;

    /// <summary>Generate cover PDF for every export.</summary>
    public bool AlwaysGenerateCover { get; set; } = true;

    /// <summary>Generate validation report for every export.</summary>
    public bool AlwaysGenerateReport { get; set; } = true;

    /// <summary>Enable barcode generation for print covers.</summary>
    public bool EnableBarcode { get; set; } = true;

    /// <summary>Barcode width in inches.</summary>
    public double BarcodeWidth { get; set; } = 1.5;

    /// <summary>Barcode height in inches.</summary>
    public double BarcodeHeight { get; set; } = 1.0;
}

/// <summary>
/// Logging configuration.
/// </summary>
public sealed class LoggingSettings
{
    /// <summary>Minimum log level: "Verbose", "Debug", "Information", "Warning", "Error", "Fatal".</summary>
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>Log file path (relative to app data directory).</summary>
    public string LogFilePath { get; set; } = "logs/aps.log";

    /// <summary>Maximum log file size in MB before rotation.</summary>
    public int MaxFileSizeMb { get; set; } = 50;

    /// <summary>Number of retained log files.</summary>
    public int RetainedFileCount { get; set; } = 5;

    /// <summary>Enable structured logging.</summary>
    public bool StructuredLogging { get; set; } = true;

    /// <summary>Enable performance tracing.</summary>
    public bool EnablePerformanceTracing { get; set; } = false;
}

/// <summary>
/// Theme configuration.
/// </summary>
public sealed class ThemeSettings
{
    /// <summary>Active theme name: "Light", "Dark", "HighContrast", or custom.</summary>
    public string ActiveTheme { get; set; } = "Light";

    /// <summary>Accent color as hex string (e.g., "#0078D4").</summary>
    public string AccentColor { get; set; } = "#0078D4";

    /// <summary>UI font family.</summary>
    public string FontFamily { get; set; } = "Segoe UI";

    /// <summary>UI font size in points.</summary>
    public double FontSize { get; set; } = 12.0;

    /// <summary>Enable animations.</summary>
    public bool EnableAnimations { get; set; } = true;
}

/// <summary>
/// Backup configuration.
/// </summary>
public sealed class BackupSettings
{
    /// <summary>Enable automatic backups.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Backup interval in minutes.</summary>
    public int IntervalMinutes { get; set; } = 15;

    /// <summary>Maximum number of backups to retain per project.</summary>
    public int MaxBackupsPerProject { get; set; } = 50;

    /// <summary>Backup directory (relative to app data directory).</summary>
    public string BackupDirectory { get; set; } = "backups";

    /// <summary>Enable incremental backups.</summary>
    public bool Incremental { get; set; } = true;
}

/// <summary>
/// Plugin configuration.
/// </summary>
public sealed class PluginSettings
{
    /// <summary>Plugins directory (relative to app data directory).</summary>
    public string PluginsDirectory { get; set; } = "plugins";

    /// <summary>Enable plugin auto-discovery.</summary>
    public bool AutoDiscover { get; set; } = true;

    /// <summary>Enable plugin digital signature verification.</summary>
    public bool RequireSignature { get; set; } = false;

    /// <summary>Maximum plugin isolation: "AppDomain" or "AssemblyLoadContext".</summary>
    public string IsolationMode { get; set; } = "AssemblyLoadContext";
}
