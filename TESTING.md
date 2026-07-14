# Testing Anay Publisher Studio Locally

## Prerequisites

| Requirement | Version | Check |
|---|---|---|
| .NET SDK | 9.0+ | `dotnet --list-sdks` |
| OS | Windows 10/11 (WPF requires Windows) | — |
| IDE (optional) | Visual Studio 2022 17.8+ or Rider 2024+ | — |

## Quick Start

### 1. Clone & Build

```bash
git clone <your-repo-url>
cd AnayPublisherStudio
dotnet restore
dotnet build --configuration Release
```

### 2. Run Tests

```bash
dotnet test --configuration Release --verbosity normal
```

### 3. Run WPF App

```bash
dotnet run --project src/AnayPublisherStudio.Presentation --configuration Release
```

### 4. Run CLI

```bash
dotnet run --project src/AnayPublisherStudio.Cli -- "path/to/manuscript.docx" --output "output/"
```

## Test Structure

```
tests/AnayPublisherStudio.Tests/
├── ContentIntegrityGuardTests.cs       # SHA-256 fingerprinting
├── DocxParserTests.cs                  # DOCX parsing
├── SpineCalculatorTests.cs             # Spine width calculation
├── ValidationEngineTests.cs            # Preflight validation
├── Cover/
│   └── CoverDesignerTests.cs           # Cover design creation
├── Export/
│   └── ArtifactExporterTests.cs        # Multi-format export
├── Layout/
│   └── ProfessionalLayoutEngineTests.cs # Pagination engine
├── Performance/
│   └── LayoutPerformanceTests.cs       # Benchmarks
├── Plugins/
│   └── PluginManagerTests.cs           # Plugin discovery/loading
├── Typography/
│   └── ParagraphComposerTests.cs       # Line measurement
└── Qa/
    ├── IntegrationPipelineTests.cs     # End-to-end pipeline
    ├── LayoutRenderingPdfTests.cs      # Layout + PDF rendering
    ├── ContentIntegrityExtendedTests.cs # Extended integrity
    ├── TemplateEngineTests.cs          # Template discovery
    ├── PerformanceExtendedTests.cs     # Extended benchmarks
    ├── TypographyValidationExportTests.cs # Typography + validation + export
    ├── ErrorHandlingSecurityUiTests.cs # Error handling, security
    └── TestFixtures.cs                 # Shared test helpers
```

## Configuration Testing

The new configuration system uses `appsettings.json`. Test different configs:

### Default Config
Located at `src/AnayPublisherStudio.Presentation/appsettings.json`. Contains all 10 configurable sections.

### Environment Override
Set `APS_ENVIRONMENT=Development` to load `appsettings.Development.json`.

### Environment Variables
Override any setting with `APS_` prefix:
```powershell
$env:APS_Validation__KdpMinPages = "10"
dotnet run --project src/AnayPublisherStudio.Presentation
```

### CLI Config Override
```bash
dotnet run --project src/AnayPublisherStudio.Cli -- manuscript.docx --output ./out
```

## Running Specific Test Categories

```bash
# Unit tests only
dotnet test --filter "Category=Unit"

# Integration tests only
dotnet test --filter "Category=Integration"

# Performance tests only
dotnet test --filter "Category=Performance"

# Single test class
dotnet test --filter "FullyQualifiedName~ContentIntegrityGuardTests"

# Single test method
dotnet test --filter "FullyQualifiedName~ContentIntegrityGuardTests.VerifyIntact"
```

## Generating a Test Report

```bash
dotnet test --configuration Release --collect:"XPlat Code Coverage" --results-directory ./coverage
```

Coverage reports are generated in `./coverage/` as Cobertura XML files.

## Build Verification

The solution must compile with:
- **0 errors**
- **0 warnings** (CS1591 doc warnings suppressed via `<NoWarn>`)

```bash
dotnet build --configuration Release 2>&1 | Select-String "Error|Warning"
```
