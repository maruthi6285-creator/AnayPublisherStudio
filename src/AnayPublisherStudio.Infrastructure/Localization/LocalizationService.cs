using System.Collections.Concurrent;
using System.Globalization;
using System.Resources;
using System.Text.Json;
using AnayPublisherStudio.Application.Abstractions;

namespace AnayPublisherStudio.Infrastructure.Localization;

public sealed class LocalizationService : ILocalizationService
{
    public string CurrentCulture { get; private set; } = "en-US";
    public IReadOnlyList<string> SupportedCultures { get; } = new[] { "en-US", "fr-FR", "de-DE", "es-ES", "hi-IN", "ar-SA" };
    public event Action<string>? CultureChanged;

    private readonly Dictionary<string, Dictionary<string, string>> _strings = new();

    public string this[string key] => GetString(key);

    public LocalizationService()
    {
        LoadBuiltInStrings();
    }

    public void SetCulture(string cultureCode)
    {
        if (!SupportedCultures.Contains(cultureCode)) return;
        CurrentCulture = cultureCode;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(cultureCode);
            CultureInfo.CurrentCulture = new CultureInfo(cultureCode);
        }
        catch { }
        CultureChanged?.Invoke(cultureCode);
    }

    public string GetString(string key, params object[] args)
    {
        if (_strings.TryGetValue(CurrentCulture, out var dict) && dict.TryGetValue(key, out var val))
            return args.Length > 0 ? string.Format(val, args) : val;
        if (_strings.TryGetValue("en-US", out dict) && dict.TryGetValue(key, out val))
            return args.Length > 0 ? string.Format(val, args) : val;
        return key;
    }

    public bool TryGetString(string key, out string value)
    {
        value = GetString(key);
        return value != key;
    }

    private void LoadBuiltInStrings()
    {
        _strings["en-US"] = new Dictionary<string, string>
        {
            ["App.Title"] = "Anay Publisher Studio",
            ["App.Ready"] = "Ready.",
            ["App.OpenManuscript"] = "Open a manuscript to see the live publishing preview.",
            ["App.PublishSuccess"] = "Exported {0} pages. Ready to publish.",
            ["App.PublishIssues"] = "Exported with {0} blocking issue(s).",
            ["App.Publishing"] = "Publishing...",
            ["App.ProjectSaved"] = "Project saved.",
            ["App.NoManuscript"] = "Open a manuscript first.",
            ["Button.OpenManuscript"] = "Open Manuscript",
            ["Button.SaveProject"] = "Save Project",
            ["Button.Publish"] = "Publish",
            ["Button.RefreshPreview"] = "Refresh Preview",
            ["Button.Settings"] = "Save Settings",
            ["Button.Reset"] = "Reset to Defaults",
            ["Tab.Home"] = "Home",
            ["Tab.Layout"] = "Layout",
            ["Tab.Styles"] = "Styles",
            ["Tab.Typography"] = "Typography",
            ["Tab.Cover"] = "Cover",
            ["Tab.AI"] = "AI",
            ["Tab.Review"] = "Review",
            ["Tab.Export"] = "Export",
            ["Tab.Publish"] = "Publish",
            ["Tab.Settings"] = "Settings",
            ["Tab.Diagnostics"] = "Diagnostics",
            ["Tab.Structure"] = "Structure",
            ["Tab.Bookmarks"] = "Bookmarks",
            ["Tab.Assets"] = "Assets",
            ["Tab.Properties"] = "Properties",
            ["Tab.Validation"] = "Validation",
            ["Tab.Preview"] = "Preview",
            ["Settings.General"] = "General",
            ["Settings.Typography"] = "Typography",
            ["Settings.Validation"] = "Validation",
            ["Settings.Rendering"] = "Rendering",
            ["Settings.Publishing"] = "Publishing",
            ["Settings.Theme"] = "Theme",
            ["Settings.Logging"] = "Logging",
            ["Settings.Backup"] = "Backup",
        };

        _strings["fr-FR"] = new Dictionary<string, string>
        {
            ["App.Title"] = "Anay Publisher Studio",
            ["App.Ready"] = "Prêt.",
            ["Button.OpenManuscript"] = "Ouvrir le manuscrit",
            ["Button.SaveProject"] = "Sauvegarder le projet",
            ["Button.Publish"] = "Publier",
            ["Tab.Home"] = "Accueil",
            ["Tab.Layout"] = "Mise en page",
            ["Tab.Settings"] = "Paramètres",
        };

        _strings["de-DE"] = new Dictionary<string, string>
        {
            ["App.Title"] = "Anay Publisher Studio",
            ["App.Ready"] = "Bereit.",
            ["Button.OpenManuscript"] = "Manuskript öffnen",
            ["Button.SaveProject"] = "Projekt speichern",
            ["Button.Publish"] = "Veröffentlichen",
            ["Tab.Home"] = "Start",
            ["Tab.Layout"] = "Layout",
            ["Tab.Settings"] = "Einstellungen",
        };

        _strings["es-ES"] = new Dictionary<string, string>
        {
            ["App.Title"] = "Anay Publisher Studio",
            ["App.Ready"] = "Listo.",
            ["Button.OpenManuscript"] = "Abrir manuscrito",
            ["Tab.Home"] = "Inicio",
            ["Tab.Settings"] = "Configuración",
        };

        _strings["hi-IN"] = new Dictionary<string, string>
        {
            ["App.Title"] = "अनय पब्लिशर स्टूडियो",
            ["App.Ready"] = "तैयार।",
            ["Button.OpenManuscript"] = "पांडुलिपि खोलें",
            ["Tab.Home"] = "होम",
            ["Tab.Settings"] = "सेटिंग्स",
        };

        _strings["ar-SA"] = new Dictionary<string, string>
        {
            ["App.Title"] = "أناي Publisher Studio",
            ["App.Ready"] = "جاهز.",
            ["Button.OpenManuscript"] = "فتح المخطوطة",
            ["Tab.Home"] = "الرئيسية",
            ["Tab.Settings"] = "الإعدادات",
        };
    }
}
