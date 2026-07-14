# ADR 0003: Professional Layout as Composition Document

## Status
Accepted

## Context
Live preview and print export must share the same pagination rules.

## Decision
`IProfessionalLayoutEngine` produces a `LayoutDocument` of `ComposedPage`s
(recto/verso, gutters, frames, running matter). `ILayoutEngine` and
`ILivePreviewEngine` consume that geometry. Author blocks are referenced by
order only.

## Consequences
- Incremental/async composition is possible for large manuscripts.
- Rendering remains swappable (QuestPDF today).
