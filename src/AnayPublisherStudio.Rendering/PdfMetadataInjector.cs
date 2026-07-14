using System.Text;
using System.Text.RegularExpressions;

namespace AnayPublisherStudio.Rendering;

public static class PdfMetadataInjector
{
    public static void Inject(byte[] pdf, string title, string author, string subject, Stream output,
        double? trimWidthPoints = null, double? trimHeightPoints = null, double? bleedPoints = null)
    {
        var ascii = Encoding.ASCII.GetString(pdf);
        var eofIdx = ascii.LastIndexOf("%%EOF", StringComparison.Ordinal);
        if (eofIdx < 0) { output.Write(pdf, 0, pdf.Length); return; }

        var rootMatch = Regex.Match(ascii, @"/Root\s+(\d+)\s+(\d+)\s*R", RegexOptions.RightToLeft);
        if (!rootMatch.Success) { output.Write(pdf, 0, pdf.Length); return; }

        string rootNum = rootMatch.Groups[1].Value;
        string rootGen = rootMatch.Groups[2].Value;

        var catalogObjMatch = Regex.Match(ascii,
            $@"\n{rootNum}\s+{rootGen}\s+obj\s*(?<dict><<.*?>>)\s*endobj",
            RegexOptions.Singleline);
        if (!catalogObjMatch.Success) { output.Write(pdf, 0, pdf.Length); return; }

        string oldDict = catalogObjMatch.Groups["dict"].Value;
        var trimmed = oldDict.TrimEnd('>', ' ').TrimStart('<', ' ');

        string xmp = GenerateXmp(title, author, subject);
        byte[] xmpBytes = Encoding.UTF8.GetBytes(xmp);

        // Build new objects ---------------------------------------------------
        // allObjects[0] = 99000 obj (OutputIntent)
        // allObjects[1] = 99001 obj header
        // allObjects[2] = 99001 obj data (XMP bytes)
        // allObjects[3] = 99001 obj footer
        // allObjects[4] = 99002 obj (modified catalog)
        // allObjects[5] = 99003 obj (TrimBox/BleedBox) — optional

        var allObjects = new List<byte[]>();

        // Object 99000: OutputIntent
        allObjects.Add(Encoding.ASCII.GetBytes(
            "99000 0 obj\n" +
            "<< /Type /OutputIntent /S /GTS_PDFX /OutputConditionIdentifier (FOGRA39) " +
            "/RegistryName (http://www.color.org) /Info (CMYK - FOGRA39) >>\n" +
            "endobj\n"));

        // Object 99001: XMP metadata stream (split into 3 parts)
        allObjects.Add(Encoding.ASCII.GetBytes(
            "99001 0 obj\n" +
            $"<< /Type /Metadata /Subtype /XML /Length {xmpBytes.Length} >>\n" +
            "stream\n"));
        allObjects.Add(xmpBytes);
        allObjects.Add(Encoding.ASCII.GetBytes("\nendstream\nendobj\n"));

        // Object 99002: Modified catalog
        allObjects.Add(Encoding.ASCII.GetBytes(
            "99002 0 obj\n" +
            $"<< {trimmed} /Metadata 99001 0 R /OutputIntents [99000 0 R] >>\n" +
            "endobj\n"));

        // Object 99003: TrimBox/BleedBox (optional)
        bool haveBox = trimWidthPoints.HasValue && trimHeightPoints.HasValue && bleedPoints.HasValue;
        if (haveBox)
        {
            var tw = trimWidthPoints!.Value;
            var th = trimHeightPoints!.Value;
            var bp = bleedPoints!.Value;
            allObjects.Add(Encoding.ASCII.GetBytes(
                "99003 0 obj\n" +
                "<<\n" +
                $"  /TrimBox [{F(bp)} {F(bp)} {F(tw + bp)} {F(th + bp)}]\n" +
                $"  /BleedBox [0 0 {F(tw + 2 * bp)} {F(th + 2 * bp)}]\n" +
                ">>\n" +
                "endobj\n"));
        }

        // Write original PDF -------------------------------------------------
        output.Write(pdf, 0, pdf.Length);

        // Write new objects and record byte offset of each LOGICAL PDF object.
        // Logical objects: 99000, 99001, 99002, (99003).
        // allObjects[2,3] are the XMP payload of object 99001 — no separate offset.
        long off99000 = output.Position; output.Write(allObjects[0], 0, allObjects[0].Length);
        long off99001 = output.Position; output.Write(allObjects[1], 0, allObjects[1].Length);
        /* XMP data   */                  output.Write(allObjects[2], 0, allObjects[2].Length);
        /* XMP footer */                  output.Write(allObjects[3], 0, allObjects[3].Length);
        long off99002 = output.Position; output.Write(allObjects[4], 0, allObjects[4].Length);
        long off99003 = haveBox ? output.Position : -1;
        if (haveBox)                      output.Write(allObjects[5], 0, allObjects[5].Length);

        long xrefOffset = output.Position;

        // Build xref table ---------------------------------------------------
        var xref = new StringBuilder();
        xref.AppendLine("xref");
        xref.AppendLine(haveBox ? "99000 4" : "99000 3");

        xref.AppendLine($"{off99000:D10} {0:00000} n ");
        xref.AppendLine($"{off99001:D10} {0:00000} n ");
        xref.AppendLine($"{off99002:D10} {0:00000} n ");
        if (haveBox)
            xref.AppendLine($"{off99003:D10} {0:00000} n ");

        xref.AppendLine("trailer");
        xref.AppendLine($"<< /Size {(haveBox ? 99004 : 99003)} /Root 99002 0 R >>");
        xref.AppendLine("startxref");
        xref.AppendLine($"{xrefOffset}");
        xref.AppendLine("%%EOF");

        byte[] xrefBytes = Encoding.ASCII.GetBytes(xref.ToString());
        output.Write(xrefBytes, 0, xrefBytes.Length);
    }

    private static string F(double v) => v.ToString("0.##");

    private static string GenerateXmp(string title, string author, string subject)
    {
        var now = DateTime.UtcNow.ToString("s") + "Z";
        return $@"<?xpacket begin=""﻿"" id=""W5M0MpCehiHzreSzNTczkc9d""?>
<x:xmpmeta xmlns:x=""adobe:ns:meta/"">
  <rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#"">
    <rdf:Description rdf:about=""""
      xmlns:dc=""http://purl.org/dc/elements/1.1/""
      xmlns:xmp=""http://ns.adobe.com/xap/1.0/""
      xmlns:pdf=""http://ns.adobe.com/pdf/1.3/""
      xmlns:xmpMM=""http://ns.adobe.com/xap/1.0/mm/"">
      <dc:title><rdf:Alt><rdf:li xml:lang=""x-default"">{EscapeXml(title)}</rdf:li></rdf:Alt></dc:title>
      <dc:creator><rdf:Seq><rdf:li>{EscapeXml(author)}</rdf:li></rdf:Seq></dc:creator>
      <dc:description><rdf:Alt><rdf:li xml:lang=""x-default"">{EscapeXml(subject)}</rdf:li></rdf:Alt></dc:description>
      <xmp:CreatorTool>AnayPublisherStudio</xmp:CreatorTool>
      <xmp:CreateDate>{now}</xmp:CreateDate>
      <xmp:ModifyDate>{now}</xmp:ModifyDate>
      <pdf:Producer>AnayPublisherStudio</pdf:Producer>
      <xmpMM:DocumentID>uuid:{Guid.NewGuid()}</xmpMM:DocumentID>
      <xmpMM:InstanceID>uuid:{Guid.NewGuid()}</xmpMM:InstanceID>
    </rdf:Description>
  </rdf:RDF>
</x:xmpmeta>
<?xpacket end=""w""?>";
    }

    private static string EscapeXml(string s)
    {
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
            .Replace("\"", "&quot;").Replace("'", "&apos;");
    }
}
