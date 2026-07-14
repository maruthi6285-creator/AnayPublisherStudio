using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AnayPublisherStudio.Rendering;

/// <summary>
/// Builds a single-page wraparound cover PDF laid out back-cover | spine |
/// front-cover at the template's overall dimensions. Honours the barcode safe
/// area on the lower-right of the back cover.
/// </summary>
public sealed class QuestPdfCoverEngine : ICoverEngine
{
    /// <inheritdoc/>
    public void Render(PublishingProject project, PublishingTemplate template, int pageCount, Stream output)
    {
        RenderingLicense.Configure();

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

                    // Back cover. A layered panel keeps the barcode anchored at the
                    // KDP lower-right without an Extend() spacer that can overflow.
                    row.RelativeItem((float)sideRel).Background(Colors.Grey.Lighten3)
                       .Layers(layers =>
                       {
                           layers.PrimaryLayer().Padding(18).Column(top =>
                           {
                               top.Item().Text(project.Metadata.Title).FontSize(14).Bold();
                               top.Item().PaddingTop(6)
                                  .Text(project.Metadata.Description ?? string.Empty).FontSize(9);
                           });
                           layers.Layer().Padding(18).AlignBottom().AlignRight()
                              .Width((float)template.BarcodeWidth, Unit.Inch)
                              .Height((float)template.BarcodeHeight, Unit.Inch)
                              .Background(Colors.White).Border(0.5f)
                              .AlignMiddle().AlignCenter()
                              .Text("BARCODE AREA").FontSize(7).FontColor(Colors.Grey.Medium);
                       });

                    // Spine. KDP only permits spine text on spines >= 0.35in
                    // (~100+ pages); thinner spines are filled with colour only.
                    row.RelativeItem((float)spineRel).Background(Colors.Grey.Darken2).Element(spine =>
                    {
                        if (template.SpineWidth >= 0.35)
                            spine.AlignMiddle().AlignCenter().RotateLeft()
                                 .Text(project.Metadata.Title).FontColor(Colors.White).FontSize(8);
                    });

                    // Front cover.
                    row.RelativeItem((float)sideRel).Element(front =>
                    {
                        if (!string.IsNullOrEmpty(project.FrontCoverImagePath) && File.Exists(project.FrontCoverImagePath))
                            front.Image(project.FrontCoverImagePath).FitArea();
                        else
                            front.Background(Colors.Blue.Darken3).Padding(24).Column(col =>
                            {
                                col.Item().AlignCenter().Text(project.Metadata.Title)
                                   .FontSize(28).Bold().FontColor(Colors.White);
                                col.Item().PaddingVertical(40).AlignCenter().Text(project.Metadata.Author)
                                   .FontSize(16).FontColor(Colors.White);
                            });
                    });
                });
            });
        }).GeneratePdf(output);
    }
}
