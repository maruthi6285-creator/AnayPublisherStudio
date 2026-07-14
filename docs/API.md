# API Reference - Engine Contracts

All contracts live in `AnayPublisherStudio.Application.Abstractions`.

## IDocumentParser
```csharp
BookDocument Parse(Stream docxStream);
```

## ITemplateProvider
```csharp
IReadOnlyList<PublishingTemplate> ListTemplates();
PublishingTemplate? GetTemplate(string id);
```

## ITemplatePackageService
Installable Template SDK packages (`template.json`, `styles.json`, `fonts.json`,
`layout.json`, `cover.json`, `preview.png`, `publisher.json`).

## ISpineCalculator
```csharp
double CalculateInches(int pageCount, PaperType paper, ColorMode color);
```

## ILayoutEngine
```csharp
int Render(BookDocument book, PublishingTemplate template, Stream output);
```

## IProfessionalLayoutEngine
```csharp
LayoutDocument Compose(BookDocument book, PublishingTemplate template);
Task<LayoutDocument> ComposeAsync(...);
```
Professional pagination: recto/verso, gutters, chapter opens, widow/orphan,
running headers/footers, text frames. Presentation only.

## ILivePreviewEngine
Layout-backed live preview (not PDF viewer): refresh, page render, thumbnails.

## ICoverEngine / ICoverDesigner
Wraparound cover PDF + layered cover design model (barcode, spine, guides).

## IValidationEngine
Preflight validation report (structured findings).

## IArtifactExporter
Multi-format export: Print/Digital/PDF-X/PDF-A, EPUB, Kindle, DOCX, archive, images, report.

## ITypographyEngine / IParagraphComposer / IHyphenationService
Presentation metrics only — no author text in or out of typography resolution.

## IPluginManager
Dynamic plugin discovery and isolated assembly load.

## IAiAssistant
Suggest-only AI. Never auto-rewrites manuscript content.

## IContentIntegrityGuard
```csharp
string ComputeFingerprint(BookDocument book);
ContentIntegrityResult Verify(string expectedFingerprint, BookDocument book);
```

## IExportService
```csharp
Task<PublishResult> PublishAsync(PublishingProject project, string outputDirectory, CancellationToken ct = default);
```
parse → fingerprint → compose → layout → spine → cover → integrity-verify → validate → optional multi-export.
