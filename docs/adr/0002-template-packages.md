# ADR 0002: Publisher Profiles Are Template Packages

## Status
Accepted

## Context
Multiple print vendors (KDP, IngramSpark, Lulu, etc.) differ in trim, margins, and spine rules.

## Decision
Every publisher exists only as a template package (`template.json`, `styles.json`,
`fonts.json`, `layout.json`, `cover.json`, `preview.png`, `publisher.json`).
No hard-coded publisher logic in engines.

## Consequences
- New platforms = new folders; optional engine plugins via `IPluginManager`.
- `JsonTemplateProvider` / `ITemplatePackageService` discover and install packages.
