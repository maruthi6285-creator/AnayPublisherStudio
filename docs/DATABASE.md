# Database Design (SQLite)

The desktop app persists projects and app state to a local SQLite file
(`%AppData%/AnayPublisherStudio/projects.db`). The current build ships the
`Projects` table; the remaining tables are the planned schema.

```mermaid
erDiagram
    PROJECTS ||--o{ EXPORTS : produces
    PROJECTS ||--o{ HISTORY : logs
    PROJECTS }o--|| TEMPLATES : uses
    PROJECTS {
        TEXT Id PK
        TEXT Name
        TEXT ModifiedUtc
        TEXT Payload "JSON of PublishingProject"
    }
    TEMPLATES {
        TEXT Id PK
        TEXT Name
        TEXT Platform
        TEXT Json
    }
    EXPORTS {
        TEXT Id PK
        TEXT ProjectId FK
        TEXT Format
        TEXT Path
        TEXT CreatedUtc
    }
    HISTORY {
        TEXT Id PK
        TEXT ProjectId FK
        TEXT Action
        TEXT CreatedUtc
    }
    SETTINGS {
        TEXT Key PK
        TEXT Value
    }
```

### Implemented
- **Projects** - `Id`, `Name`, `ModifiedUtc`, and a `Payload` column holding the
  serialized `PublishingProject` graph. Recent-projects queries order by
  `ModifiedUtc DESC`.

### Planned
- **Templates**, **Exports**, **History**, **Settings**, plus `Books`, `Fonts`,
  `Images` caches as described in the specification.
