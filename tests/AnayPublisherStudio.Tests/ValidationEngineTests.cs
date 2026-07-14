using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;
using AnayPublisherStudio.Validation;
using Xunit;

namespace AnayPublisherStudio.Tests;

public class ValidationEngineTests
{
    private static BookDocument SampleBook() => new()
    {
        Metadata = new BookMetadata { Title = "T", Author = "A" },
        Chapters = { new Chapter { Number = 1, Title = "One",
            Blocks = { new Domain.Blocks.ParagraphBlock { Runs = { new TextRun { Text = "hi" } } } } } }
    };

    [Fact]
    public void ShortBook_FailsPageCount()
    {
        var report = new KdpValidationEngine().Validate(SampleBook(), new PublishingTemplate(), pageCount: 5);
        Assert.False(report.IsPublishable);
        Assert.Contains(report.Findings, f => f.Check == "PageCount");
    }

    [Fact]
    public void MissingTitle_IsError()
    {
        var book = SampleBook();
        book.Metadata.Title = "";
        var report = new KdpValidationEngine().Validate(book, new PublishingTemplate(), pageCount: 50);
        Assert.Contains(report.Findings, f => f.Check == "Metadata.Title");
    }

    [Fact]
    public void ValidBook_IsPublishable()
    {
        var report = new KdpValidationEngine().Validate(SampleBook(), new PublishingTemplate(), pageCount: 120);
        Assert.True(report.IsPublishable);
    }
}
