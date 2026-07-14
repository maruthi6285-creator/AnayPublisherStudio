using System.Text;
using AnayPublisherStudio.Domain.Blocks;
using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Wp = DocumentFormat.OpenXml.Wordprocessing;

namespace AnayPublisherStudio.Infrastructure.Export;

public static class DocxDocumentWriter
{
    public static string Write(BookDocument book, PublishingTemplate template, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(template);

        var chapters = book.Chapters ?? [];
        var metadata = book.Metadata ?? new();

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        using var doc = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
        var main = doc.AddMainDocumentPart();
        main.Document = new Document();
        var body = new Body();
        main.Document.Append(body);

        // Document settings: widow/orphan control, hyphenation
        var settingsPart = main.AddNewPart<DocumentSettingsPart>();
        settingsPart.Settings = new Settings(
            new Compatibility(new CompatibilitySetting
            {
                Name = CompatSettingNameValues.CompatibilityMode,
                Uri = "http://schemas.microsoft.com/office/word",
                Val = "15"
            }),
            new DefaultTabStop { Val = 720 }
        );
        settingsPart.Settings.Append(new WidowControl());
        settingsPart.Settings.Append(new AutoHyphenation());

        long Twip(double inches) => (long)(inches * 1440);

        int imageCounter = 0;
        int hyperlinkCounter = 0;

        SectionProperties MakeSectionProps()
        {
            var sp = new SectionProperties(
                new PageSize
                {
                    Width = (UInt32Value)(uint)Twip(template.TrimWidth),
                    Height = (UInt32Value)(uint)Twip(template.TrimHeight)
                },
                new PageMargin
                {
                    Top = (Int32Value)(int)Twip(template.TopMargin),
                    Bottom = (Int32Value)(int)Twip(template.BottomMargin),
                    Header = (UInt32Value)(uint)Twip(0.4),
                    Footer = (UInt32Value)(uint)Twip(0.4),
                    Left = (UInt32Value)(uint)Twip(template.InsideMargin),
                    Right = (UInt32Value)(uint)Twip(template.OutsideMargin)
                }
            );
            if (template.MirrorMargins)
                sp.SetAttribute(new OpenXmlAttribute("mirrorMargins", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "true"));
            return sp;
        }

        string CreateOddHeader()
        {
            var part = main.AddNewPart<HeaderPart>();
            var relId = main.GetIdOfPart(part);
            var h = new Wp.Header();
            h.Append(new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Right },
                    new SpacingBetweenLines { Before = "0", After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto }
                ),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = "Garamond", HighAnsi = "Garamond" },
                        new FontSize { Val = "18" }
                    ),
                    new Text(metadata.Title ?? "Untitled") { Space = SpaceProcessingModeValues.Preserve }
                )
            ));
            part.Header = h;
            return relId;
        }

        string CreateEvenHeader()
        {
            var part = main.AddNewPart<HeaderPart>();
            var relId = main.GetIdOfPart(part);
            var h = new Wp.Header();
            var para = new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Left },
                    new SpacingBetweenLines { Before = "0", After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto }
                )
            );
            var run = new Run(
                new RunProperties(
                    new RunFonts { Ascii = "Garamond", HighAnsi = "Garamond" },
                    new FontSize { Val = "18" }
                )
            );
            run.Append(new FieldCode(" STYLEREF \"Heading 1\" \\n \\* MERGEFORMAT "));
            para.Append(run);
            h.Append(para);
            part.Header = h;
            return relId;
        }

        string CreateFirstHeader()
        {
            var part = main.AddNewPart<HeaderPart>();
            var relId = main.GetIdOfPart(part);
            part.Header = new Wp.Header();
            return relId;
        }

        string CreateFooter()
        {
            var part = main.AddNewPart<FooterPart>();
            var relId = main.GetIdOfPart(part);
            var f = new Wp.Footer();
            var para = new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { Before = "0", After = "0" }
                )
            );
            var run = new Run(
                new RunProperties(
                    new RunFonts { Ascii = "Garamond", HighAnsi = "Garamond" },
                    new FontSize { Val = "18" }
                )
            );
            run.Append(new FieldCode(" PAGE "));
            para.Append(run);
            f.Append(para);
            part.Footer = f;
            return relId;
        }

        void RenderBlocks(IEnumerable<ContentBlock> blocks)
        {
            foreach (var block in blocks.OrderBy(b => b.Order))
            {
                switch (block)
                {
                    case PageBreakBlock:
                        body.Append(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
                        break;

                    case HeadingBlock h:
                        if (h.Level == HeadingLevel.H1) continue;
                        var hSize = h.Level switch
                        {
                            HeadingLevel.H2 => "32", HeadingLevel.H3 => "28",
                            HeadingLevel.H4 => "24", HeadingLevel.H5 => "22",
                            HeadingLevel.H6 => "20", _ => "28"
                        };
                        body.Append(new Paragraph(
                            new ParagraphProperties(
                                new SpacingBetweenLines { Before = "240", After = "120", Line = "240", LineRule = LineSpacingRuleValues.Auto },
                                new Justification { Val = JustificationValues.Left },
                                new KeepNext(), new KeepLines()
                            ),
                            new Run(
                                new RunProperties(
                                    new RunFonts { Ascii = "Garamond", HighAnsi = "Garamond" },
                                    new Bold { Val = OnOffValue.FromBoolean(h.Level <= HeadingLevel.H4) },
                                    new Italic { Val = OnOffValue.FromBoolean(h.Level == HeadingLevel.H4) },
                                    new FontSize { Val = hSize }, new FontSizeComplexScript { Val = hSize }
                                ),
                                new Text(h.Text ?? "") { Space = SpaceProcessingModeValues.Preserve }
                            )
                        ));
                        break;

                    case ParagraphBlock p:
                        var pPara = new Paragraph();
                        pPara.ParagraphProperties = new ParagraphProperties(
                            new SpacingBetweenLines { Before = "0", After = "0", Line = "276", LineRule = LineSpacingRuleValues.Auto },
                            new Justification { Val = MapAlignment(p.Alignment) },
                            new Indentation { FirstLine = Twip(0.25).ToString() }
                        );
                        if (p.IsQuote)
                            pPara.ParagraphProperties.Append(new Indentation { Left = Twip(0.5).ToString(), Right = Twip(0.5).ToString() });

                        foreach (var run in (p.Runs ?? []))
                        {
                            var rProps = new RunProperties(
                                new RunFonts { Ascii = "Garamond", HighAnsi = "Garamond" },
                                new FontSize { Val = "22" }, new FontSizeComplexScript { Val = "22" }
                            );
                            if (run.Bold) rProps.Append(new Bold());
                            if (run.Italic) rProps.Append(new Italic());
                            if (run.Underline) rProps.Append(new Underline { Val = UnderlineValues.Single });

                            if (!string.IsNullOrEmpty(run.Hyperlink) && Uri.TryCreate(run.Hyperlink, UriKind.Absolute, out var hlUri))
                            {
                                hyperlinkCounter++;
                                var hlRelId = $"hl{hyperlinkCounter}";
                                main.AddHyperlinkRelationship(hlUri, true, hlRelId);
                                pPara.Append(new Hyperlink(
                                    new Run(rProps, new Text(run.Text ?? "") { Space = SpaceProcessingModeValues.Preserve })
                                ) { Id = hlRelId });
                            }
                            else
                            {
                                pPara.Append(new Run(rProps, new Text(run.Text ?? "") { Space = SpaceProcessingModeValues.Preserve }));
                            }
                        }
                        body.Append(pPara);
                        break;

                    case ImageBlock img when img.Data is { Length: > 0 }:
                        imageCounter++;
                        var imgPart = main.AddImagePart(
                            (img.ContentType ?? "image/png").Contains("png", StringComparison.OrdinalIgnoreCase)
                                ? ImagePartType.Png : ImagePartType.Jpeg
                        );
                        imgPart.FeedData(new MemoryStream(img.Data));
                        var imgRelId = main.GetIdOfPart(imgPart);
                        long emuW = img.PixelWidth > 0 ? (long)(img.PixelWidth * 914400L / (img.Dpi > 0 ? img.Dpi : 72)) : 4000000;
                        long emuH = img.PixelHeight > 0 ? (long)(img.PixelHeight * 914400L / (img.Dpi > 0 ? img.Dpi : 72)) : 3000000;
                        body.Append(new Paragraph(
                            new ParagraphProperties(new SpacingBetweenLines { Before = "120", After = "120" }, new Justification { Val = JustificationValues.Center }),
                            new Drawing(new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(
                                new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent { Cx = emuW, Cy = emuH },
                                new DocumentFormat.OpenXml.Drawing.Wordprocessing.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                                new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties { Id = (UInt32Value)(uint)imageCounter, Name = $"Image{imageCounter}" },
                                new DocumentFormat.OpenXml.Drawing.Graphic(new DocumentFormat.OpenXml.Drawing.GraphicData(
                                    new DocumentFormat.OpenXml.Drawing.Pictures.Picture(
                                        new DocumentFormat.OpenXml.Drawing.Pictures.BlipFill(new DocumentFormat.OpenXml.Drawing.Blip { Embed = imgRelId, CompressionState = DocumentFormat.OpenXml.Drawing.BlipCompressionValues.Print }),
                                        new DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties(
                                            new DocumentFormat.OpenXml.Drawing.Transform2D(new DocumentFormat.OpenXml.Drawing.Offset { X = 0L, Y = 0L }, new DocumentFormat.OpenXml.Drawing.Extents { Cx = emuW, Cy = emuH }),
                                            new DocumentFormat.OpenXml.Drawing.PresetGeometry { Preset = DocumentFormat.OpenXml.Drawing.ShapeTypeValues.Rectangle })
                                    )
                                ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                            ) { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U })
                        ));
                        if (!string.IsNullOrWhiteSpace(img.Caption))
                            body.Append(new Paragraph(
                                new ParagraphProperties(new SpacingBetweenLines { Before = "60", After = "120" }, new Justification { Val = JustificationValues.Center }),
                                new Run(new RunProperties(new RunFonts { Ascii = "Garamond", HighAnsi = "Garamond" }, new Italic(), new FontSize { Val = "20" }, new FontSizeComplexScript { Val = "20" }),
                                    new Text(img.Caption) { Space = SpaceProcessingModeValues.Preserve })
                            ));
                        break;

                    case TableBlock tb when tb.Rows is { Count: > 0 }:
                        var table = new Table();
                        table.Append(new TableProperties(
                            new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
                            new TableBorders(
                                new TopBorder { Val = BorderValues.Single, Size = 6 },
                                new BottomBorder { Val = BorderValues.Single, Size = 6 },
                                new LeftBorder { Val = BorderValues.Single, Size = 6 },
                                new RightBorder { Val = BorderValues.Single, Size = 6 },
                                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 6 },
                                new InsideVerticalBorder { Val = BorderValues.Single, Size = 6 }
                            ),
                            new TableLook { Val = "04A0" }
                        ));
                        foreach (var row in (tb.Rows ?? []))
                        {
                            var tr = new TableRow();
                            foreach (var cell in (row ?? []))
                                tr.Append(new TableCell(
                                    new TableCellProperties(new Shading { Val = ShadingPatternValues.Clear, Fill = "auto" }),
                                    new Paragraph(new Run(
                                        new RunProperties(new RunFonts { Ascii = "Garamond", HighAnsi = "Garamond" }, new FontSize { Val = "22" }, new FontSizeComplexScript { Val = "22" }),
                                        new Text(cell ?? "") { Space = SpaceProcessingModeValues.Preserve }))
                                ));
                            table.Append(tr);
                        }
                        body.Append(table);
                        body.Append(new Paragraph(new SpacingBetweenLines { After = "120" }));
                        break;
                }
            }
        }

        bool hasBodyChapters = chapters.Any(c => c.Number >= 1);

        if (!hasBodyChapters && chapters.Count >= 1)
        {
            var oddHdrId = CreateOddHeader();
            var evenHdrId = CreateEvenHeader();
            var firstHdrId = CreateFirstHeader();
            var ftrId = CreateFooter();

            foreach (var chapter in chapters)
                RenderBlocks(chapter.Blocks ?? []);

            var sp = MakeSectionProps();
            sp.Append(new HeaderReference { Type = HeaderFooterValues.Default, Id = oddHdrId });
            sp.Append(new HeaderReference { Type = HeaderFooterValues.Even, Id = evenHdrId });
            sp.Append(new HeaderReference { Type = HeaderFooterValues.First, Id = firstHdrId });
            sp.Append(new FooterReference { Type = HeaderFooterValues.Default, Id = ftrId });
            body.Append(sp);
            main.Document.Save();
            return outputPath;
        }

        foreach (var chapter in chapters)
        {
            bool isBodyChapter = chapter.Number >= 1;

            if (isBodyChapter && !body.Elements<SectionProperties>().Any(sp => sp.HasChildren))
                body.Append(MakeSectionProps());

            if (isBodyChapter)
            {
                body.Append(new Paragraph(
                    new ParagraphProperties(
                        new SpacingBetweenLines { Before = "2160", After = "480", Line = "240", LineRule = LineSpacingRuleValues.Auto },
                        new Justification { Val = JustificationValues.Center },
                        new KeepNext(), new KeepLines()
                    ),
                    new Run(
                        new RunProperties(
                            new RunFonts { Ascii = "Garamond", HighAnsi = "Garamond" },
                            new Bold(), new FontSize { Val = "44" }, new FontSizeComplexScript { Val = "44" }, new Color { Val = "000000" }
                        ),
                        new Text(chapter.Title ?? $"Chapter {chapter.Number}") { Space = SpaceProcessingModeValues.Preserve }
                    )
                ));
            }

            RenderBlocks(chapter.Blocks ?? []);

            if (chapter != chapters.Last())
                body.Append(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
        }

        var oddHdrId2 = CreateOddHeader();
        var evenHdrId2 = CreateEvenHeader();
        var firstHdrId2 = CreateFirstHeader();
        var ftrId2 = CreateFooter();
        var finalSp = MakeSectionProps();
        finalSp.Append(new HeaderReference { Type = HeaderFooterValues.Default, Id = oddHdrId2 });
        finalSp.Append(new HeaderReference { Type = HeaderFooterValues.Even, Id = evenHdrId2 });
        finalSp.Append(new HeaderReference { Type = HeaderFooterValues.First, Id = firstHdrId2 });
        finalSp.Append(new FooterReference { Type = HeaderFooterValues.Default, Id = ftrId2 });
        body.Append(finalSp);
        main.Document.Save();
        return outputPath;
    }

    public static string ApplyPageSettings(string sourcePath, string outputPath, double widthInches, double heightInches,
        double topMargin, double bottomMargin, double insideMargin, double outsideMargin, bool mirrorMargins)
    {
        ArgumentNullException.ThrowIfNull(sourcePath);
        ArgumentNullException.ThrowIfNull(outputPath);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.Copy(sourcePath, outputPath, overwrite: true);

        using var doc = WordprocessingDocument.Open(outputPath, isEditable: true);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null) return outputPath;

        long Twip(double inches) => (long)(inches * 1440);

        var sectionProps = body.Elements<SectionProperties>().FirstOrDefault();
        if (sectionProps is null)
        {
            sectionProps = new SectionProperties();
            body.Append(sectionProps);
        }

        var pageSize = sectionProps.Elements<PageSize>().FirstOrDefault();
        if (pageSize is null)
        {
            pageSize = new PageSize();
            sectionProps.PrependChild(pageSize);
        }
        pageSize.Width = (UInt32Value)(uint)Twip(widthInches);
        pageSize.Height = (UInt32Value)(uint)Twip(heightInches);

        var pageMargin = sectionProps.Elements<PageMargin>().FirstOrDefault();
        if (pageMargin is null)
        {
            pageMargin = new PageMargin();
            sectionProps.Append(pageMargin);
        }
        pageMargin.Top = (Int32Value)(int)Twip(topMargin);
        pageMargin.Bottom = (Int32Value)(int)Twip(bottomMargin);
        pageMargin.Header = (UInt32Value)(uint)Twip(0.4);
        pageMargin.Footer = (UInt32Value)(uint)Twip(0.4);
        pageMargin.Left = (UInt32Value)(uint)Twip(insideMargin);
        pageMargin.Right = (UInt32Value)(uint)Twip(outsideMargin);

        if (mirrorMargins)
            sectionProps.SetAttribute(new OpenXmlAttribute("mirrorMargins", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "true"));

        doc.Save();
        return outputPath;
    }

    public static string Write(BookDocument book, string outputPath)
    {
        // Fallback overload: uses default KDP 6x9 template values
        var tpl = new PublishingTemplate
        {
            TrimWidth = 6,
            TrimHeight = 9,
            TopMargin = 0.75,
            BottomMargin = 0.75,
            InsideMargin = 0.625,
            OutsideMargin = 0.5,
            MirrorMargins = true,
            BodyFont = "Garamond",
            HeadingFont = "Garamond",
            BodyFontSize = 11,
            LineHeight = 1.15,
            FirstLineIndent = 0.25
        };
        return Write(book, tpl, outputPath);
    }

    private static JustificationValues MapAlignment(Domain.Enums.TextAlignment alignment) => alignment switch
    {
        Domain.Enums.TextAlignment.Left => JustificationValues.Left,
        Domain.Enums.TextAlignment.Center => JustificationValues.Center,
        Domain.Enums.TextAlignment.Right => JustificationValues.Right,
        Domain.Enums.TextAlignment.Justify => JustificationValues.Both,
        _ => JustificationValues.Both
    };
}
