using System.Text;
using AnayPublisherStudio.Domain.Blocks;
using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AnayPublisherStudio.Tests.Qa;

/// <summary>Shared fixtures for QA suites. Does not alter product code.</summary>
internal static class TestFixtures
{
    public static string SolutionRoot
    {
        get
        {
            // Walk up from the test output directory until Resources/Templates is found.
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                var probe = Path.Combine(dir.FullName, "Resources", "Templates");
                if (Directory.Exists(probe))
                    return dir.FullName;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("Could not locate solution root containing Resources/Templates.");
        }
    }

    public static string TemplatesRoot => Path.Combine(SolutionRoot, "Resources", "Templates");

    public static BookDocument MakeBook(int chapters = 2, int paragraphsPerChapter = 8, string title = "QA Book", string author = "QA Author")
    {
        var book = new BookDocument
        {
            Metadata = new BookMetadata { Title = title, Author = author, Language = "en-US" },
        };

        for (var c = 1; c <= chapters; c++)
        {
            var ch = new Chapter { Number = c, Title = $"Chapter {c}" };
            ch.Blocks.Add(new HeadingBlock { Level = HeadingLevel.H1, Text = $"Chapter {c}", Order = 0 });
            for (var p = 0; p < paragraphsPerChapter; p++)
            {
                ch.Blocks.Add(new ParagraphBlock
                {
                    Order = p + 1,
                    Runs =
                    {
                        new TextRun
                        {
                            Text = $"Chapter {c} paragraph {p + 1}. " +
                                   "This sentence is intentionally long enough for line composition, " +
                                   "justification measurement, and pagination stress without rewriting author content. " +
                                   new string('x', 120),
                        },
                    },
                });
            }
            book.Chapters.Add(ch);
        }

        return book;
    }

    public static BookDocument MakeLargeBook(int targetPagesApprox)
    {
        // Roughly 3 paragraphs ~ 1 page at 6x9 / 11pt.
        var paragraphs = Math.Max(10, targetPagesApprox * 3);
        var chapters = Math.Max(1, paragraphs / 40);
        var per = Math.Max(5, paragraphs / chapters);
        return MakeBook(chapters, per, title: $"Perf {targetPagesApprox}p");
    }

    public static PublishingTemplate DefaultTemplate() => new()
    {
        Id = "qa-default",
        Name = "QA Default",
        Platform = "Amazon",
        TrimWidth = 6,
        TrimHeight = 9,
        OverallWidth = 12.486,
        OverallHeight = 9.250,
        InsideMargin = 0.6,
        OutsideMargin = 0.5,
        TopMargin = 0.75,
        BottomMargin = 0.75,
        Bleed = false,
        BleedInches = 0.125,
        MirrorMargins = true,
        SpineWidth = 0.236,
        BarcodeWidth = 2.0,
        BarcodeHeight = 1.2,
        Paper = PaperType.White,
        Color = ColorMode.BlackWhite,
        BodyFont = "Georgia",
        HeadingFont = "Georgia",
        BodyFontSize = 11,
        LineHeight = 1.35,
        FirstLineIndent = 0.25,
        MinImageDpi = 300,
        GutterByPageCount =
        {
            new GutterRule { MaxPages = 150, Inside = 0.375 },
            new GutterRule { MaxPages = 300, Inside = 0.5 },
            new GutterRule { MaxPages = 500, Inside = 0.625 },
            new GutterRule { MaxPages = 700, Inside = 0.75 },
            new GutterRule { MaxPages = 828, Inside = 0.875 },
        },
    };

    public static MemoryStream BuildDocx(
        string title = "Sample Book",
        string author = "Test Author",
        int chapters = 2,
        int paragraphsPerChapter = 3,
        bool includeTable = false,
        bool includeHyperlink = false,
        bool emptyBody = false)
    {
        var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
        {
            var main = doc.AddMainDocumentPart();
            var body = new Body();
            if (!emptyBody)
            {
                for (var c = 1; c <= chapters; c++)
                {
                    body.Append(MakeHeading($"Chapter {c}"));
                    for (var p = 0; p < paragraphsPerChapter; p++)
                    {
                        var text = $"Paragraph {p + 1} of chapter {c}. Author text must remain immutable across layout and export.";
                        if (includeHyperlink && p == 0 && c == 1)
                            body.Append(MakeHyperlinkPara(text, "https://example.com/source"));
                        else
                            body.Append(MakePara(text));
                    }
                    if (includeTable && c == 1)
                    {
                        body.Append(new Table(
                            new TableRow(new TableCell(new Paragraph(new Run(new Text("A")))), new TableCell(new Paragraph(new Run(new Text("B"))))),
                            new TableRow(new TableCell(new Paragraph(new Run(new Text("1")))), new TableCell(new Paragraph(new Run(new Text("2")))))));
                    }
                }
            }
            body.Append(new SectionProperties());
            main.Document = new Document(body);
            main.Document.Save();

            doc.PackageProperties.Title = title;
            doc.PackageProperties.Creator = author;
        }
        ms.Position = 0;
        return ms;
    }

    public static MemoryStream BuildCorruptDocx()
    {
        var ms = new MemoryStream(Encoding.UTF8.GetBytes("this is not a docx package"));
        return ms;
    }

    private static Paragraph MakeHeading(string text) => new(
        new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" }),
        new Run(new Text(text)));

    private static Paragraph MakePara(string text) => new(new Run(new Text(text)));

    private static Paragraph MakeHyperlinkPara(string text, string url)
    {
        // Simple paragraph with visible URL text (parser captures hyperlinks when present on runs).
        return new Paragraph(new Run(new Text($"{text} {url}")));
    }
}
