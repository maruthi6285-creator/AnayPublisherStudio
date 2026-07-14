namespace AnayPublisherStudio.Domain.Enums;

/// <summary>Print color mode for a publishing target.</summary>
public enum ColorMode
{
    /// <summary>Black-and-white interior.</summary>
    BlackWhite,
    /// <summary>Full-color interior.</summary>
    Color,
}

/// <summary>Paper stock used by the print target.</summary>
public enum PaperType
{
    /// <summary>Standard white stock.</summary>
    White,
    /// <summary>Cream stock.</summary>
    Cream,
    /// <summary>Premium stock.</summary>
    Premium,
}

/// <summary>Kind of a structural block inside the manuscript.</summary>
public enum BlockType
{
    /// <summary>Body paragraph.</summary>
    Paragraph,
    /// <summary>Heading.</summary>
    Heading,
    /// <summary>Embedded image.</summary>
    Image,
    /// <summary>Table.</summary>
    Table,
    /// <summary>List item.</summary>
    ListItem,
    /// <summary>Block quote.</summary>
    Quote,
    /// <summary>Caption.</summary>
    Caption,
    /// <summary>Explicit page break.</summary>
    PageBreak,
    /// <summary>Footnote marker/content.</summary>
    Footnote,
}

/// <summary>Heading level (H1..H6) detected during parsing.</summary>
public enum HeadingLevel
{
    /// <summary>Chapter-level heading.</summary>
    H1 = 1,
    /// <summary>Section heading.</summary>
    H2 = 2,
    /// <summary>Subsection heading.</summary>
    H3 = 3,
    /// <summary>Level 4 heading.</summary>
    H4 = 4,
    /// <summary>Level 5 heading.</summary>
    H5 = 5,
    /// <summary>Level 6 heading.</summary>
    H6 = 6,
}

/// <summary>Horizontal text alignment.</summary>
public enum TextAlignment
{
    /// <summary>Left aligned.</summary>
    Left,
    /// <summary>Centered.</summary>
    Center,
    /// <summary>Right aligned.</summary>
    Right,
    /// <summary>Justified.</summary>
    Justify,
}

/// <summary>Supported export artifact formats.</summary>
public enum ExportFormat
{
    /// <summary>Print-ready interior PDF.</summary>
    PrintPdf,
    /// <summary>Digital-distribution interior PDF.</summary>
    DigitalPdf,
    /// <summary>EPUB 3 package.</summary>
    Epub,
    /// <summary>Kindle-oriented EPUB package.</summary>
    Kindle,
    /// <summary>Wraparound cover PDF.</summary>
    CoverPdf,
    /// <summary>Project archive (.apsproj).</summary>
    ProjectArchive,
    /// <summary>PDF/X print-exchange PDF.</summary>
    PdfX,
    /// <summary>PDF/A archival PDF.</summary>
    PdfA,
    /// <summary>DOCX / HTML manuscript export.</summary>
    Docx,
    /// <summary>Extracted image assets.</summary>
    Images,
    /// <summary>Structured validation report.</summary>
    ValidationReport,
}

/// <summary>Severity level for a validation finding.</summary>
public enum ValidationSeverity
{
    /// <summary>Informational finding.</summary>
    Info,
    /// <summary>Warning that does not block publish.</summary>
    Warning,
    /// <summary>Error that blocks publish.</summary>
    Error,
}
