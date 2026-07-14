using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Application.Configuration;
using AnayPublisherStudio.Composition;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;
using AnayPublisherStudio.Infrastructure.Export;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

// Usage: aps-cli <manuscript.docx> [templateId] [--output <dir>] [--cover <image>] [--format pdf|docx]
if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: aps-cli <manuscript.docx> [templateId] [--output <dir>] [--cover <image>] [--format pdf|docx]");
    return 1;
}

var manuscript = args[0];
var templateId = "amazon-paperback-6x9";
var outputDir = Path.Combine(Path.GetDirectoryName(manuscript) ?? ".", "output");
string? coverImage = null;
var format = "pdf";

for (var i = 1; i < args.Length; i++)
{
    if (args[i] == "--output" && i + 1 < args.Length)
    {
        outputDir = args[++i];
    }
    else if (args[i] == "--cover" && i + 1 < args.Length)
    {
        coverImage = args[++i];
    }
    else if (args[i] == "--format" && i + 1 < args.Length)
    {
        format = args[++i].ToLowerInvariant();
    }
    else
    {
        templateId = args[i];
    }
}

Directory.CreateDirectory(outputDir);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        Path.Combine(outputDir, "aps-cli-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 3)
    .CreateLogger();

try
{
    Log.Information("Anay Publisher Studio CLI starting");

    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
        .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
        .AddEnvironmentVariables(prefix: "APS_")
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{ApplicationOptions.SectionName}:Templates:RootPath"] = ResolveTemplatesRoot(),
            [$"{ApplicationOptions.SectionName}:App:DefaultOutputDirectory"] = outputDir,
        })
        .Build();

    var services = new ServiceCollection()
        .AddAnayPublisherStudio(configuration)
        .AddLogging(builder => builder.AddSerilog(dispose: true))
        .BuildServiceProvider();

    var logger = services.GetRequiredService<ILogger<Program>>();

    if (format == "docx")
    {
        var templateProvider = services.GetRequiredService<ITemplateProvider>();
        var tmpl = templateProvider.GetTemplate(templateId);

        Log.Information("Parsing '{File}'", Path.GetFileName(manuscript));

        IDocumentParser parser;
        BookDocument book;
        using (var stream = File.OpenRead(manuscript))
        {
            parser = services.GetRequiredService<IDocumentParser>();
            book = parser.Parse(stream);
        }

        if (string.IsNullOrWhiteSpace(book.Metadata?.Title))
        {
            book.Metadata ??= new BookMetadata();
            book.Metadata.Title = Path.GetFileNameWithoutExtension(manuscript);
        }

        var docxPath = Path.Combine(outputDir, "manuscript.docx");
        Directory.CreateDirectory(outputDir);

        Log.Information("Applying professional KDP formatting to '{File}'", Path.GetFileName(manuscript));

        DocxDocumentWriter.Write(book, tmpl, docxPath);

        Console.WriteLine();
        Console.WriteLine($"  Manuscript : {docxPath}");
        Console.WriteLine($"  Page       : {tmpl.TrimWidth}\u00d7{tmpl.TrimHeight} in");
        Console.WriteLine($"  Font       : {tmpl.BodyFont} {tmpl.BodyFontSize}pt");
        Console.WriteLine($"  Margins    : T={tmpl.TopMargin} B={tmpl.BottomMargin} I={tmpl.InsideMargin} O={tmpl.OutsideMargin}");
        Log.Information("Professional KDP formatting applied to {Path}", docxPath);
        return 0;
    }

    var exporter = services.GetRequiredService<IExportService>();

    var project = new PublishingProject
    {
        Name = "CLI Run",
        ManuscriptPath = manuscript,
        TemplateId = templateId,
        FrontCoverImagePath = coverImage,
    };

    Log.Information("Publishing '{File}' with template '{Template}'",
        Path.GetFileName(manuscript), templateId);
    var result = await exporter.PublishAsync(project, outputDir);

    Console.WriteLine();
    if (result.PrintPdfPath is not null)
        Console.WriteLine($"  Interior PDF : {result.PrintPdfPath}");
    if (result.DocxPath is not null)
        Console.WriteLine($"  Manuscript   : {result.DocxPath}");
    if (result.CoverPdfPath is not null)
        Console.WriteLine($"  Cover PDF    : {result.CoverPdfPath}");
    Console.WriteLine($"  Page count   : {result.PageCount}");
    Console.WriteLine($"  Report       : {result.ValidationReportPath}");
    if (result.ContentIntegrity is not null)
        Console.WriteLine($"  Content OK   : {result.ContentIntegrity.IsIntact}");
    Console.WriteLine($"  Publishable  : {result.Validation.IsPublishable} " +
                      $"({result.Validation.ErrorCount} errors, {result.Validation.WarningCount} warnings)");
    Console.WriteLine();
    foreach (var f in result.Validation.Findings)
        Console.WriteLine($"   - {f}");

    Log.Information("Publishing completed: {PageCount} pages, {Result}",
        result.PageCount, result.Validation.IsPublishable ? "PASS" : "FAIL");

    return result.Validation.IsPublishable ? 0 : 2;
}
catch (Exception ex)
{
    Log.Fatal(ex, "CLI failed");
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

static string ResolveTemplatesRoot()
{
    var deployed = Path.Combine(AppContext.BaseDirectory, "Resources", "Templates");
    if (Directory.Exists(deployed))
        return deployed;
    var dev = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Resources", "Templates");
    if (Directory.Exists(dev))
        return Path.GetFullPath(dev);
    return deployed;
}
