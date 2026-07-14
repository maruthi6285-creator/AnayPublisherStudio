using System.Text.RegularExpressions;

namespace AnayPublisherStudio.Rendering;

/// <summary>Counts pages in a PDF byte buffer by scanning for /Type /Page objects.</summary>
public static class PdfPageCounter
{
    /// <summary>Returns the number of page objects in the PDF.</summary>
    public static int Count(byte[] pdf)
    {
        var text = System.Text.Encoding.Latin1.GetString(pdf);
        // Prefer the /Count on the page tree root when present.
        var m = Regex.Matches(text, "/Type\\s*/Pages[^>]*?/Count\\s+(\\d+)");
        if (m.Count > 0 && int.TryParse(m[^1].Groups[1].Value, out var count) && count > 0)
            return count;
        return Regex.Matches(text, "/Type\\s*/Page[^s]").Count;
    }
}
