using System.Windows;
using System.Windows.Media;

namespace AnayPublisherStudio.Presentation.ViewModels;

/// <summary>Swaps the merged theme dictionary between Light, Dark, and HighContrast.</summary>
public static class ThemeManager
{
    private static bool _dark;

    /// <summary>Toggles the application theme between Light and Dark.</summary>
    public static void Toggle()
    {
        _dark = !_dark;
        ApplyTheme(_dark ? "Dark" : "Light");
    }

    /// <summary>Applies the specified theme by name.</summary>
    /// <param name="themeName">Theme name: "Light", "Dark", or "HighContrast".</param>
    /// <param name="accentColor">Optional accent color hex string (e.g., "#0078D4").</param>
    public static void ApplyTheme(string themeName, string? accentColor = null)
    {
        var fileName = themeName switch
        {
            "Dark" => "Dark",
            "HighContrast" => "HighContrast",
            _ => "Light",
        };

        var uri = new Uri($"Themes/{fileName}.xaml", UriKind.Relative);
        var dict = new ResourceDictionary { Source = uri };
        System.Windows.Application.Current.Resources.MergedDictionaries.Clear();
        System.Windows.Application.Current.Resources.MergedDictionaries.Add(dict);

        if (!string.IsNullOrEmpty(accentColor) && ColorConverter.ConvertFromString(accentColor) is Color color)
        {
            System.Windows.Application.Current.Resources["AccentColor"] = new SolidColorBrush(color);
        }

        _dark = themeName == "Dark";
    }
}
