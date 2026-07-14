using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;
using AnayPublisherStudio.Infrastructure.Cover;
using AnayPublisherStudio.Infrastructure.Layout;
using Xunit;

namespace AnayPublisherStudio.Tests.Cover;

public class CoverDesignerTests
{
    [Fact]
    public void CreateDesign_IncludesBarcodeAndSpine()
    {
        var designer = new CoverDesigner();
        var project = new PublishingProject
        {
            Metadata = { Title = "Book", Author = "Author" },
        };
        var template = new PublishingTemplate
        {
            TrimWidth = 6, TrimHeight = 9,
            OverallWidth = 12.486, OverallHeight = 9.25,
            SpineWidth = 0.236, BarcodeWidth = 2, BarcodeHeight = 1.2,
            BleedInches = 0.125, CoverSafeZoneInches = 0.25,
            HeadingFont = "Georgia", BodyFont = "Georgia",
        };

        var design = designer.CreateDesign(project, template, 105);
        Assert.Contains(design.Layers, l => l.Kind == "barcode");
        Assert.Contains(design.Layers, l => l.Name.Contains("Spine"));
        Assert.True(design.SpineX > 0);

        designer.RecalculateSpine(design, template, 105, new KdpSpineCalculator());
        Assert.InRange(design.SpineWidth, 0.235, 0.237);

        var issues = designer.ValidateDesign(design, template);
        Assert.DoesNotContain(issues, i => i.Contains("missing", StringComparison.OrdinalIgnoreCase));
    }
}
