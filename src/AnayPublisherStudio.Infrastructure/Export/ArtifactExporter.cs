using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Domain.Blocks;
using AnayPublisherStudio.Domain.Enums;
using AnayPublisherStudio.Domain.Layout;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;

namespace AnayPublisherStudio.Infrastructure.Export;

/// <summary>
/// Multi-format export engine. Presentation packaging only — author text is
/// copied verbatim into digital formats; never rewritten.
/// </summary>
public sealed class ArtifactExporter : IArtifactExporter
{
    private readonly ILayoutEngine _layout;
    private readonly ICoverEngine _cover;
    private readonly IValidationEngine _validator;
    private readonly IContentIntegrityGuard? _integrity;

    /// <summary>Creates the exporter.</summary>
    public ArtifactExporter(
        ILayoutEngine layout,
        ICoverEngine cover,
        IValidationEngine validator,
        IContentIntegrityGuard? integrity = null)
    {
        _layout = layout;
        _cover = cover;
        _validator = validator;
        _integrity = integrity;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ExportFormat> SupportedFormats { get; } = new[]
    {
        ExportFormat.PrintPdf,
        ExportFormat.DigitalPdf,
        ExportFormat.PdfX,
        ExportFormat.PdfA,
        ExportFormat.CoverPdf,
        ExportFormat.Epub,
        ExportFormat.Kindle,
        ExportFormat.Docx,
        ExportFormat.ProjectArchive,
        ExportFormat.Images,
        ExportFormat.ValidationReport,
    };

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<ExportFormat, string>> ExportAsync(
        PublishingProject project,
        BookDocument book,
        PublishingTemplate template,
        LayoutDocument layout,
        IEnumerable<ExportFormat> formats,
        string outputDirectory,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);
        var results = new Dictionary<ExportFormat, string>();
        var set = formats.Distinct().ToHashSet();

        string? expected = _integrity?.ComputeFingerprint(book);
        int pageCount = layout.PageCount > 0 ? layout.PageCount : 1;

        if (set.Contains(ExportFormat.PrintPdf) || set.Contains(ExportFormat.DigitalPdf)
            || set.Contains(ExportFormat.PdfX) || set.Contains(ExportFormat.PdfA))
        {
            var printPath = Path.Combine(outputDirectory, "interior-print.pdf");
            await using (var pdf = File.Create(printPath))
                pageCount = _layout.Render(book, template, pdf);
            results[ExportFormat.PrintPdf] = printPath;

            if (set.Contains(ExportFormat.DigitalPdf))
            {
                var dig = Path.Combine(outputDirectory, "interior-digital.pdf");
                File.Copy(printPath, dig, overwrite: true);
                results[ExportFormat.DigitalPdf] = dig;
            }
            if (set.Contains(ExportFormat.PdfX))
            {
                var x = Path.Combine(outputDirectory, "interior-pdfx.pdf");
                File.Copy(printPath, x, overwrite: true);
                results[ExportFormat.PdfX] = x;
            }
            if (set.Contains(ExportFormat.PdfA))
            {
                var a = Path.Combine(outputDirectory, "interior-pdfa.pdf");
                File.Copy(printPath, a, overwrite: true);
                results[ExportFormat.PdfA] = a;
            }
        }

        if (set.Contains(ExportFormat.CoverPdf))
        {
            var coverPath = Path.Combine(outputDirectory, "cover.pdf");
            await using (var cov = File.Create(coverPath))
                _cover.Render(project, template, pageCount, cov);
            results[ExportFormat.CoverPdf] = coverPath;
        }

        if (set.Contains(ExportFormat.Epub) || set.Contains(ExportFormat.Kindle))
        {
            var epubPath = Path.Combine(outputDirectory, "book.epub");
            await WriteEpubAsync(book, project, epubPath, ct);
            results[ExportFormat.Epub] = epubPath;
            if (set.Contains(ExportFormat.Kindle))
            {
                var kindle = Path.Combine(outputDirectory, "book-kindle.epub");
                File.Copy(epubPath, kindle, overwrite: true);
                results[ExportFormat.Kindle] = kindle;
            }
        }

        if (set.Contains(ExportFormat.Docx))
        {
            var docxPath = Path.Combine(outputDirectory, "manuscript-export.html");
            await File.WriteAllTextAsync(docxPath, BuildHtml(book), ct);
            results[ExportFormat.Docx] = docxPath;
        }

        if (set.Contains(ExportFormat.Images))
        {
            var imgDir = Path.Combine(outputDirectory, "images");
            Directory.CreateDirectory(imgDir);
            var n = 0;
            foreach (var img in book.Chapters.SelectMany(c => c.Blocks).OfType<ImageBlock>())
            {
                if (img.Data.Length == 0) continue;
                n++;
                var ext = img.ContentType.Contains("png", StringComparison.OrdinalIgnoreCase) ? "png" : "jpg";
                var path = Path.Combine(imgDir, $"image-{n:D3}.{ext}");
                await File.WriteAllBytesAsync(path, img.Data, ct);
            }
            results[ExportFormat.Images] = imgDir;
        }

        if (set.Contains(ExportFormat.ValidationReport))
        {
            var report = _validator.Validate(book, template, pageCount);
            var reportPath = Path.Combine(outputDirectory, "validation-report.json");
            await File.WriteAllTextAsync(reportPath,
                JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }), ct);
            results[ExportFormat.ValidationReport] = reportPath;
        }

        if (set.Contains(ExportFormat.ProjectArchive))
        {
            var archive = Path.Combine(outputDirectory, "project.apsproj");
            if (File.Exists(archive)) File.Delete(archive);
            using (var zip = ZipFile.Open(archive, ZipArchiveMode.Create))
            {
                var meta = zip.CreateEntry("project.json");
                await using (var s = meta.Open())
                await using (var sw = new StreamWriter(s))
                    await sw.WriteAsync(JsonSerializer.Serialize(project, new JsonSerializerOptions { WriteIndented = true }));

                if (!string.IsNullOrEmpty(project.ManuscriptPath) && File.Exists(project.ManuscriptPath))
                    zip.CreateEntryFromFile(project.ManuscriptPath, "manuscript.docx");

                foreach (var kv in results)
                {
                    if (Directory.Exists(kv.Value)) continue;
                    if (File.Exists(kv.Value))
                        zip.CreateEntryFromFile(kv.Value, "artifacts/" + Path.GetFileName(kv.Value));
                }
            }
            results[ExportFormat.ProjectArchive] = archive;
        }

        if (_integrity is not null && expected is not null)
        {
            var verify = _integrity.Verify(expected, book);
            if (!verify.IsIntact)
                throw new InvalidOperationException(
                    "Content integrity violation during export: author content was modified. " +
                    $"Expected={verify.ExpectedFingerprint}, Actual={verify.ActualFingerprint}");
        }

        return results;
    }

    private static async Task WriteEpubAsync(BookDocument book, PublishingProject project, string path, CancellationToken ct)
    {
        if (File.Exists(path)) File.Delete(path);
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);

        var mimetype = zip.CreateEntry("mimetype", CompressionLevel.NoCompression);
        await using (var s = mimetype.Open())
        {
            var bytes = Encoding.ASCII.GetBytes("application/epub+zip");
            await s.WriteAsync(bytes, ct);
        }

        var container = zip.CreateEntry("META-INF/container.xml");
        await using (var s = container.Open())
        await using (var sw = new StreamWriter(s, Encoding.UTF8))
        {
            await sw.WriteAsync(
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\">" +
                "<rootfiles><rootfile full-path=\"OEBPS/content.opf\" media-type=\"application/oebps-package+xml\"/>" +
                "</rootfiles></container>");
        }

        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.Append("<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\"><head><title>");
        sb.Append(Escape(book.Metadata.Title));
        sb.Append("</title></head><body>");
        foreach (var chapter in book.Chapters)
        {
            sb.Append("<section><h1>");
            sb.Append(Escape(chapter.Title));
            sb.Append("</h1>");
            foreach (var block in chapter.Blocks)
            {
                switch (block)
                {
                    case HeadingBlock h:
                        var level = (int)h.Level;
                        sb.Append("<h").Append(level).Append('>')
                          .Append(Escape(h.Text))
                          .Append("</h").Append(level).Append('>');
                        break;
                    case ParagraphBlock p:
                        sb.Append("<p>");
                        foreach (var run in p.Runs)
                        {
                            var t = Escape(run.Text);
                            if (run.Bold) t = "<strong>" + t + "</strong>";
                            if (run.Italic) t = "<em>" + t + "</em>";
                            if (run.Underline) t = "<u>" + t + "</u>";
                            if (!string.IsNullOrEmpty(run.Hyperlink))
                                t = "<a href=\"" + Escape(run.Hyperlink) + "\">" + t + "</a>";
                            sb.Append(t);
                        }
                        sb.Append("</p>");
                        break;
                    case TableBlock tb:
                        sb.Append("<table>");
                        foreach (var row in tb.Rows)
                        {
                            sb.Append("<tr>");
                            foreach (var cell in row)
                                sb.Append("<td>").Append(Escape(cell)).Append("</td>");
                            sb.Append("</tr>");
                        }
                        sb.Append("</table>");
                        break;
                }
            }
            sb.Append("</section>");
        }
        sb.Append("</body></html>");

        var chapterEntry = zip.CreateEntry("OEBPS/chapter.xhtml");
        await using (var s = chapterEntry.Open())
        await using (var sw = new StreamWriter(s, Encoding.UTF8))
            await sw.WriteAsync(sb.ToString());

        var opf = zip.CreateEntry("OEBPS/content.opf");
        await using (var s = opf.Open())
        await using (var sw = new StreamWriter(s, Encoding.UTF8))
        {
            var modified = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            await sw.WriteAsync(
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<package xmlns=\"http://www.idpf.org/2007/opf\" version=\"3.0\" unique-identifier=\"uid\">" +
                "<metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\">" +
                "<dc:identifier id=\"uid\">" + Escape(project.Id.ToString()) + "</dc:identifier>" +
                "<dc:title>" + Escape(book.Metadata.Title) + "</dc:title>" +
                "<dc:creator>" + Escape(book.Metadata.Author) + "</dc:creator>" +
                "<dc:language>" + Escape(book.Metadata.Language) + "</dc:language>" +
                "<meta property=\"dcterms:modified\">" + modified + "</meta>" +
                "</metadata>" +
                "<manifest>" +
                "<item id=\"chap\" href=\"chapter.xhtml\" media-type=\"application/xhtml+xml\"/>" +
                "<item id=\"nav\" href=\"nav.xhtml\" media-type=\"application/xhtml+xml\" properties=\"nav\"/>" +
                "</manifest>" +
                "<spine><itemref idref=\"chap\"/></spine></package>");
        }

        var nav = zip.CreateEntry("OEBPS/nav.xhtml");
        await using (var s = nav.Open())
        await using (var sw = new StreamWriter(s, Encoding.UTF8))
        {
            await sw.WriteAsync(
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\">" +
                "<head><title>Navigation</title></head><body>" +
                "<nav epub:type=\"toc\"><ol><li><a href=\"chapter.xhtml\">Start</a></li></ol></nav>" +
                "</body></html>");
        }
    }

    private static string BuildHtml(BookDocument book)
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"/><title>");
        sb.Append(Escape(book.Metadata.Title));
        sb.Append("</title></head><body>");
        foreach (var chapter in book.Chapters)
        {
            sb.Append("<h1>").Append(Escape(chapter.Title)).Append("</h1>");
            foreach (var block in chapter.Blocks.OfType<ParagraphBlock>())
                sb.Append("<p>").Append(Escape(block.PlainText)).Append("</p>");
        }
        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static string Escape(string? s)
        => System.Net.WebUtility.HtmlEncode(s ?? string.Empty);
}
