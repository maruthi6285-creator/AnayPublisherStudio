using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Application.Configuration;
using AnayPublisherStudio.Application.Pipeline;
using AnayPublisherStudio.Infrastructure.Ai;
using AnayPublisherStudio.Infrastructure.Configuration;
using AnayPublisherStudio.Infrastructure.Cover;
using AnayPublisherStudio.Infrastructure.Export;
using AnayPublisherStudio.Infrastructure.Integrity;
using AnayPublisherStudio.Infrastructure.Layout;
using AnayPublisherStudio.Infrastructure.Parsing;
using AnayPublisherStudio.Infrastructure.Persistence;
using AnayPublisherStudio.Infrastructure.Plugins;
using AnayPublisherStudio.Infrastructure.Templates;
using AnayPublisherStudio.Rendering;
using AnayPublisherStudio.Rendering.Preview;
using AnayPublisherStudio.Typography;
using AnayPublisherStudio.Typography.Hyphenation;
using AnayPublisherStudio.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnayPublisherStudio.Composition;

/// <summary>
/// Single composition root. Registers every engine against its Application
/// abstraction so the Presentation, CLI and test hosts share identical wiring.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers all Anay Publisher Studio services using configuration-driven paths.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    public static IServiceCollection AddAnayPublisherStudio(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApplicationOptions>(options =>
            configuration.GetSection(ApplicationOptions.SectionName).Bind(options));

        var appOptions = new ApplicationOptions();
        configuration.GetSection(ApplicationOptions.SectionName).Bind(appOptions);

        var appData = ResolveAppDataDirectory(appOptions);
        var templatesRoot = ResolveTemplatesRoot(appOptions, appData);
        var databasePath = Path.Combine(appData, "projects.db");

        services.Configure<AppDataPaths>(paths =>
        {
            paths.AppData = appData;
            paths.DatabasePath = databasePath;
            paths.TemplatesRoot = templatesRoot;
            paths.PluginsDirectory = Path.Combine(appData, appOptions.Plugins.PluginsDirectory);
            paths.LogsDirectory = Path.Combine(appData, "logs");
            paths.BackupsDirectory = Path.Combine(appData, appOptions.Backup.BackupDirectory);
            paths.CacheDirectory = Path.Combine(appData, "cache");
        });

        services.AddSingleton<ISettingsService, SettingsService>();

        RegisterEngines(services, templatesRoot, databasePath);

        return services;
    }

    /// <summary>Registers all Anay Publisher Studio services using explicit paths (backward-compatible).</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="templatesRoot">Folder containing template.json definitions.</param>
    /// <param name="databasePath">SQLite database file path.</param>
    public static IServiceCollection AddAnayPublisherStudio(
        this IServiceCollection services, string templatesRoot, string databasePath)
    {
        services.Configure<ApplicationOptions>(opts =>
        {
            opts.Templates.RootPath = templatesRoot;
            opts.App.AppDataDirectory = Path.GetDirectoryName(databasePath) ?? string.Empty;
        });

        services.Configure<AppDataPaths>(paths =>
        {
            paths.AppData = Path.GetDirectoryName(databasePath) ?? string.Empty;
            paths.DatabasePath = databasePath;
            paths.TemplatesRoot = templatesRoot;
            paths.PluginsDirectory = Path.Combine(templatesRoot, "..", "Plugins");
            paths.LogsDirectory = Path.Combine(paths.AppData, "logs");
            paths.BackupsDirectory = Path.Combine(paths.AppData, "backups");
            paths.CacheDirectory = Path.Combine(paths.AppData, "cache");
        });

        services.AddSingleton<ISettingsService, SettingsService>();

        RegisterEngines(services, templatesRoot, databasePath);

        return services;
    }

    private static void RegisterEngines(
        IServiceCollection services, string templatesRoot, string databasePath)
    {
        services.AddSingleton<IDocumentParser, DocxDocumentParser>();
        services.AddSingleton<ITemplateProvider>(_ => new JsonTemplateProvider(templatesRoot));
        services.AddSingleton<ISpineCalculator, KdpSpineCalculator>();
        services.AddSingleton<IHyphenationService, HyphenationService>();
        services.AddSingleton<IParagraphComposer>(sp =>
            new ParagraphComposer(sp.GetService<IHyphenationService>()));
        services.AddSingleton<ITypographyEngine, TypographyEngine>();
        services.AddSingleton<IContentIntegrityGuard, ContentIntegrityGuard>();
        services.AddSingleton<IProfessionalLayoutEngine>(sp =>
            new ProfessionalLayoutEngine(
                sp.GetService<ITypographyEngine>(),
                sp.GetService<IParagraphComposer>()));
        services.AddSingleton<ILayoutEngine>(sp =>
            new QuestPdfLayoutEngine(
                sp.GetService<ITypographyEngine>(),
                sp.GetService<IProfessionalLayoutEngine>()));
        services.AddSingleton<ICoverEngine, QuestPdfCoverEngine>();
        services.AddSingleton<ICoverDesigner, CoverDesigner>();
        services.AddSingleton<IValidationEngine, KdpValidationEngine>();
        services.AddSingleton<IAiAssistant, HeuristicAiAssistant>();
        services.AddSingleton<IProjectRepository>(_ => new SqliteProjectRepository(databasePath));
        services.AddSingleton<ILivePreviewEngine>(sp =>
            new LivePreviewEngine(
                sp.GetRequiredService<IProfessionalLayoutEngine>(),
                sp.GetService<ITypographyEngine>()));
        services.AddSingleton<ITemplatePackageService>(sp =>
            new TemplatePackageService(templatesRoot, sp.GetRequiredService<ITemplateProvider>()));
        services.AddSingleton<IPluginManager>(_ =>
            new PluginManager(Path.Combine(templatesRoot, "..", "Plugins")));
        services.AddSingleton<IArtifactExporter>(sp =>
            new ArtifactExporter(
                sp.GetRequiredService<ILayoutEngine>(),
                sp.GetRequiredService<ICoverEngine>(),
                sp.GetRequiredService<IValidationEngine>(),
                sp.GetService<IContentIntegrityGuard>()));
        services.AddSingleton<IExportService>(sp =>
            new PublishPipeline(
                sp.GetRequiredService<IDocumentParser>(),
                sp.GetRequiredService<ITemplateProvider>(),
                sp.GetRequiredService<ISpineCalculator>(),
                sp.GetRequiredService<ILayoutEngine>(),
                sp.GetRequiredService<ICoverEngine>(),
                sp.GetRequiredService<IValidationEngine>(),
                sp.GetService<IContentIntegrityGuard>(),
                sp.GetService<IProfessionalLayoutEngine>(),
                sp.GetService<IArtifactExporter>(),
                sp.GetService<ICoverDesigner>()));
    }

    private static string ResolveAppDataDirectory(ApplicationOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.App.AppDataDirectory))
            return options.App.AppDataDirectory;

        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AnayPublisherStudio");
        Directory.CreateDirectory(appData);
        return appData;
    }

    private static string ResolveTemplatesRoot(ApplicationOptions options, string appData)
    {
        if (!string.IsNullOrWhiteSpace(options.Templates.RootPath))
            return options.Templates.RootPath;

        var templatesRoot = Path.Combine(AppContext.BaseDirectory, "Resources", "Templates");
        if (!Directory.Exists(templatesRoot))
        {
            var probe = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Resources", "Templates"));
            if (Directory.Exists(probe)) templatesRoot = probe;
        }
        return templatesRoot;
    }
}
