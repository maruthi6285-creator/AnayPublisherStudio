using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Domain.Cover;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;

namespace AnayPublisherStudio.Infrastructure.Cover;

/// <summary>
/// Professional cover designer: front, back, spine, barcode area, bleed,
/// live area, layers, guides, safe zones, automatic spine calculation.
/// </summary>
public sealed class CoverDesigner : ICoverDesigner
{
    /// <inheritdoc/>
    public CoverDesign CreateDesign(PublishingProject project, PublishingTemplate template, int pageCount)
    {
        var design = new CoverDesign
        {
            TrimWidth = template.TrimWidth,
            TrimHeight = template.TrimHeight,
            SpineWidth = template.SpineWidth,
            BleedInches = template.BleedInches > 0 ? template.BleedInches : 0.125,
            SafeZoneInches = template.CoverSafeZoneInches,
            BarcodeWidth = template.BarcodeWidth,
            BarcodeHeight = template.BarcodeHeight,
            OverallWidth = template.OverallWidth,
            OverallHeight = template.OverallHeight,
        };

        // Layers: background, front title, author, spine text, barcode reservation, author image.
        design.Layers.Add(new CoverLayer
        {
            Name = "Background",
            Kind = "shape",
            XInches = 0,
            YInches = 0,
            WidthInches = design.OverallWidth,
            HeightInches = design.OverallHeight,
            ZIndex = 0,
        });

        design.Layers.Add(new CoverLayer
        {
            Name = "Front Title",
            Kind = "text",
            Text = project.Metadata.Title,
            FontFamily = template.HeadingFont,
            FontSizePoints = 28,
            XInches = design.FrontX + design.SafeZoneInches,
            YInches = design.OverallHeight * 0.35,
            WidthInches = design.TrimWidth - 2 * design.SafeZoneInches,
            HeightInches = 1.0,
            ZIndex = 10,
        });

        design.Layers.Add(new CoverLayer
        {
            Name = "Front Author",
            Kind = "text",
            Text = project.Metadata.Author,
            FontFamily = template.BodyFont,
            FontSizePoints = 16,
            XInches = design.FrontX + design.SafeZoneInches,
            YInches = design.OverallHeight * 0.55,
            WidthInches = design.TrimWidth - 2 * design.SafeZoneInches,
            HeightInches = 0.5,
            ZIndex = 11,
        });

        if (!string.IsNullOrEmpty(project.FrontCoverImagePath))
        {
            design.Layers.Add(new CoverLayer
            {
                Name = "Front Image",
                Kind = "image",
                ImagePath = project.FrontCoverImagePath,
                XInches = design.FrontX,
                YInches = design.BleedInches,
                WidthInches = design.TrimWidth,
                HeightInches = design.TrimHeight,
                ZIndex = 5,
            });
        }

        if (!string.IsNullOrEmpty(project.AuthorImagePath))
        {
            design.Layers.Add(new CoverLayer
            {
                Name = "Author Photo",
                Kind = "image",
                ImagePath = project.AuthorImagePath,
                XInches = design.SafeZoneInches,
                YInches = design.OverallHeight - 2.5,
                WidthInches = 1.5,
                HeightInches = 1.5,
                ZIndex = 12,
            });
        }

        if (!string.IsNullOrEmpty(project.BackCoverImagePath))
        {
            design.Layers.Add(new CoverLayer
            {
                Name = "Back Image",
                Kind = "image",
                ImagePath = project.BackCoverImagePath,
                XInches = design.BleedInches,
                YInches = design.BleedInches,
                WidthInches = design.TrimWidth,
                HeightInches = design.TrimHeight,
                ZIndex = 5,
            });
        }

        design.Layers.Add(new CoverLayer
        {
            Name = "Spine Text",
            Kind = "text",
            Text = project.Metadata.Title,
            FontFamily = template.BodyFont,
            FontSizePoints = 8,
            XInches = design.SpineX,
            YInches = design.SafeZoneInches,
            WidthInches = design.SpineWidth,
            HeightInches = design.TrimHeight - 2 * design.SafeZoneInches,
            ZIndex = 15,
        });

        // Automatic barcode reservation (bottom-right of back cover).
        design.Layers.Add(new CoverLayer
        {
            Name = "Barcode Area",
            Kind = "barcode",
            XInches = design.TrimWidth - design.BarcodeWidth - design.SafeZoneInches,
            YInches = design.OverallHeight - design.BarcodeHeight - design.SafeZoneInches,
            WidthInches = design.BarcodeWidth,
            HeightInches = design.BarcodeHeight,
            ZIndex = 20,
            Locked = true,
        });

        // Guides (snap targets): spine edges, safe zones, trim.
        design.Layers.Add(new CoverLayer { Name = "Guide: Spine Left", Kind = "guide", XInches = design.SpineX, YInches = 0, WidthInches = 0, HeightInches = design.OverallHeight, ZIndex = 100, Locked = true });
        design.Layers.Add(new CoverLayer { Name = "Guide: Spine Right", Kind = "guide", XInches = design.FrontX, YInches = 0, WidthInches = 0, HeightInches = design.OverallHeight, ZIndex = 100, Locked = true });

        return design;
    }

    /// <inheritdoc/>
    public void RecalculateSpine(CoverDesign design, PublishingTemplate template, int pageCount, ISpineCalculator spine)
    {
        design.SpineWidth = spine.CalculateInches(pageCount, template.Paper, template.Color);
        // Overall width = back + spine + front + 2*bleed (template may already include).
        var sides = template.TrimWidth * 2 + design.SpineWidth + 2 * design.BleedInches;
        design.OverallWidth = Math.Max(template.OverallWidth, sides);
        design.OverallHeight = Math.Max(template.OverallHeight, template.TrimHeight + 2 * design.BleedInches);
        design.TrimWidth = template.TrimWidth;
        design.TrimHeight = template.TrimHeight;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> ValidateDesign(CoverDesign design, PublishingTemplate template)
    {
        var issues = new List<string>();
        if (design.SpineWidth < 0.06)
            issues.Add("Spine width is below practical minimum (0.06in).");
        if (design.BleedInches < 0.125)
            issues.Add("Bleed is below the 0.125in industry minimum.");
        if (design.BarcodeWidth < 1.5 || design.BarcodeHeight < 1.0)
            issues.Add("Barcode reservation is smaller than typical KDP requirements.");
        if (design.OverallWidth < design.TrimWidth * 2 + design.SpineWidth)
            issues.Add("Overall cover width is smaller than back+spine+front.");
        var barcode = design.Layers.FirstOrDefault(l => l.Kind == "barcode");
        if (barcode is null)
            issues.Add("Barcode area layer is missing.");
        else if (barcode.XInches + barcode.WidthInches > design.TrimWidth)
            issues.Add("Barcode area intersects the spine or front cover.");
        return issues;
    }
}
