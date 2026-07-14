using System.Security.Cryptography;
using System.Text;
using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Application.Integrity;
using AnayPublisherStudio.Domain.Blocks;
using AnayPublisherStudio.Domain.Model;

namespace AnayPublisherStudio.Infrastructure.Integrity;

/// <summary>
/// Default <see cref="IContentIntegrityGuard"/>. Serialises author content into
/// a canonical, order-preserving byte stream and hashes it with SHA-256.
/// </summary>
/// <remarks>
/// The canonical form intentionally covers ONLY author-owned content —
/// chapter order and titles, block order and kind, run text and character emphasis,
/// hyperlinks, footnote references, table cells, footnotes, endnotes, and image bytes.
/// Presentation and generated artefacts (typography, margins, page numbers,
/// the table of contents) are excluded so that legitimate presentation work
/// never trips the guard, while any change to wording, ordering, structure,
/// tables, footnotes or images is detected immediately.
/// </remarks>
public sealed class ContentIntegrityGuard : IContentIntegrityGuard
{
    /// <inheritdoc/>
    public string ComputeFingerprint(BookDocument book)
    {
        using var sha = SHA256.Create();
        using var cs = new CryptoStream(Stream.Null, sha, CryptoStreamMode.Write);
        using (var w = new StreamWriter(cs, new UTF8Encoding(false), 4096, leaveOpen: true))
            WriteCanonical(w, book);
        cs.FlushFinalBlock();
        return Convert.ToHexString(sha.Hash!);
    }

    /// <inheritdoc/>
    public ContentIntegrityResult Verify(string expectedFingerprint, BookDocument book)
    {
        var actual = ComputeFingerprint(book);
        return new ContentIntegrityResult
        {
            ExpectedFingerprint = expectedFingerprint,
            ActualFingerprint = actual,
            IsIntact = string.Equals(expectedFingerprint, actual, StringComparison.Ordinal),
        };
    }

    private static void WriteCanonical(TextWriter w, BookDocument book)
    {
        for (var ci = 0; ci < book.Chapters.Count; ci++)
        {
            var c = book.Chapters[ci];
            w.Write("C\u0001"); w.Write(ci); w.Write('\u0001');
            w.Write(c.Number); w.Write('\u0001'); w.Write(c.Title); w.Write('\u0002');

            for (var bi = 0; bi < c.Blocks.Count; bi++)
                WriteBlock(w, bi, c.Blocks[bi]);
        }

        foreach (var fn in book.Footnotes)
        {
            w.Write("F\u0001"); w.Write(fn.Id); w.Write('\u0001'); w.Write(fn.Text); w.Write('\u0002');
        }

        foreach (var en in book.Endnotes)
        {
            w.Write("E\u0001"); w.Write(en.Id); w.Write('\u0001'); w.Write(en.Text); w.Write('\u0002');
        }
    }

    private static void WriteBlock(TextWriter w, int index, ContentBlock block)
    {
        w.Write("B\u0001"); w.Write(index); w.Write('\u0001'); w.Write((int)block.Type); w.Write('\u0001');
        switch (block)
        {
            case HeadingBlock h:
                w.Write((int)h.Level); w.Write('\u0001'); w.Write(h.Text);
                break;
            case ParagraphBlock p:
                w.Write(p.IsQuote ? '1' : '0'); w.Write('\u0001');
                foreach (var r in p.Runs)
                {
                    w.Write(r.Text); w.Write('\u0003');
                    w.Write(r.Bold ? 'b' : '-');
                    w.Write(r.Italic ? 'i' : '-');
                    w.Write(r.Underline ? 'u' : '-');
                    w.Write('\u0003'); w.Write(r.Hyperlink ?? "");
                    w.Write('\u0003'); w.Write(r.FootnoteRef ?? "");
                    w.Write('\u0004');
                }
                break;
            case ImageBlock img:
                w.Write(img.ContentType); w.Write('\u0001');
                w.Write(img.PixelWidth); w.Write('x'); w.Write(img.PixelHeight); w.Write('\u0001');
                w.Write(img.Data.Length); w.Write('\u0001');
                // Hash full image bytes so same-length substitutions are detected.
                w.Write(Convert.ToHexString(SHA256.HashData(img.Data ?? Array.Empty<byte>())));
                w.Write('\u0001'); w.Write(img.Caption ?? "");
                break;
            case TableBlock t:
                foreach (var row in t.Rows)
                {
                    foreach (var cell in row) { w.Write(cell); w.Write('\u0003'); }
                    w.Write('\u0004');
                }
                break;
        }
        w.Write('\u0002');
    }
}
