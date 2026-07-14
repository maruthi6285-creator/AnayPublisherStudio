namespace AnayPublisherStudio.Application.Abstractions;

public interface ILocalizationService
{
    string CurrentCulture { get; }
    IReadOnlyList<string> SupportedCultures { get; }
    string this[string key] { get; }
    event Action<string>? CultureChanged;
    void SetCulture(string cultureCode);
    string GetString(string key, params object[] args);
    bool TryGetString(string key, out string value);
}
