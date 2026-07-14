<p align="center">
  <img src="https://img.shields.io/badge/version-1.0.0-blue" alt="Version"/>
  <img src="https://img.shields.io/badge/.NET-10.0-purple" alt=".NET"/>
  <img src="https://img.shields.io/badge/license-MIT-green" alt="License"/>
  <img src="https://img.shields.io/badge/tests-91%2F91%20passed-brightgreen" alt="Tests"/>
</p>

<h1 align="center">Anay Publisher Studio</h1>

<p align="center">
  <strong>AI-Assisted Desktop Publishing Platform</strong><br/>
  Turn your DOCX manuscript into publication-ready artifacts for Amazon KDP, IngramSpark, Lulu, and more.
</p>

<p align="center">
  <a href="https://github.com/maruthi6285-creator/AnayPublisherStudio/releases/latest">
    <img src="https://img.shields.io/badge/Download-v1.0.0-brightgreen?style=for-the-badge&logo=windows" alt="Download Latest Release"/>
  </a>
  <a href="https://github.com/maruthi6285-creator/AnayPublisherStudio/issues/new?template=bug_report.md">
    <img src="https://img.shields.io/badge/Upload-Bug%20Report-orange?style=for-the-badge&logo=github" alt="Upload Bug Report"/>
  </a>
</p>

<p align="center">
  <a href="#download">Download</a> |
  <a href="#upload--contribute">Upload</a> |
  <a href="#features">Features</a> |
  <a href="#quick-start">Quick Start</a> |
  <a href="#architecture">Architecture</a> |
  <a href="#documentation">Docs</a>
</p>

---

## Download

| Platform | Link | Size |
|---|---|---|
| **Windows (WPF)** | [Download v1.0.0](https://github.com/maruthi6285-creator/AnayPublisherStudio/releases/latest) | Desktop app |
| **CLI (Cross-platform)** | [Download v1.0.0](https://github.com/maruthi6285-creator/AnayPublisherStudio/releases/latest) | Command-line tool |
| **Source Code** | [Download ZIP](https://github.com/maruthi6285-creator/AnayPublisherStudio/archive/refs/heads/master.zip) | Full source |
| **Source Code** | [Clone via Git](https://github.com/maruthi6285-creator/AnayPublisherStudio.git) | `git clone` |

### Quick Install
```powershell
# Download and run installer (when available)
winget install AnayGlobalSolutions.AnayPublisherStudio

# Or download manually from Releases
# https://github.com/maruthi6285-creator/AnayPublisherStudio/releases/latest
```

---

## Upload / Contribute

We welcome contributions! Here's how to upload your work:

### Report a Bug
[![Upload Bug Report](https://img.shields.io/badge/Upload-Bug%20Report-orange?style=flat&logo=github)](https://github.com/maruthi6285-creator/AnayPublisherStudio/issues/new?template=bug_report.md)

### Request a Feature
[![Upload Feature Request](https://img.shields.io/badge/Upload-Feature%20Request-blue?style=flat&logo=github)](https://github.com/maruthi6285-creator/AnayPublisherStudio/issues/new?template=feature_request.md)

### Submit a Pull Request
[![Upload Code](https://img.shields.io/badge/Upload-PR-green?style=flat&logo=git)](https://github.com/maruthi6285-creator/AnayPublisherStudio/compare)

### Upload Templates
Have a publisher template? Upload it to `Resources/Templates/`:
```
Resources/Templates/
  YourPublisher/
    Paperback/
      6x9/
        template.json
        styles.json
        fonts.json
        layout.json
        margins.json
        cover.json
        publisher.json
```

### Upload Plugins
Create a plugin and upload to `Resources/Plugins/`:
```
Resources/Plugins/
  your-plugin/
    plugin.json
    YourPlugin.dll
```

---

## Features

### Core Publishing
- **Professional Layout** - Recto/verso pages, dynamic gutters, chapter opens, widow/orphan control
- **Cover Designer** - Layers, barcode reservation, spine calculation, safe zones
- **11 Publisher Templates** - Amazon KDP, IngramSpark, Lulu, B&N, Blurb, NotionPress, and more
- **Multi-format Export** - Print PDF, Digital PDF, EPUB, Kindle, Project Archive

### Configuration System
- **10 Configurable Sections** - App, Templates, Rendering, Typography, Validation, Publishing, Logging, Theme, Backup, Plugins
- **appsettings.json** - All settings in JSON format
- **Settings UI** - Full WPF settings panel with save/reset
- **Environment Overrides** - `APS_` prefix environment variables

### Quality
- **91 Tests Passing** - Unit, integration, performance, and QA tests
- **Clean Architecture** - Domain, Application, Infrastructure, Rendering, Typography, Validation, Composition, CLI, WPF
- **Content Integrity** - SHA-256 fingerprinting ensures author content is never modified
- **100% Async I/O** - All operations are asynchronous

---

## Quick Start

### Prerequisites
- .NET 10.0 SDK or later
- Windows 10/11 (for WPF app)

### Run the WPF App
```powershell
dotnet run --project src/AnayPublisherStudio.Presentation
```

### Run the CLI
```powershell
dotnet run --project src/AnayPublisherStudio.Cli -- "manuscript.docx" --output "output/"
```

### Run Tests
```powershell
dotnet test --configuration Release
```

### Build Release
```powershell
dotnet build --configuration Release
```

---

## Architecture

```
AnayPublisherStudio/
├── src/
│   ├── Domain/              # Entities, Value Objects, Enums
│   ├── Application/         # Interfaces, Pipeline, Configuration
│   ├── Infrastructure/      # Engine implementations
│   ├── Rendering/           # QuestPDF interior + cover
│   ├── Typography/          # Font metrics, paragraph composer
│   ├── Validation/          # Preflight checks
│   ├── Composition/         # DI registration
│   ├── Cli/                 # Command-line interface
│   └── Presentation/        # WPF desktop app
├── tests/                   # 91 tests (xUnit)
├── Resources/
│   ├── Templates/           # 11 publisher template packages
│   └── Plugins/             # Plugin system
└── docs/                    # Architecture, API, Developer Guide
```

### Clean Architecture Layers
```
┌─────────────────────────────────────┐
│         Presentation (WPF)          │
├─────────────────────────────────────┤
│         Composition (DI)            │
├─────────────────────────────────────┤
│  Infrastructure │ Rendering │ Typography │ Validation │
├─────────────────────────────────────┤
│         Application (Interfaces)    │
├─────────────────────────────────────┤
│         Domain (Entities)           │
└─────────────────────────────────────┘
```

---

## Documentation

| Document | Description |
|---|---|
| [Architecture](docs/ARCHITECTURE.md) | System design, component diagrams |
| [API Reference](docs/API.md) | Engine interfaces and contracts |
| [Developer Guide](docs/DEVELOPER_GUIDE.md) | How to extend the platform |
| [Database Design](docs/DATABASE.md) | SQLite schema and persistence |
| [Roadmap](docs/ROADMAP.md) | Planned features |
| [Testing](TESTING.md) | How to run and write tests |

---

## Technology Stack

| Category | Technology |
|---|---|
| Language | C# 13 |
| Runtime | .NET 10.0 |
| UI | WPF + MVVM (CommunityToolkit.Mvvm) |
| DI | Microsoft.Extensions.DependencyInjection |
| Config | Microsoft.Extensions.Configuration |
| PDF | QuestPDF |
| DOCX | DocumentFormat.OpenXml |
| Images | SixLabors.ImageSharp |
| Database | Microsoft.Data.Sqlite |
| Logging | Serilog |
| Testing | xUnit + Coverlet |

---

## License

MIT License - Copyright (c) Anay Global Solutions Private Limited

---

<p align="center">
  <strong>Anay Global Solutions Private Limited</strong><br/>
  <a href="https://github.com/maruthi6285-creator/AnayPublisherStudio">GitHub</a> |
  <a href="https://github.com/maruthi6285-creator/AnayPublisherStudio/issues">Issues</a> |
  <a href="https://github.com/maruthi6285-creator/AnayPublisherStudio/releases">Releases</a>
</p>
