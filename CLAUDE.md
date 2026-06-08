# re-tracker

Reverse-engineering readability tracking system for Ghidra-decompiled C code.
Tracks rename progress, method status, call graphs, and drives LLM rename ordering.

---

## Project Structure

```
re-tracker/
  docker-compose.yml          # 3 services: sqlserver, api, web
  .env                        # SQL_SA_PASSWORD (never commit)
  src/                        # .NET 10 backend (solution root)
    Tracker.sln
    Tracker.Core/             # Enums, interfaces, DTOs, parse models
    Tracker.Data/             # EF Core entities, DbContext, Migrations/
    Tracker.Parsers/          # ILanguageParser impls (C, C#, Java stubs)
    Tracker.Api/              # ASP.NET Core 10 Web API — port 8080 (5000 host)
    Tracker.Cli/              # System.CommandLine 2.0 console app
      Commands/               # One file per command (Done, Next, Skip, …)
    Tracker.Tests/            # xUnit tests
  re-tracker-web/             # Angular 18 SPA — port 4200 (nginx in Docker)
    src/app/
      core/api.service.ts     # All HTTP calls + DTO interfaces
      shared/                 # StatusBadgeComponent, CopyButtonComponent
      dashboard/
      milestones/
      methods/
      files/
      search/
    src/styles.scss           # GitHub Primer light theme (single source of truth)
    design.md                 # Design spec: spacing, typography, color tokens
    nginx.conf                # Reverse-proxies /api/ → api:8080
```

---

## Stack

| Layer | Technology |
|---|---|
| Database | SQL Server 2025 (Docker, named volume `sqldata`) |
| ORM | EF Core 10, code-first migrations |
| API | ASP.NET Core 10, controllers, Swagger at `/swagger` |
| CLI | .NET 10 console, System.CommandLine 2.0.0 (stable) |
| Frontend | Angular 18 standalone components, pure CSS (no Angular Material) |
| Container | Docker Compose — sqlserver → api → web |

---

## Running the full stack

```powershell
# Start everything (first time or after code changes)
cd C:\projects\re-tracker
docker compose up -d

# Rebuild a specific service after code changes
docker compose build api   && docker compose up -d api
docker compose build web   && docker compose up -d web

# View logs
docker compose logs -f api
docker compose logs -f web
```

- Web UI: http://localhost:4200
- API + Swagger: http://localhost:5000/swagger
- SQL Server: localhost:1433 (sa / value from .env)

**Never run `docker compose down -v`** — the `-v` flag destroys the `sqldata` named volume and wipes the database.

---

## Development (without Docker)

```powershell
# API — requires SQL Server reachable at localhost:1433
cd src
dotnet run --project Tracker.Api

# Angular dev server (proxies /api/ to localhost:5000)
cd re-tracker-web
npm start
```

---

## Database migrations

```powershell
# Add a new migration
cd src
dotnet ef migrations add <Name> --project Tracker.Data --startup-project Tracker.Api --output-dir Migrations

# Apply migrations manually
dotnet ef database update --project Tracker.Data --startup-project Tracker.Api
```

Migrations are **auto-applied on API startup** (`db.Database.Migrate()` in `Program.cs`).

---

## API overview (18 endpoints)

| Prefix | Description |
|---|---|
| `GET /api/summary` | Progress stats |
| `GET /api/milestones[/tree/:id/next/:id/graph]` | Milestone tree, next method, D3 graph |
| `GET /api/methods`, `GET /api/methods/:id` | Paged method list, detail with callers/callees |
| `PUT /api/methods/:id/status` | Update status + comment |
| `GET /api/files` | Source file list |
| `GET /api/search?q=` | Full-text search |
| `POST /api/projects/:id/scan` | Trigger file scan (202 + jobId) |

---

## CLI commands

```
re-tracker next   [--milestone <id>]        # Pick next method to work on
re-tracker done   <name|id> [--comment]     # Mark Done
re-tracker skip   <name|id> [--reason]      # Mark Skipped
re-tracker defer  <name|id> [--comment]     # Mark Deferred
re-tracker review <name|id>                 # Mark NeedsReview
re-tracker start  <name|id>                 # Mark InProgress
re-tracker scan   --path <path>             # Index a source file/directory
re-tracker status [--filter] [--file]       # List methods
re-tracker search <query>                   # Search methods/files
re-tracker info   <name|id>                 # Show method detail
```

Output status symbols: `[ ]` Pending · `[~]` InProgress · `[?]` NeedsReview · `[x]` Done · `[-]` Skipped · `[>]` Deferred

---

## Frontend design

All styling lives in `re-tracker-web/src/styles.scss`.
Refer to `re-tracker-web/design.md` for the full design spec (spacing scale, typography, color tokens, component measurements).

- **No Angular Material** — pure CSS using GitHub Primer light theme
- CSS custom properties (`--color-canvas-default`, `--color-fg-default`, etc.) are the single source of truth
- Components use utility classes: `.Box`, `.Box-row`, `.Box-header`, `.Label`, `.btn`, `.gh-table`, `.stat-card`, `.Counter`
- Status colors: Done=green, InProgress=blue, NeedsReview=amber, Deferred=purple, Pending/Skipped=gray

---

## Adding a new language parser

1. Create `src/Tracker.Parsers/XLanguageParser.cs` implementing `ILanguageParser`
2. Register in `src/Tracker.Api/Program.cs`: `builder.Services.AddScoped<ILanguageParser, XLanguageParser>()`
3. No other files need to change — the scan service discovers parsers via DI

---

## git

- **Never include AI model names or `Co-Authored-By` lines in commits.** Commits must be solely authored by the human developer.
- Always commit as: name `Umang Bhatt`, email `bhatt.umang7@gmail.com`
- Never commit `.env` (contains the SA password)
