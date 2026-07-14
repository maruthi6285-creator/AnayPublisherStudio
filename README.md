# Anay Publisher Studio

**Anay Global Solutions Private Limited**

An AI-assisted, template-driven desktop publishing platform that turns a DOCX
manuscript into publication-ready artifacts for Amazon KDP (and, by design,
IngramSpark, Lulu and future platforms): a print-ready interior PDF, a
wraparound cover PDF, and a KDP-compliance validation report.

> This is **not** a Word-to-PDF converter. It parses the manuscript into a rich
> Book Object Model, applies a data-driven publishing template, lays out the
> interior with mirrored margins / running headers / chapter starts, builds the
> cover from the official KDP dimensions, and validates the result.
>
> **Absolute rule:** the author owns the content. The software owns only
> presentation (margins, headers, typography, cover, export). A content-integrity
> fingerprint is taken after parse and verified after layout; any content mutation
> fails the run.

---

## Status of this build

| Layer | Project | Target | Builds on Linux | Verified |
|---|---|---|---|---|
| Domain | `AnayPublisherStudio.Domain` | net9.0 | Yes | Compiles |
| Application | `AnayPublisherStudio.Application` | net9.0 | Yes | Compiles |
| Infrastructure | `AnayPublisherStudio.Infrastructure` | net9.0 | Yes | Compiles |
| Rendering | `AnayPublisherStudio.Rendering` | net9.0 | Yes | Compiles |
| Typography | `AnayPublisherStudio.Typography` | net9.0 | Yes | Compiles |
| Validation | `AnayPublisherStudio.Validation` | net9.0 | Yes | Compiles |
| Composition | `AnayPublisherStudio.Composition` | net9.0 | Yes | Compiles |
| CLI harness | `AnayPublisherStudio.Cli` | net9.0 | Yes | **Runs end-to-end** |
| Tests | `AnayPublisherStudio.Tests` | net9.0 | Yes | **14/14 pass** |
| Presentation (WPF) | `AnayPublisherStudio.Presentation` | net9.0-windows | Windows only | Compiles* |

\* The WPF head is Windows-only. It was verified to compile against the Windows
reference assemblies via `-p:EnableWindowsTargeting=true`; **run it on Windows**.

### What already works (proven by the CLI + tests)
- **Professional layout** (recto/verso, dynamic gutter, chapter opens, widow/orphan, running matter).
- **Live preview engine** (layout-backed; zoom / single / facing / continuous / guides).
- **Cover designer** (layers, barcode reservation, spine calc, safe zones).
- **Publisher template packages** (Amazon, IngramSpark, Lulu, B&N, Notion Press, Blurb, Thesis, Magazine, Journal, Children, Comic).
- **Template SDK** + installable packages; **plugin manager**.
- **Preflight validation** (Adobe Preflight–class checks).
- **Multi-format export** (Print/Digital/PDF-X/PDF-A, EPUB, Kindle, archive, images, report).
- **AI suggest-only** assistant (never auto-rewrites content).
- **Professional WPF shell** (ribbon, panels, dark/light, preview, validation).

- DOCX parsing into the Book Object Model (chapters, headings, paragraphs with
  runs, images with DPI, tables, page breaks, footnotes, read-only properties).
- Data-driven template loading from `template.json` (no hard-coded KDP values).
- Spine-width calculation from page count / paper / colour (matches the KDP
  0.236in figure for 105 BW white pages).
- Interior PDF layout at true 6x9in trim with running headers and page numbers.
- Typography engine (body/heading/caption metrics, first-line indent, drop-cap lines).
- Content-integrity guard (SHA-256 fingerprint; pipeline fails if content mutates).
- Wraparound cover PDF at the exact 12.486 x 9.250in overall size with the
  barcode safe area anchored bottom-right of the back cover.
- KDP validation report (page-count, margins, image DPI, metadata, structure).

---

## Quick start

### Prerequisites
- .NET 9 SDK

### Run the pipeline headless (any OS)
```bash
dotnet run --project src/AnayPublisherStudio.Cli -- \
    path/to/manuscript.docx \
    Resources/Templates \
    ./output \
    amazon-paperback-6x9
```
Outputs `interior-print.pdf`, `cover.pdf` and `validation-report.json` into `./output`.

### Run the tests
```bash
dotnet test
```

### Run the desktop app (Windows)
```bash
dotnet run --project src/AnayPublisherStudio.Presentation
```

---

## Documentation
- [Architecture](docs/ARCHITECTURE.md) - layers, component/class/sequence diagrams
- [Database design](docs/DATABASE.md)
- [Developer guide](docs/DEVELOPER_GUIDE.md)
- [API reference](docs/API.md) - the engine interfaces
- [Roadmap](docs/ROADMAP.md)

## Technology
C# / .NET 9, WPF + MVVM (CommunityToolkit.Mvvm), Microsoft.Extensions DI &
Hosting, DocumentFormat.OpenXml, QuestPDF, SixLabors.ImageSharp,
Microsoft.Data.Sqlite, Serilog, xUnit.
