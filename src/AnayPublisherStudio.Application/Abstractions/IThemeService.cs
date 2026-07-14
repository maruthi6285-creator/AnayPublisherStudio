namespace AnayPublisherStudio.Application.Abstractions;

public enum ThemeKind
{
    Light,
    Dark,
    HighContrast,
    Custom,
}

public sealed record CustomTheme
{
    public string Name { get; init; } = "Custom";
    public string WindowBackground { get; init; } = "#F5F5F5";
    public string CanvasBackground { get; init; } = "#E8E8E8";
    public string PrimaryText { get; init; } = "#1A1A1A";
    public string SecondaryText { get; init; } = "#666666";
    public string BorderBrush { get; init; } = "#CCCCCC";
    public string ButtonBackground { get; init; } = "#E0E0E0";
    public string AccentColor { get; init; } = "#0078D4";
}

public interface IThemeService
{
    event Action<ThemeKind>? ThemeChanged;
    ThemeKind ActiveTheme { get; }
    void ApplyTheme(ThemeKind theme, string? accentColor = null);
    void ApplyCustomTheme(CustomTheme theme);
    CustomTheme? CurrentCustomTheme { get; }
}
