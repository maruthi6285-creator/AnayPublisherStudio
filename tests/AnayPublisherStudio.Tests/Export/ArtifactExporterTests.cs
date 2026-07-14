using AnayPublisherStudio.Domain.Blocks;
using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Domain.Layout;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;
using AnayPublisherStudio.Infrastructure.Export;
using AnayPublisherStudio.Infrastructure.Integrity;
using AnayPublisherStudio.Infrastructure.Layout;
using AnayPublisherStudio.Rendering;
using AnayPublisherStudio.Typography;
using AnayPublisherStudio.Validation;
using Xunit;

namespace AnayPublisherStudio.Tests.Export;

public class ArtifactExporterTests
{
    [Fact]
    public async Task Export_Epub_PreservesAuthorText()
    {
        var book = new BookDocument();
        book.Metadata.Title = "T";
        book.Metadata.Author = "A";
        book.Chapters.Add(new Chapter
        {
            Number = 1,
            Title = "One",
            Blocks =
            {
                new HeadingBlock { Level = HeadingLevel.H1, Text = "One", Order = 0 },
                new ParagraphBlock { Order = 1, Runs = { new TextRun { Text = "Hello immutable world." } } },
            },
        });

        var project = new PublishingProject { Metadata = book.Metadata };
        var template = new PublishingTemplate { Id = "t", TrimWidth = 6, TrimHeight = 9, BodyFont = "Georgia", BodyFontSize = 11 };
        var layout = new LayoutDocument { Pages = { new ComposedPage { PageNumber = 1, Side = PageSide.Recto } } };

        var typography = new TypographyEngine();
        var professional = new ProfessionalLayoutEngine(typography, new ParagraphComposer());
        var layoutEngine = new QuestPdfLayoutEngine(typography, professional);
        var exporter = new ArtifactExporter(layoutEngine, new QuestPdfCoverEngine(), new KdpValidationEngine(), new ContentIntegrityGuard());

        var dir = Path.Combine(Path.GetTempPath(), "aps-export-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var result = await exporter.ExportAsync(project, book, template, layout,
                new[] { ExportFormat.Epub, ExportFormat.ValidationReport }, dir);

            Assert.True(result.ContainsKey(ExportFormat.Epub));
            Assert.True(File.Exists(result[ExportFormat.Epub]));
            // Author text still intact
            Assert.Equal("Hello immutable world.", ((ParagraphBlock)book.Chapters[0].Blocks[1]).PlainText);
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { /* ignore */ }
        }
    }
}
