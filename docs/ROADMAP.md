# Roadmap

## Completed
- Absolute content-ownership rule (`IContentIntegrityGuard`).
- Typography engine with OpenType flags, drop caps, justification ranges, hyphenation hooks.
- Professional layout engine (pagination, recto/verso, dynamic gutter, widow/orphan, keep-with-next, running matter, text frames).
- Live preview engine (layout-backed, zoom/modes/guides).
- Cover designer (layers, barcode, spine, safe zones, KDP validation).
- Publisher profiles as template packages (Amazon, IngramSpark, Lulu, B&N, Notion Press, Blurb, Thesis, Magazine, Journal, Children, Comic).
- Publishing Template SDK + installable packages.
- Preflight validation (margins, trim, bleed, live area, images/DPI, fonts, hyperlinks, TOC, spine/barcode, profile checks).
- Multi-format export (Print/Digital/PDF-X/PDF-A, EPUB, Kindle, DOCX/HTML, archive, images, validation report).
- Plugin manager (dynamic discovery / AssemblyLoadContext).
- AI assistant (suggest-only: metadata, checklist, TOC, index, glossary, bibliography).
- Professional WPF shell (ribbon, dockable panels, dark/light, preview, validation).
- Performance-oriented async composition + virtualization-friendly page model.
- Unit / integration / performance tests.

## Next
- Full OpenType GSUB/GPOS shaping backend.
- CMYK conversion pipeline for cover art.
- Richer WPF page rasterization at 150+ DPI thumbnails.
- LLM-backed `IAiAssistant` provider plugin.
