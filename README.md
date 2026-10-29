<div align="center">
  <br />
  <h1>Coordina</h1>
  <p>A workspace-first collaboration platform foundation built with .NET, React, PostgreSQL, and a strict quality gate.</p>
  <p>
    <a href="https://github.com/Neptune2k21/Coordina/actions/workflows/quality.yml">
      <img src="https://github.com/Neptune2k21/Coordina/actions/workflows/quality.yml/badge.svg?branch=master" alt="Quality workflow status" />
    </a>
    <img src="https://img.shields.io/badge/status-pre--MVP-111827?style=flat-square" alt="Project status: pre-MVP" />
    <img src="https://img.shields.io/badge/license-MIT-111827?style=flat-square" alt="MIT License" />
  </p>
</div>

## About

Coordina is the technical foundation for a future collaboration product, not an MVP yet. It currently provides authentication, workspace tenancy and membership, project and board workflows, a React application shell, persistence, tests, CI, and documentation.

The project is in **pre-MVP** state and is not production-hardened. The API enforces workspace access rules; frontend state is never treated as authorization.

## Foundation Capabilities

- Account registration and sign-in with JWT bearer authentication.
- Workspace creation, membership roles, and invitation-based onboarding.
- Server-side authorization for workspace-owned resources.
- Projects and project boards for organizing collaborative work.
- OpenAPI/Scalar API inspection and repeatable local Docker infrastructure.
- Automated formatting, linting, type checking, tests, and builds.

## Tech Stack

- **API:** .NET 9, ASP.NET Core Minimal APIs, C#
- **Graph engine:** C++20 native library with a stable C ABI and .NET interop
- **Persistence:** PostgreSQL 16, Entity Framework Core, Npgsql
- **Authentication:** JWT bearer tokens, PBKDF2 password hashing
- **Web:** React 19, TypeScript, Vite, Tailwind CSS 4, Radix UI
- **Testing:** xUnit, WebApplicationFactory, Vitest, Testing Library, Playwright
- **Tooling:** pnpm, Make, Docker Compose, GitHub Actions

## Architecture at a Glance

The API is organized into core infrastructure, shared helpers, and feature modules. Dependencies flow inward from modules to shared code and core; a test enforces the intended direction.

| Area | Responsibility |
| --- | --- |
| `src/api/core` | Module contracts, host setup, results/errors, persistence, and configuration |
| `src/api/shared` | Reusable validation and normalization helpers |
| `src/api/modules` | Feature-specific application logic, contracts, infrastructure, and endpoints |
| `src/graph-engine` | Native graph algorithms and planning analysis, exposed through a stable C ABI |
| `src/web/src/features` | Product-area UI and state |
| `src/web/src/lib` and `types` | Shared utilities, API client, and contracts |

Current API modules include authentication, workspaces, projects, boards, and health checks. For module-level behavior and implementation details, use the [documentation site](docs/).

## Quickstart

Requirements: .NET SDK 9.x, Node.js 24.x, pnpm 10.17.x, and Docker.

```bash
cp .env.example .env
pnpm --dir src/web install
make docker-up
make api-migrate
make dev
```

The app and local services are available at:

| Service | URL |
| --- | --- |
| Web app | http://localhost:5173 |
| API | http://localhost:5050 |
| API reference | http://localhost:5050/api-docs |
| OpenAPI schema | http://localhost:5050/openapi/v1.json |
| Mintlify docs | http://localhost:3000 |
| Adminer | http://localhost:8080 |

The checked-in environment example is for local development only. Use a unique signing key and managed secrets outside local environments.

## Makefile Essentials

| Command | Purpose |
| --- | --- |
| `make dev` | Run the API watcher and web dev server |
| `make api` | Apply migrations and run the API |
| `make web` | Run the Vite dev server |
| `make test` | Run API, web, and end-to-end tests |
| `make lint` | Verify formatting, lint, and typecheck |
| `make quality` | Run the full quality gate used by CI |
| `make docs` | Build the documentation site |

For additional test targets, benchmarks, and formatting commands, see the [Makefile](Makefile).

## Quality Gate

The GitHub Actions quality workflow runs for pushes and pull requests targeting `master`. Run the same overall gate locally before opening a pull request:

```bash
make quality
```

The gate checks API formatting and builds, web formatting/lint/type safety, API and web tests, and the Playwright end-to-end flow. Native graph-engine tests are available through `make graph-engine-test`.

## Security Direction

Workspaces are the tenant boundary, and authorization is enforced by the API. Resource access is scoped to the authenticated user's workspace membership; non-member and cross-workspace access is hidden behind `404` responses where applicable. Owner-level actions and invitation rules are enforced server-side.

For vulnerability reporting, security guarantees, and known limitations, read [SECURITY.md](SECURITY.md).

## Roadmap Direction

The next meaningful layers are product capabilities rather than additional scaffolding:

- **Boards:** core collaboration workflow and context-aware planning.
- **Realtime updates:** shared workspace presence and live state.
- **Audit trail:** visibility into membership and sensitive actions.
- **Production hardening:** rate limiting, refresh tokens, secret management, and deployment documentation.

## Documentation and Contributing

Detailed endpoint references, project lifecycle behavior, board-system details, environment configuration, and manual test instructions belong in the matching pages under `docs/`, not duplicated here.

- [Documentation source](docs/)
- [Board system](docs/boards.mdx)
- [Contributing guide](CONTRIBUTING.md)
- [Security policy](SECURITY.md)
- [Support](SUPPORT.md)
- [Web app setup and structure](src/web/README.md)

Run the documentation workflow locally with `make docs`. Mintlify uses the repository's `docs/` directory; the web app redirects `/docs` to `VITE_DOCS_URL`, while the API serves its OpenAPI tester at `/api-docs`.

## License

Coordina is released under the [MIT License](LICENSE).
