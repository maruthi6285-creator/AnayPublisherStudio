using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Application.Configuration;
using AnayPublisherStudio.Composition;
using AnayPublisherStudio.Domain.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// Usage: dotnet run --project src/AnayPublisherStudio.Cli -- <manuscript.docx> [templateId] [--output <dir>]
if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: aps-cli <manuscript.docx> [templateId] [--output <dir>]");
    return 1;
}

var manuscript = args[0];
var templateId = "amazon-paperback-6x9";
var outputDir = Path.Combine(Path.GetDirectoryName(manuscript) ?? ".", "output");

for (var i = 1; i < args.Length; i++)
{
    if (args[i] == "--output" && i + 1 < args.Length)
    {
        outputDir = args[++i];
    }
    else
    {
        templateId = args[i];
    }
}

Directory.CreateDirectory(outputDir);

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        [$"{ApplicationOptions.SectionName}:Templates:RootPath"] = args.Length > 1 && args[1] != "--output"
            ? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Resources", "Templates")
            : Path.Combine(AppContext.BaseDirectory, "Resources", "Templates"),
        [$"{ApplicationOptions.SectionName}:App:DefaultOutputDirectory"] = outputDir,
    })
    .Build();

var services = new ServiceCollection()
    .AddAnayPublisherStudio(configuration)
    .BuildServiceProvider();

var exporter = services.GetRequiredService<IExportService>();

var project = new PublishingProject
{
    Name = "CLI Run",
    ManuscriptPath = manuscript,
    TemplateId = templateId,
};

Console.WriteLine($"Publishing '{Path.GetFileName(manuscript)}' with template '{templateId}'...");
var result = await exporter.PublishAsync(project, outputDir);

Console.WriteLine();
Console.WriteLine($"  Interior PDF : {result.PrintPdfPath}");
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

return result.Validation.IsPublishable ? 0 : 2;
