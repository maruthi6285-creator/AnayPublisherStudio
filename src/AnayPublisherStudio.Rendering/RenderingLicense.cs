using QuestPDF.Infrastructure;

namespace AnayPublisherStudio.Rendering;

/// <summary>Central place to configure the QuestPDF licence once at startup.</summary>
public static class RenderingLicense
{
    private static bool _configured;

    /// <summary>Sets the QuestPDF Community licence (idempotent).</summary>
    public static void Configure()
    {
        if (_configured) return;
        QuestPDF.Settings.License = LicenseType.Community;
        _configured = true;
    }
}
