using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

// Generates Resources/Sample/manuscript.docx with 24+ pages for KDP validation.
// ~3 paragraphs per page at 6x9/11pt → 80 paragraphs ≈ 27 pages.

var root = FindSolutionRoot();
var outputPath = Path.Combine(root, "Resources", "Sample", "manuscript.docx");
Console.WriteLine($"Generating {outputPath}...");

using var doc = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document, autoSave: true);
var main = doc.AddMainDocumentPart();
var body = new Body();

var chapters = new (string Title, int Paragraphs)[]
{
    ("Introduction", 12),
    ("The Beginning", 12),
    ("A New Discovery", 10),
    ("Challenges Arise", 10),
    ("The Turning Point", 10),
    ("Unraveling the Mystery", 10),
    ("Confrontation", 10),
    ("Resolution", 10),
};

var lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum. ";

foreach (var (title, paraCount) in chapters)
{
    body.Append(new Paragraph(
        new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" }),
        new Run(new Text(title))));

    for (var i = 0; i < paraCount; i++)
    {
        var text = $"{lorem}{lorem}This is paragraph {i + 1} of {title}. It contains enough text to fill approximately one-third of a page at standard book dimensions, ensuring the manuscript reaches the required minimum page count for publishing platforms like KDP and IngramSpark.";
        body.Append(new Paragraph(new Run(new Text(text))));
    }
}

body.Append(new SectionProperties(
    new PageSize { Width = 12240U, Height = 15840U }, // 8.5x11in in twips
    new PageMargin { Top = 1440, Bottom = 1440, Left = 1440, Right = 1440 }));

main.Document = new Document(body);
main.Document.Save();

doc.PackageProperties.Title = "Sample Book: A Complete Story";
doc.PackageProperties.Creator = "Anay Publisher Studio";
doc.PackageProperties.Description = "A sample manuscript generated for testing the Anay Publisher Studio publishing pipeline. Contains 8 chapters with sufficient content to produce a 24+ page book.";
doc.PackageProperties.Language = "en-US";

Console.WriteLine($"Done! Generated {outputPath}");
Console.WriteLine($"Chapters: {chapters.Length}, Total paragraphs: {chapters.Sum(c => c.Paragraphs)}");
Console.WriteLine($"Estimated pages: ~{chapters.Sum(c => c.Paragraphs) / 3}");

static string FindSolutionRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "AnayPublisherStudio.sln")))
            return dir.FullName;
        dir = dir.Parent;
    }
    throw new DirectoryNotFoundException("Could not find solution root.");
}
