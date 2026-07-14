# Developer Guide

## Solution layout
```
AnayPublisherStudio/
  src/
    AnayPublisherStudio.Domain          # entities, value objects, enums (no deps)
    AnayPublisherStudio.Application     # engine interfaces, pipeline, DTOs
    AnayPublisherStudio.Infrastructure  # OpenXML parser, template loader, SQLite, AI
    AnayPublisherStudio.Rendering       # QuestPDF interior + cover engines
    AnayPublisherStudio.Validation      # KDP compliance engine
    AnayPublisherStudio.Composition     # DI composition root
    AnayPublisherStudio.Cli             # headless pipeline runner
    AnayPublisherStudio.Presentation    # WPF MVVM desktop head (Windows)
  tests/
    AnayPublisherStudio.Tests           # xUnit unit tests
  Resources/Templates/Amazon/Paperback/6x9/  # data-driven template + assets
  docs/
```

## Building
```bash
# Cross-platform libraries, CLI and tests build & run anywhere:
dotnet build src/AnayPublisherStudio.Cli
dotnet test

# The WPF head is Windows-only. On Windows:
dotnet build src/AnayPublisherStudio.Presentation
# To merely COMPILE-CHECK it on Linux/macOS:
dotnet build src/AnayPublisherStudio.Presentation -p:EnableWindowsTargeting=true
```

## Adding a new publishing target (e.g. IngramSpark)
1. Create `Resources/Templates/IngramSpark/Paperback/6x9/template.json` with the
   platform's trim, margins and spine rules. **No code change** is required for
   layout - `JsonTemplateProvider` discovers it automatically.
2. If the platform needs bespoke spine maths or cover rules, implement a new
   `ISpineCalculator` / `ICoverEngine` and register it in `Composition`.

## Adding an engine implementation
1. Depend on the interface in `AnayPublisherStudio.Application.Abstractions`.
2. Implement it in the appropriate layer.
3. Register it in `ServiceCollectionExtensions.AddAnayPublisherStudio`.
   Because the pipeline depends only on interfaces, nothing else changes.

## Swapping the AI provider
`IAiAssistant` ships as `HeuristicAiAssistant` (offline). To use an LLM, add a
new implementation (e.g. `OpenAiAssistant`) and change the single registration
line in `Composition`.

## Coding standards
- XML documentation on public types and members.
- `async`/`await` for all I/O.
- No business logic in the Presentation layer.
- Unit tests for every engine with deterministic behaviour.
