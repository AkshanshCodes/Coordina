# Board Data Foundation Design (Lot 1)

## Context

Coordina is pre-MVP and not deployed. There is no production data and no external
API consumer. This lot therefore removes legacy surfaces instead of preserving them.

It replaces the archived `2026-10-27-task-board-planning-foundation` design, which
bundled four independent projects and protected compatibility that no user needs.
Later lots are listed at the end of this document.

## Intent

A board card is the only representation of project work, and its workflow state is
explicit: renaming a list never changes what a card means. The persistence layer is
tested against real PostgreSQL, starting with the one known transactional defect.

## Current State (verified 2026-10-28)

- `src/api/modules/tasks` persists flat tasks in a `tasks` table. Nothing uses it:
  the web app calls only the Boards API, and no other module references Tasks.
- Workflow meaning is inferred from list titles in the web app
  (`isDoneListTitle`, `isActiveWorkListTitle` in `board-utils.ts`). Metrics and WIP
  limits change when a list is renamed.
- Card completion (`is_completed`) is independent of the list. Moving a card into
  `Done` does not complete it; completing a card does not move it.
- `PostgresWorkspaceStore.ConsumeInviteAsync` consumes the invite with
  `ExecuteUpdateAsync` (committed immediately), then inserts the membership with a
  separate `SaveChangesAsync`. A failed insert leaves a consumed invite and no
  member. A lost race throws `InvalidOperationException`, which surfaces as a 500.
  There is no `BeginTransaction` anywhere in `src/api`.
- API integration tests replace every store with an in-memory implementation; no
  test exercises EF mappings, migrations, constraints, or transactions.

## Goals

1. Delete the Tasks module, its endpoints, and the `tasks` table.
2. Give every board list an explicit, rename-safe `taskStatus`.
3. Keep card completion consistent with list status.
4. Make the web app read workflow state from `taskStatus`, never from titles.
5. Add a PostgreSQL integration test project that runs real migrations and stores.
6. Make invite consumption and membership creation atomic, with no 500 on races.

## Non-goals

- Frontend restructuring (Lot 2) and planning/effort features (Lot 3).
- Changing a list's status after creation (see Open Follow-ups).
- Any change to the native graph engine.

## Design

### Removing Tasks

Delete `src/api/modules/tasks`, `tests/api/Integration/Tasks`, the
`AddTasksModule`/`MapTasksEndpoints` calls in `Program.cs`, the
`AddInMemoryTaskStoreForTests` call in the test factory, and the `Tasks` DbSet.
A migration `RemoveLegacyTasks` drops the `tasks` table. Its `Down` recreates the
empty table (EF default). No data is copied.

The web feature folder stays named `features/tasks`; renaming it belongs to Lot 2.

### Explicit list status

`BoardTaskStatus { Todo, InProgress, Done }` serialized in the API and database as
`TODO`, `IN_PROGRESS`, `DONE`. The mapping is explicit in a small
`BoardTaskStatusCodec` (parse/format). `ToString().ToUpperInvariant()`, used for
priority, would produce `INPROGRESS` and must not be used.

`task_status` is a non-null `varchar(16)` on `board_lists` with a check constraint
on the three values. It flows through `BoardListEntity`, `ProjectBoardList`,
`BoardListSeed`, and `BoardListResponse` (`taskStatus`).

Assignment rules, applied once at creation and never re-derived:

| Source | Rule |
| --- | --- |
| Template | Declared per list in `BoardTemplateCatalog`, following Jira status categories: not-started lists (Backlog, Sprint, Ideas, Planned, Reported, To Do) `TODO`; final delivered list `DONE`; others `IN_PROGRESS` |
| Custom board (`customListTitles`) | First `TODO`, last `DONE` when ≥2 lists, middle `IN_PROGRESS`; a single list is `TODO` |
| `POST .../lists` | Optional `taskStatus`, default `TODO`; invalid values return a validation problem |
| Rename (`PATCH .../lists/{id}`) | Status unchanged |

The migration `AddBoardListTaskStatus` backfills existing rows with the custom
position rule (rank by `position, created_at, id` within each board), corrects the
template lists `Sprint` and `Planned` to `TODO`, then aligns
existing cards with the invariant below (cards in `DONE` lists completed with
`completed_at = updated_at`; completed cards in other lists reopened). This
rewrites dev data only.

### Completion invariant

`card.isCompleted ⇔ card's list.taskStatus = DONE`.

| Operation | Behavior |
| --- | --- |
| Create card in a `DONE` list | Created completed (`completedAt`, `completedBy` = creator) |
| Move into a `DONE` list | Completes the card |
| Move out of a `DONE` list | Reopens the card (clears completion fields) |
| `PATCH` with `isCompleted: true` | Moves to the first `DONE` list (lowest position) on the board |
| `PATCH` with `isCompleted: false` | Moves to the first `TODO` list on the board |
| Target status has no list | `409 Conflict` with an actionable message; card unchanged |

The service resolves the target list (`BoardWorkflow.FindFirstList`) before
calling the store. Field changes and the list change persist in one
`SaveChangesAsync` (Postgres) or one critical section (in-memory). A moved card is
appended at the end of the target list.

The web app already replaces local state with the board returned by the server, so
a card that moves after completion shows up in its new list once the response
arrives. The web app does not need to predict the move.

### Web app

- `BoardList` gains `taskStatus: "TODO" | "IN_PROGRESS" | "DONE"`.
- Metrics count `card.isCompleted` (now equivalent to list status).
- WIP limits apply to lists with `taskStatus === "IN_PROGRESS"`.
- `isDoneListTitle` and `isActiveWorkListTitle` are deleted.
- A 409 from completion shows the API message through the existing error surface.

### PostgreSQL integration tests

`tests/api-postgres/Coordina.Api.Postgres.Tests.csproj` (xUnit, Testcontainers
PostgreSQL module, `Microsoft.AspNetCore.Mvc.Testing`). One `postgres:16` container
per test run (collection fixture). Each test class gets a randomly named database,
applies migrations with `Database.MigrateAsync()`, and drops the database on
dispose. The web factory uses environment `Testing` (which skips `AddPostgres`) and
registers `CoordinaDbContext` with the disposable connection string. It registers no
in-memory stores.

Docker-unavailable failures are reported with a message starting
`PostgreSQL integration tests require Docker:`.

Scenarios in this lot:

- all migrations apply to an empty database;
- list status round-trips and survives rename;
- completion invariant holds through real stores (create/move/complete/reopen/409);
- concurrent joins with one invite → exactly one membership, one success, one
  `404`/`409`, no 500;
- membership insert failure → invite not consumed;
- reads and writes with a foreign `workspaceId`/`projectId` return nothing.

Make targets: `api-test-fast` (in-memory suite only), `api-test-postgres`,
`api-test` (both; unchanged command since both projects are in the solution).
CI already has Docker, so `make quality` keeps working.

### Atomic invite consumption

`ConsumeInviteAsync` returns `ConsumeInviteOutcome(ConsumeInviteStatus Status,
Workspace? Workspace)` with `Consumed | Unavailable | AlreadyMember`, and never
throws for expected races. The Postgres store opens a transaction, runs the
conditional `ExecuteUpdateAsync`, inserts the membership, saves, and commits. If 0
rows are updated, it rolls back and returns `Unavailable`. If saving hits a
unique-membership `DbUpdateException` (Postgres `23505`), it rolls back, clears the
change tracker, and returns `AlreadyMember`. `WorkspaceService` maps these to the
existing result statuses. The in-memory store matches this behavior.

## Acceptance Criteria

1. No code, table, endpoint, or test references the Tasks module.
2. Every list response carries `taskStatus`; renaming a list leaves it unchanged.
3. The completion invariant holds for every card mutation in both stores, and the
   missing-target case returns 409 without mutation.
4. The web app contains no title-based workflow inference; metrics and WIP limits
   are unchanged when a list is renamed.
5. PostgreSQL tests pass for migrations, invariants, tenant scoping, and the invite
   race/rollback.
6. `make quality` passes.

## Open Follow-ups (not in this lot)

- Editing a list's status (requires re-syncing its cards' completion).
- A status picker in the "add list" UI (the API supports it; the UI uses the default).

## Later Lots

Each lot gets its own spec when it starts, written against the code as it is then.

**Lot 2 — Board frontend decomposition.** Split `use-project-board.ts` (723 lines),
`board-utils.ts` (490), and `use-card-panel-form.ts` (349) by responsibility:
API, resource loading with stale-response protection, mutations with rollback,
pure selectors/updates/formatters, card draft reducer. Pure refactor, no behavior or
visual change, characterization tests first. Rename `features/tasks` → `features/board`
if it reads better. Independent of Lot 3; doing it first makes Lot 3's UI easier.

**Lot 3 — Context-aware planning.** The user-facing value: optional
`estimatedMinutes` on cards, assignment affinity for the current user, in-progress
momentum (from `taskStatus`), optional label context, and available focus time.
Extend the existing `coordina_graph_work_item`/recommendation structs and
`work_planner.cpp`; add explainable reasons in the API; add focus/context controls
to the planning panel. Optionally replace the six native calls in
`BoardDependencyGraph.AnalyzeBoard` with one call. There is no need for a `_v2`
surface or legacy exports, because the ABI has a single in-repo consumer shipped
alongside it.

**Deferred until a measured need:** C++ folder reorganization (22 files / ~1,600
lines), 10k-node benchmarks, ABI versioning policy.
