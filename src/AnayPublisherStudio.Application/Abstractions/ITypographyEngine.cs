using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Domain.ValueObjects;

namespace AnayPublisherStudio.Application.Abstractions;

/// <summary>
/// Resolves presentation-only <see cref="Typography"/> for content, driven by
/// the active template. It is a pure function of (template, block role): it
/// receives no author text and returns no author text, guaranteeing it can
/// never modify content.
/// </summary>
public interface ITypographyEngine
{
    /// <summary>Typography for a body paragraph.</summary>
    /// <param name="template">Active publishing template.</param>
    /// <param name="isChapterOpening">
    /// True for the first body paragraph after a chapter (H1) heading, which
    /// suppresses the first-line indent and may carry a drop cap.
    /// </param>
    Typography ResolveBody(PublishingTemplate template, bool isChapterOpening);

    /// <summary>Typography for a heading of the given level.</summary>
    Typography ResolveHeading(PublishingTemplate template, HeadingLevel level);

    /// <summary>Typography for a caption line.</summary>
    Typography ResolveCaption(PublishingTemplate template);

    /// <summary>Typography for footnotes (presentation metrics only).</summary>
    Typography ResolveFootnote(PublishingTemplate template) => ResolveCaption(template) with
    {
        FontSizePoints = Math.Max(8, template.BodyFontSize - 3),
        LineHeight = 1.15,
        FirstLineIndentInches = 0,
        DropCapLines = 0,
    };

    /// <summary>Typography for running headers/footers.</summary>
    Typography ResolveRunningMatter(PublishingTemplate template) => new()
    {
        FontFamily = template.BodyFont,
        FontSizePoints = template.Composition.RunningMatter.FontSizePoints,
        LineHeight = 1.1,
        Ligatures = true,
        Kerning = true,
    };
}
