# ADR 0001: Author Content Is Immutable

## Status
Accepted

## Context
A publishing platform must never silently rewrite manuscripts.

## Decision
All presentation engines (layout, typography, cover, export) operate without
mutating author text, structure, tables, footnotes, endnotes, captions, or images.
`IContentIntegrityGuard` fingerprints author content after parse and verifies
after presentation. Fingerprint mismatch fails the export.

## Consequences
- Engines receive BookDocument and may only add presentation artefacts (TOC, page numbers).
- AI may only suggest; user approval is required for any content change.
