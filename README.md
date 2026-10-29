<div align="center">
  <br />
  <h1>Coordina</h1>
  <p>
    A workspace-first collaboration platform foundation built with .NET, React, PostgreSQL, and a strict quality gate.
  </p>
  <p>
    <a href="https://github.com/Neptune2k21/Coordina/actions/workflows/quality.yml">
      <img src="https://github.com/Neptune2k21/Coordina/actions/workflows/quality.yml/badge.svg?branch=master" alt="Quality workflow status" />
    </a>
    <img src="https://img.shields.io/badge/status-pre--MVP-111827?style=flat-square" alt="Project status: pre-MVP" />
    <img src="https://img.shields.io/badge/license-MIT-111827?style=flat-square" alt="MIT License" />
  </p>
  <br />
  <p>
    <img height="42" src="https://cdn.jsdelivr.net/gh/devicons/devicon@latest/icons/dotnetcore/dotnetcore-original.svg" alt=".NET" />
    &nbsp;&nbsp;
    <img height="42" src="https://cdn.jsdelivr.net/gh/devicons/devicon@latest/icons/csharp/csharp-original.svg" alt="C#" />
    &nbsp;&nbsp;
    <img height="42" src="https://cdn.jsdelivr.net/gh/devicons/devicon@latest/icons/postgresql/postgresql-original.svg" alt="PostgreSQL" />
    &nbsp;&nbsp;
    <img height="42" src="https://cdn.jsdelivr.net/gh/devicons/devicon@latest/icons/react/react-original.svg" alt="React" />
    &nbsp;&nbsp;
    <img height="42" src="https://cdn.jsdelivr.net/gh/devicons/devicon@latest/icons/typescript/typescript-original.svg" alt="TypeScript" />
    &nbsp;&nbsp;
    <img height="42" src="https://cdn.jsdelivr.net/gh/devicons/devicon@latest/icons/vitejs/vitejs-original.svg" alt="Vite" />
    &nbsp;&nbsp;
    <img height="42" src="https://cdn.jsdelivr.net/gh/devicons/devicon@latest/icons/tailwindcss/tailwindcss-original.svg" alt="Tailwind CSS" />
    &nbsp;&nbsp;
    <img height="42" src="https://cdn.jsdelivr.net/gh/devicons/devicon@latest/icons/docker/docker-original.svg" alt="Docker" />
  </p>
  <br />
</div>

## What This Is

Coordina is not an MVP yet. It is the technical base of a future collaboration product: authentication, workspace tenancy, invitation-based membership, a React application shell, persistence, tests, CI, and documentation.

The current goal is to make the project easy to evolve without turning the first real product features into a rewrite. The backend is organized by feature module, the frontend is organized by product area, and the quality gate is strict enough to catch formatting, typing, tests, builds, and end-to-end regressions before code lands.

## Project State

| Area | Current state |
| --- | --- |
| Product maturity | Pre-MVP foundation |
| Primary domain concept | Workspace as tenant boundary |
| Backend shape | Modular ASP.NET Core API |
| Frontend shape | React SaaS shell with auth and workspace onboarding |
| Persistence | PostgreSQL with EF Core migrations |
| CI | GitHub Actions quality workflow |
| Tests | API integration tests, frontend tests, Playwright e2e |
| Deployment | Not production-hardened yet |

## Foundation Capabilities

| Capability | Why it exists |
| --- | --- |
| Account registration and sign-in | Gives the platform an authenticated user boundary |
| JWT bearer authentication | Provides the first secure API access layer |
| Workspace creation | Establishes the future tenant model |
| Workspace membership roles | Separates owner-level administration from member access |
| One-time invitation codes | Enables controlled workspace onboarding |
| Server-side authorization checks | Keeps workspace access rules in the API, not the browser |
| React workspace shell | Provides the first authenticated product surface |
| OpenAPI and Scalar | Keeps the API inspectable during development |
| Local Docker infrastructure | Makes database-backed development repeatable |
| Full quality gate | Keeps the foundation clean while the product grows |

## Tech Stack

| Layer | Technologies |
| --- | --- |
| API | .NET 9, ASP.NET Core Minimal APIs, C# |
| Graph engine | C++20 native library with a stable C ABI and .NET interop wrapper |
| Persistence | PostgreSQL 16, Entity Framework Core, Npgsql |
| Authentication | JWT bearer tokens, PBKDF2 password hashing |
| Web | React 19, TypeScript, Vite |
| Styling | Tailwind CSS 4, shadcn-style primitives, Radix UI |
| Testing | xUnit, WebApplicationFactory, Vitest, Testing Library, Playwright |
| Tooling | pnpm, Make, Docker Compose, GitHub Actions |

## Architecture

The API is split into a kernel, a toolbox and feature modules, with one-way
dependencies (`modules → shared → core`) enforced by a test:

| Layer | Responsibility |
| --- | --- |
| `src/api/core` | Module contract (`IModule`), host setup, `Result`/`Error`, result-to-HTTP mapping, `CurrentUser`, persistence and configuration |
| `src/api/shared` | Pure helpers: `ValidationErrors`, `TextNormalizer`, `EnumCode` |
| `src/api/modules` | Feature modules built on core and shared |

Each feature module follows the same structure:

```text
src/api/modules/<feature>
├── Application      # Use cases and ports, returning core Result values
├── Contracts        # HTTP request and response contracts
├── Domain           # Domain records and enums
├── Infrastructure   # EF entities, configurations, stores
├── <Feature>Module.cs     # IModule: registers services and endpoints
└── <Feature>Endpoints.cs
```

Current modules:

| Module | Responsibility |
| --- | --- |
| `auth` | Account registration, login, password hashing, JWT issuing |
| `workspaces` | Workspace lifecycle, invitations, membership, authorization rules |
| `projects` | Workspace-owned projects with tenant-scoped reads and owner-only deletion |
| `boards` | Project boards: templates, lists with an explicit workflow status, cards, assignees, collaboration, and dependencies. Board cards are the single source of truth for project work |
| `health` | Runtime health endpoint |

The task dependency graph is backed by a native C++ graph engine in
`src/graph-engine`. The API consumes it through an `IGraphEngine<Guid>` port, so
board pages can rely on one cycle-safe relationship engine while the native
implementation can evolve independently.

The native graph engine is split by responsibility:

```text
src/graph-engine
├── include/coordina_graph_engine.h  # Stable C ABI consumed by .NET
└── src
    ├── core/                        # Node/edge types, immutable graph snapshot
    ├── algorithms/                  # Traversal, cycle-safe validation, topological
    │                                # order, critical path, node analysis, suggestions
    ├── planning/                    # Shared board analysis (planning_analysis), next-task
    │                                # ranking (work_planner), score rules (work_score)
    └── interop/                     # C ABI exports and array conversion helpers
```

Layers depend inward only: `core` ← `algorithms` ← `planning` ← `interop`.
The board planning view uses one native call, `coordina_graph_analyze_board`,
which builds the graph once and returns readiness, dependency order, critical
path, node analyses, and ranked work together. It reports an explicit status
(`OK`, `INVALID_ARGUMENT`, `INSUFFICIENT_CAPACITY`) separately from output
counts. `make graph-engine-benchmark` compares it with the individual calls.
Includes are rooted at `src/graph-engine/src` (for example
`#include "algorithms/critical_path.h"`).

The web application mirrors this direction with feature folders plus shared
contracts and infrastructure:

```text
src/web/src
├── features      # Product areas and UI/state owned by those areas (board: api/, model/, hooks/, components/)
├── lib           # Shared utilities and the API client
└── types         # Shared API/domain contracts used across features
```

## Repository Map

```text
.
├── src
│   ├── api                  # ASP.NET Core API
│   └── web                  # React application
├── tests
│   ├── api                  # Fast API tests (in-memory stores)
│   ├── api-postgres         # API tests against a real PostgreSQL 16 (Testcontainers, Docker required)
│   └── graph-engine         # Native C++ graph engine tests
├── docker                   # PostgreSQL and Adminer compose services
├── .github                  # CI workflows, issue/PR templates, CODEOWNERS, Dependabot
├── Makefile                 # Local workflow entrypoint
├── CONTRIBUTING.md          # Contribution workflow
├── CODE_OF_CONDUCT.md       # Community standards
├── SUPPORT.md               # Where to ask for help
├── SECURITY.md              # Security policy
└── LICENSE                  # MIT license
```

## Getting Started

Requirements:

| Tool | Version |
| --- | --- |
| .NET SDK | 9.x |
| Node.js | 24.x recommended |
| pnpm | 10.17.x recommended |
| Docker | Current stable |

Create local configuration:

```bash
cp .env.example .env
```

Install frontend dependencies:

```bash
pnpm --dir src/web install
```

Start PostgreSQL and Adminer:

```bash
make docker-up
```

Apply migrations:

```bash
make api-migrate
```

Run the full development app:

```bash
make dev
```

Local services:

| Service | URL |
| --- | --- |
| Web app | http://localhost:5173 |
| API | http://localhost:5050 |
| API reference | http://localhost:5050/api-docs |
| OpenAPI schema | http://localhost:5050/openapi/v1.json |
| Mintlify docs | http://localhost:3000 |
| Adminer | http://localhost:8080 |

## Environment

The local `.env` file follows this shape:

```dotenv
POSTGRES_USER=coordina
POSTGRES_PASSWORD=change-me
POSTGRES_DB=coordina
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
ADMINER_PORT=8080
ASPNETCORE_ENVIRONMENT=Development

Auth__Issuer=Coordina.Api
Auth__Audience=Coordina.Web
Auth__SigningKey=change-me-to-a-secure-random-secret-with-at-least-32-characters
Auth__AccessTokenMinutes=60
```

The checked-in example is for local development only. Use a unique signing key and managed secrets outside local environments.

## Workflow

The Makefile is the main local interface.

| Command | Purpose |
| --- | --- |
| `make graph-engine-test` | Build and run the native C++ graph engine tests |
| `make graph-engine-benchmark` | Benchmark native board analysis (individual calls vs batch) |
| `make dev` | Run API watcher and web dev server together |
| `make api` | Apply migrations and run the API |
| `make web` | Run the Vite dev server |
| `make api-test` | Run all .NET tests (fast + PostgreSQL) |
| `make api-test-fast` | Run .NET tests against in-memory stores |
| `make api-test-postgres` | Run .NET tests against a disposable PostgreSQL container (Docker required) |
| `make web-test` | Run Vitest tests |
| `make web-e2e` | Run Playwright tests |
| `make lint` | Run format verification, lint, and typecheck |
| `make test` | Run API, web, and e2e tests |
| `make format` | Format API and web sources |
| `make quality` | Run the same full gate expected by CI |

## Quality Gate

The CI workflow runs on pushes and pull requests targeting `master`.

```bash
make quality
```

The gate verifies:

| Check | Tooling |
| --- | --- |
| API formatting | `dotnet format --verify-no-changes` |
| Web formatting | Prettier |
| Web lint | ESLint |
| Web type safety | TypeScript |
| API tests | xUnit (in-memory and real PostgreSQL via Testcontainers) |
| Web tests | Vitest |
| End-to-end flow | Playwright Chromium |
| API build | `dotnet build` |
| Web build | Vite production build |

The current e2e flow covers account creation, workspace onboarding, workspace creation, sign-out, sign-in, and return to the existing workspace.

## Security Direction

Coordina treats workspaces as the tenant boundary. The API owns the access rules.

Current rules include:

| Rule | Behavior |
| --- | --- |
| Workspace reads | Scoped to the authenticated user's memberships |
| Project reads | Nested under `/workspaces/{workspaceId}` and filtered by workspace id |
| Non-member access | Hidden behind `404` responses |
| Owner actions | Required for invites, member removal, workspace deletion, and permanent project deletion |
| Invite codes | One-time use, expiration-aware, stored hashed |
| Frontend state | Useful for UX, never trusted for authorization |

For vulnerability reporting and current security limits, read [SECURITY.md](SECURITY.md).

## Projects Module

Projects are the first structured work layer inside a workspace. V2 adds
lifecycle status, project ownership, metadata, audit fields, and soft deletion.

API endpoints:

```http
GET    /workspaces/{workspaceId}/projects
GET    /workspaces/{workspaceId}/projects?includeArchived=true
GET    /workspaces/{workspaceId}/projects?includeCompleted=true
POST   /workspaces/{workspaceId}/projects
GET    /workspaces/{workspaceId}/projects/{projectId}
PATCH  /workspaces/{workspaceId}/projects/{projectId}
DELETE /workspaces/{workspaceId}/projects/{projectId}            # archive
DELETE /workspaces/{workspaceId}/projects/{projectId}/permanent  # hard delete
```

Lifecycle:

| Status | Behavior |
| --- | --- |
| `ACTIVE` | Default status, returned by default list endpoint |
| `ARCHIVED` | Hidden by default, returned with `includeArchived=true` |
| `COMPLETED` | Hidden by default, returned with `includeCompleted=true`, read-only |

Permission model:

| Action | Server-side rule |
| --- | --- |
| Create project | Authenticated workspace member |
| List and view projects | Authenticated workspace member |
| Project details | Authenticated workspace member, project id scoped by workspace id |
| Edit project | Workspace OWNER or project owner, unless completed |
| Archive project | Workspace OWNER or project owner, unless completed |
| Restore project | Workspace OWNER or project owner |
| Permanent delete | Workspace OWNER only |

Frontend flow:

| Step | Behavior |
| --- | --- |
| Select workspace | Existing workspace context persists `coordina.activeWorkspaceId` |
| Open Projects | `/app/projects` loads active projects by default |
| Toggle filters | Archived/completed buttons call backend query params |
| Create project | Dialog posts name, description, key, icon, and color |
| Edit project | Dialog updates metadata/status through workspace-scoped API |
| Archive/delete | Confirmation dialog calls archive or permanent delete endpoint |

Project creation intentionally does not choose the board workflow. When a new
project is opened for the first time, the board module shows a focused setup
screen where the user can choose a template or create custom lists.

Manual testing:

```text
1. Register or sign in at /login.
2. Create a workspace from /app.
3. Open /app/projects and create a project.
4. Archive it and confirm it disappears from the default list.
5. Enable Archived and confirm it appears with ARCHIVED status.
6. Mark a project COMPLETED and confirm edits/archive return 409.
7. Invite a second user and assign project ownership.
8. Confirm the project owner can edit/archive that project.
9. Confirm another member receives 403 for edit/archive/delete.
10. Confirm a non-member receives 404 for project list/details.
```

Run backend tests:

```bash
make api-test-fast      # in-memory stores, no Docker needed
make api-test-postgres  # real PostgreSQL 16 in a disposable container
```

## Project Shell And Board System

Each project detail page is a container, not a single-purpose board screen.
The shell owns project metadata and module navigation, while each module owns
its own data and state.

Project shell:

| Area | Responsibility |
| --- | --- |
| Header | Project name, key, icon, description, and lifecycle badge |
| Tabs | Board, Docs, and Settings module navigation |
| Dynamic content | Renders the active project module without mixing concerns |

Current project modules:

| Tab | State |
| --- | --- |
| Board | Implemented Trello/Linear-style project board |
| Docs | Placeholder only: `Coming soon - Notion-like Docs` |
| Settings | Placeholder only: `Project settings coming soon` |

### Project Board System

Boards belong to a project, lists belong to boards, and cards belong to lists.
The frontend renders the board as a compact horizontal work surface with inline
list editing, quick card creation, native drag and drop, and a reusable right
side panel for card details.

```http
GET    /workspaces/{workspaceId}/projects/{projectId}/board
POST   /workspaces/{workspaceId}/projects/{projectId}/boards
POST   /workspaces/{workspaceId}/projects/{projectId}/boards/{boardId}/lists
PATCH  /workspaces/{workspaceId}/projects/{projectId}/boards/{boardId}/lists/{listId}
POST   /workspaces/{workspaceId}/projects/{projectId}/boards/{boardId}/lists/{listId}/cards
PATCH  /workspaces/{workspaceId}/projects/{projectId}/boards/{boardId}/cards/{cardId}
PATCH  /workspaces/{workspaceId}/projects/{projectId}/boards/{boardId}/cards/{cardId}/move
DELETE /workspaces/{workspaceId}/projects/{projectId}/boards/{boardId}/cards/{cardId}
```

Every list carries an explicit workflow status, `taskStatus`, set when the list
is created and never derived from its title: `TODO`, `IN_PROGRESS`, or `DONE`.
These are the same status categories Jira uses. Renaming a list never changes its
meaning, metrics, or WIP limits.

Board templates:

| Template | Generated lists (status) |
| --- | --- |
| `BASIC` | To Do (`TODO`), In Progress (`IN_PROGRESS`), Done (`DONE`) |
| `AGILE_SCRUM` | Backlog (`TODO`), Sprint (`TODO`), In Progress (`IN_PROGRESS`), Review (`IN_PROGRESS`), Done (`DONE`) |
| `BUG_TRACKING` | Reported (`TODO`), Investigating (`IN_PROGRESS`), Fixed (`IN_PROGRESS`), Released (`DONE`) |
| `PRODUCT_ROADMAP` | Ideas (`TODO`), Planned (`TODO`), In Progress (`IN_PROGRESS`), Shipped (`DONE`) |
| `CUSTOM` | First list `TODO`, last list `DONE` (when there are at least two), the rest `IN_PROGRESS` |

`POST .../lists` accepts an optional `taskStatus` (default `TODO`).

Card completion follows the list status:

| Action | Result |
| --- | --- |
| Create or move a card into a `DONE` list | The card is completed |
| Move a card out of a `DONE` list | The card is reopened |
| `PATCH` a card with `isCompleted: true` | The card moves to the board's first `DONE` list |
| `PATCH` a card with `isCompleted: false` | The card moves to the board's first `TODO` list |
| No list with the target status | `409 Conflict`, card unchanged |

Card fields:

| Field | Notes |
| --- | --- |
| `id` | Server-generated card id |
| `boardId` / `listId` | Mandatory board/list placement |
| `title` | Required, 180 characters or fewer |
| `description` | Editable in the side panel |
| `priority` | Optional `LOW`, `MEDIUM`, or `HIGH` |
| `dueDate` | Optional date shown compactly on cards |
| `labels` | Small colored tags for scanning |
| `isCompleted` / `completedAt` / `completedBy` | Kept consistent with the list's `taskStatus` |
| `assignees` | Multiple workspace members |
| `createdAt` / `updatedAt` | Server-managed audit timestamps |

The board fetch endpoint returns the full board structure in one request:
board, lists, cards, labels, and assignees. The persistence layer loads these
with batched queries rather than per-card lookups, leaving room for pagination
or virtualized lanes later if board sizes grow.

### Board Permission Rules

Board access is always verified through the parent project and workspace:

| Action | Server-side rule |
| --- | --- |
| Fetch board | Authenticated workspace member and project in that workspace |
| Create board | Authenticated workspace member and valid template |
| Create/update list | Authenticated workspace member |
| Create/update card | Authenticated workspace member |
| Move card | Authenticated workspace member and target list in same board |
| Assign card | Every assignee must be a workspace member |
| Delete card | Workspace OWNER or project owner |
| Cross-project access | Hidden behind `404` responses |
| Non-member access | Hidden behind `404` responses |

Completed projects remain read-only for board mutations, matching the project
lifecycle rule already enforced by the projects module.

### Future Expansion

The project shell reserves space for future modules. The Docs tab is
intentionally a placeholder for a Notion-like document system, and Settings is
intentionally isolated from task board state. Future modules should follow the
same pattern: project shell for layout/navigation, feature modules for their own
contracts, API client, state, and UI.

The board contracts are also prepared for future realtime updates, comments,
attachments, notifications, and activity logs. Those features are intentionally
not implemented yet; they should attach to board/card identifiers rather than
changing the project shell.

## Roadmap Direction

The next meaningful layers are product layers, not more scaffolding:

| Track | Intent |
| --- | --- |
| Boards | Core collaboration workflow; context-aware planning is in place (see `docs/boards.mdx`) |
| Realtime updates | Shared workspace presence and live state |
| Audit trail | Owner visibility into membership and sensitive actions |
| Production hardening | Rate limiting, refresh tokens, secret management, deploy docs |

## Documentation

| File | Purpose |
| --- | --- |
| [docs/](docs/) | Mintlify documentation source |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Local workflow, conventions, pull request expectations |
| [SECURITY.md](SECURITY.md) | Security policy, reporting, current guarantees and gaps |
| [src/web/README.md](src/web/README.md) | Web-specific setup and structure |

Run the documentation locally with:

```bash
make docs
```

Mintlify should be configured as a monorepo deployment with documentation path
`/docs`. The web app redirects `/docs` to `VITE_DOCS_URL`, while the API still
serves the local OpenAPI tester at `/api-docs`.

## License

Coordina is released under the [MIT License](LICENSE).
