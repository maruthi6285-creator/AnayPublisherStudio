using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Application.Configuration;
using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Domain.ValueObjects;
using Microsoft.Extensions.Options;
using DomainTypography = AnayPublisherStudio.Domain.ValueObjects.Typography;

namespace AnayPublisherStudio.Typography;

/// <summary>
/// Professional typography engine. Turns a data-driven <see cref="PublishingTemplate"/>
/// into resolved, presentation-only <see cref="DomainTypography"/> for each
/// content role (body, heading, caption, footnote, running matter).
/// </summary>
/// <remarks>
/// Every method is a pure function of the template and the requested role. The
/// engine never receives or returns author text, so it structurally cannot
/// modify content — it only describes how content should be presented.
/// Supports OpenType flags (ligatures, kerning, true small caps), tracking,
/// leading, optical margin alignment, hanging punctuation, drop caps,
/// professional justification ranges, hyphenation, font fallback, and embedded fonts.
/// </remarks>
public sealed class TypographyEngine : ITypographyEngine
{
    private readonly TypographySettings _settings;

    /// <summary>Creates a TypographyEngine with optional configuration.</summary>
    public TypographyEngine(IOptions<TypographySettings>? settings = null)
    {
        _settings = settings?.Value ?? new TypographySettings();
    }

    /// <summary>Number of lines a chapter-opening drop cap spans.</summary>
    public int DropCapLines => _settings.DropCapLines;

    /// <summary>Default font fallback chain.</summary>
    public IReadOnlyList<string> DefaultFallback => _settings.DefaultFontFallback.ToArray();

    /// <inheritdoc/>
    public DomainTypography ResolveBody(PublishingTemplate template, bool isChapterOpening) => new()
    {
        FontFamily = template.BodyFont,
        FontSizePoints = template.BodyFontSize,
        LineHeight = template.LineHeight,
        FirstLineIndentInches = isChapterOpening ? 0 : template.FirstLineIndent,
        DropCapLines = isChapterOpening ? DropCapLines : 0,
        Ligatures = _settings.EnableLigatures,
        Kerning = _settings.EnableKerning,
        OpticalAlignment = _settings.EnableOpticalAlignment,
        HangingPunctuation = _settings.EnableHangingPunctuation,
        SmallCaps = false,
        TrueSmallCaps = false,
        Bold = false,
        Hyphenation = _settings.EnableHyphenation,
        WordSpacing = 1.0,
        MinWordSpacing = _settings.MinWordSpacing,
        MaxWordSpacing = _settings.MaxWordSpacing,
        Language = _settings.DefaultLanguage,
        FontFallback = DefaultFallback,
    };

    /// <inheritdoc/>
    public DomainTypography ResolveHeading(PublishingTemplate template, HeadingLevel level)
    {
        var baseSize = template.BodyFontSize;
        var sizeByLevel = level switch
        {
            HeadingLevel.H1 => baseSize * 2.0,      // 22.0 pt
            HeadingLevel.H2 => baseSize * 1.45,     // 16.0 pt
            HeadingLevel.H3 => baseSize * 1.27,     // 14.0 pt
            HeadingLevel.H4 => baseSize * 1.09,     // 12.0 pt
            HeadingLevel.H5 => baseSize * 1.0,      // 11.0 pt
            _ => baseSize * 1.0,
        };
        return new DomainTypography
        {
            FontFamily = template.HeadingFont,
            FontSizePoints = Math.Round(sizeByLevel, 1),
            LineHeight = 1.15,
            FirstLineIndentInches = 0,
            Ligatures = _settings.EnableLigatures,
            Kerning = _settings.EnableKerning,
            SmallCaps = level == HeadingLevel.H1,
            TrueSmallCaps = level == HeadingLevel.H1,
            Bold = level <= HeadingLevel.H3,
            OpticalAlignment = false,
            HangingPunctuation = false,
            Hyphenation = false,
            DropCapLines = 0,
            FontFallback = DefaultFallback,
        };
    }

    /// <inheritdoc/>
    public DomainTypography ResolveCaption(PublishingTemplate template) => new()
    {
        FontFamily = template.BodyFont,
        FontSizePoints = Math.Max(8, template.BodyFontSize - 2),
        LineHeight = 1.2,
        FirstLineIndentInches = 0,
        Ligatures = _settings.EnableLigatures,
        Kerning = _settings.EnableKerning,
        SmallCaps = false,
        Bold = false,
        OpticalAlignment = false,
        Hyphenation = false,
        DropCapLines = 0,
        FontFallback = DefaultFallback,
    };
}
