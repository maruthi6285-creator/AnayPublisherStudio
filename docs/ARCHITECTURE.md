# Architecture

Anay Publisher Studio follows **Clean Architecture**. Dependencies point
inward: the UI and infrastructure depend on the application/domain, never the
reverse. Every engine sits behind an interface declared in the Application
layer, so implementations are swappable via dependency injection.

## Layer / component view

```mermaid
flowchart TD
    subgraph Presentation["Presentation (WPF, MVVM)"]
        UI[MainWindow + Ribbon]
        VM[MainViewModel]
    end
    subgraph Composition
        DI[ServiceCollectionExtensions]
    end
    subgraph Application
        ABS[Engine Interfaces]
        PIPE[PublishPipeline]
        REP[ValidationReport]
    end
    subgraph Domain
        BOM[Book Object Model]
        TPL[PublishingTemplate]
    end
    subgraph Infrastructure
        PARSE[DocxDocumentParser]
        TP[JsonTemplateProvider]
        SPINE[KdpSpineCalculator]
        DB[SqliteProjectRepository]
        AI[HeuristicAiAssistant]
    end
    subgraph Rendering
        LAY[QuestPdfLayoutEngine]
        COV[QuestPdfCoverEngine]
    end
    subgraph ValidationEng["Validation"]
        VAL[KdpValidationEngine]
    end

    UI --> VM --> ABS
    DI --> ABS
    PIPE --> ABS
    ABS --> BOM
    ABS --> TPL
    PARSE -.implements.-> ABS
    TP -.implements.-> ABS
    SPINE -.implements.-> ABS
    DB -.implements.-> ABS
    AI -.implements.-> ABS
    LAY -.implements.-> ABS
    COV -.implements.-> ABS
    VAL -.implements.-> ABS
```

## Core domain class model

```mermaid
classDiagram
    class BookDocument {
        +BookMetadata Metadata
        +List~Chapter~ Chapters
        +List~Footnote~ Footnotes
        +List~TocEntry~ TableOfContents
        +int TotalBlocks
    }
    class Chapter {
        +string Title
        +int Number
        +List~ContentBlock~ Blocks
    }
    class ContentBlock {
        <<abstract>>
        +BlockType Type
        +int Order
    }
    ContentBlock <|-- ParagraphBlock
    ContentBlock <|-- HeadingBlock
    ContentBlock <|-- ImageBlock
    ContentBlock <|-- TableBlock
    ContentBlock <|-- PageBreakBlock
    ParagraphBlock --> "1..*" TextRun
    BookDocument --> "*" Chapter
    Chapter --> "*" ContentBlock
    BookDocument --> BookMetadata
```

## Publish sequence

```mermaid
sequenceDiagram
    actor User
    participant VM as MainViewModel
    participant P as PublishPipeline
    participant Parser as IDocumentParser
    participant TP as ITemplateProvider
    participant Layout as ILayoutEngine
    participant Spine as ISpineCalculator
    participant Cover as ICoverEngine
    participant Val as IValidationEngine

    User->>VM: Publish
    VM->>P: PublishAsync(project, outDir)
    P->>Parser: Parse(docx)
    Parser-->>P: BookDocument
    P->>TP: GetTemplate(id)
    TP-->>P: PublishingTemplate
    P->>Layout: Render(book, template) 
    Layout-->>P: pageCount + interior.pdf
    P->>Spine: CalculateInches(pageCount,...)
    Spine-->>P: spineWidth
    P->>Cover: Render(project, template, pageCount)
    Cover-->>P: cover.pdf
    P->>Val: Validate(book, template, pageCount)
    Val-->>P: ValidationReport
    P-->>VM: PublishResult
```

## Design principles applied
- **SOLID** - each engine is a single-responsibility class behind an interface.
- **Dependency Inversion** - Application defines contracts; Infrastructure /
  Rendering / Validation implement them; Composition wires them.
- **MVVM** - the WPF view binds to `MainViewModel`; no publishing logic in the UI.
- **Repository pattern** - `IProjectRepository` abstracts SQLite persistence.
- **Open/Closed** - new platforms are new template folders + (optionally) new
  engine implementations; existing code is untouched.


## Extended engines (this completion phase)
- `IProfessionalLayoutEngine` / `ProfessionalLayoutEngine` — composition document
- `ILivePreviewEngine` / `LivePreviewEngine` — layout-backed preview
- `ICoverDesigner` / `CoverDesigner` — layered cover design
- `IArtifactExporter` / `ArtifactExporter` — multi-format export
- `ITemplatePackageService` / `TemplatePackageService` — Template SDK
- `IPluginManager` / `PluginManager` — dynamic plugins
- `IParagraphComposer` / `IHyphenationService` — typography composition

All remain behind Application abstractions; Composition wires implementations.
Author content integrity remains mandatory on every export path.
