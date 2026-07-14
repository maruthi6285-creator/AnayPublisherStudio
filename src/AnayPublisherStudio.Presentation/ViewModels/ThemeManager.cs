using System.Windows;
using System.Windows.Media;
using AnayPublisherStudio.Application.Abstractions;

namespace AnayPublisherStudio.Presentation.ViewModels;

public sealed class ThemeService : IThemeService
{
    public event Action<ThemeKind>? ThemeChanged;
    public ThemeKind ActiveTheme { get; private set; } = ThemeKind.Light;
    public CustomTheme? CurrentCustomTheme { get; private set; }

    public void ApplyTheme(ThemeKind theme, string? accentColor = null)
    {
        ActiveTheme = theme;
        var fileName = theme switch
        {
            ThemeKind.Dark => "Dark",
            ThemeKind.HighContrast => "HighContrast",
            _ => "Light",
        };

        var uri = new Uri($"Themes/{fileName}.xaml", UriKind.Relative);
        var dict = new ResourceDictionary { Source = uri };
        System.Windows.Application.Current.Resources.MergedDictionaries.Clear();
        System.Windows.Application.Current.Resources.MergedDictionaries.Add(dict);

        if (!string.IsNullOrEmpty(accentColor) && ColorConverter.ConvertFromString(accentColor) is Color color)
        {
            System.Windows.Application.Current.Resources["AccentColor"] = new SolidColorBrush(color);
            var lighter = Color.FromArgb(color.A,
                (byte)Math.Min(255, color.R + 64),
                (byte)Math.Min(255, color.G + 64),
                (byte)Math.Min(255, color.B + 64));
            System.Windows.Application.Current.Resources["AccentColorLight"] = new SolidColorBrush(lighter);
        }

        ThemeChanged?.Invoke(theme);
    }

    public void ApplyCustomTheme(CustomTheme theme)
    {
        CurrentCustomTheme = theme;
        ActiveTheme = ThemeKind.Custom;

        ApplyColor("WindowBackground", theme.WindowBackground);
        ApplyColor("CanvasBackground", theme.CanvasBackground);
        ApplyColor("PrimaryText", theme.PrimaryText);
        ApplyColor("SecondaryText", theme.SecondaryText);
        ApplyColor("BorderBrush", theme.BorderBrush);
        ApplyColor("ButtonBackground", theme.ButtonBackground);
        ApplyColor("AccentColor", theme.AccentColor);

        ThemeChanged?.Invoke(ThemeKind.Custom);
    }

    private static void ApplyColor(string key, string hex)
    {
        if (ColorConverter.ConvertFromString(hex) is Color color)
            System.Windows.Application.Current.Resources[key] = new SolidColorBrush(color);
    }
}
