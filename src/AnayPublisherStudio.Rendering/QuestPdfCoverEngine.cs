using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Application.Exceptions;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AnayPublisherStudio.Rendering;

public sealed class QuestPdfCoverEngine : ICoverEngine
{
    public void Render(PublishingProject project, PublishingTemplate template, int pageCount, Stream output)
    {
        RenderingLicense.Configure();

        using var ms = new MemoryStream();

        try
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(new PageSize((float)template.OverallWidth, (float)template.OverallHeight, Unit.Inch));
                    page.Margin(0);

                    page.Content().Row(row =>
                    {
                        var spineRel = template.SpineWidth / template.OverallWidth;
                        var sideRel = (1 - spineRel) / 2;

                        row.RelativeItem((float)sideRel).Background(Colors.Grey.Lighten3)
                           .Layers(layers =>
                           {
                               layers.PrimaryLayer().Padding(18).Column(top =>
                               {
                                   top.Item().Text(project.Metadata.Title ?? "Title").FontSize(14).Bold();
                                   top.Item().PaddingTop(6)
                                      .Text(project.Metadata.Description ?? string.Empty).FontSize(9);
                               });
                               layers.Layer().Padding(18).AlignBottom().AlignRight()
                                   .Width((float)template.BarcodeWidth, Unit.Inch)
                                   .Height((float)template.BarcodeHeight, Unit.Inch)
                                   .Background(Colors.Yellow.Lighten4)
                                   .Border(1f)
                                   .BorderColor(Colors.Yellow.Darken3)
                                   .AlignMiddle().AlignCenter()
                                   .Column(c =>
                                   {
                                       c.Item().Text("BARCODE AREA").FontSize(7).FontColor(Colors.Yellow.Darken4).Bold();
                                       c.Item().Text("KDP places barcode here").FontSize(5).FontColor(Colors.Yellow.Darken3);
                                   });
                           });

                        row.RelativeItem((float)spineRel).Background(Colors.Grey.Darken2).Element(spine =>
                        {
                            if (template.SpineWidth >= 0.35)
                                spine.AlignMiddle().AlignCenter().RotateLeft()
                                     .Text(project.Metadata.Title ?? "").FontColor(Colors.White).FontSize(8);
                        });

                        row.RelativeItem((float)sideRel).Element(front =>
                        {
                            if (!string.IsNullOrEmpty(project.FrontCoverImagePath) && File.Exists(project.FrontCoverImagePath))
                                front.Image(project.FrontCoverImagePath).FitArea();
                            else
                                front.Background(Colors.Blue.Darken3).Padding(24).Column(col =>
                                {
                                    col.Item().AlignCenter().Text(project.Metadata.Title ?? "")
                                       .FontSize(28).Bold().FontColor(Colors.White);
                                    col.Item().PaddingVertical(40).AlignCenter().Text(project.Metadata.Author ?? "")
                                       .FontSize(16).FontColor(Colors.White);
                                });
                        });
                    });
                });
            }).GeneratePdf(ms);
        }
        catch (Exception ex) when (ex.Message.Contains("license", StringComparison.OrdinalIgnoreCase))
        {
            throw new PdfLicenseException(
                "Cover PDF generation failed due to QuestPDF license limits. " +
                "Falling back to DOCX output. Please upgrade to QuestPDF Professional for PDF output.", ex);
        }

        var pdfBytes = ms.ToArray();

        // Cover PDF gets XMP metadata + OutputIntent but no TrimBox (the
        // overall page dimensions already include bleed).
        PdfMetadataInjector.Inject(
            pdfBytes,
            project.Metadata.Title ?? "Untitled",
            project.Metadata.Author ?? "Unknown",
            $"Cover - {template.Platform} {template.TrimWidth}x{template.TrimHeight} {template.Paper}",
            output);
    }
}
