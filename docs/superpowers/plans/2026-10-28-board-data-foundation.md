# Board Data Foundation Implementation Plan (Lot 1)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Delete the unused Tasks module, make board list workflow status explicit and keep card completion consistent with it, and cover persistence with real PostgreSQL tests, including an atomic invite fix.

**Spec:** `docs/superpowers/specs/2026-10-28-board-data-foundation-design.md`

**Tech Stack:** .NET 9, ASP.NET Core Minimal APIs, EF Core 9, Npgsql/PostgreSQL 16, xUnit, Testcontainers.PostgreSql, React 19, TypeScript, Vitest.

## Global Constraints

- Pre-MVP, nothing deployed: delete legacy code rather than adapting it. No compatibility facades.
- Every commit leaves the EF model, migrations, and model snapshot consistent. Schema changes ship with their migration in the same task.
- Scope every persistence query by workspace and project.
- List status is assigned once at creation/migration and never inferred from a title.
- Keep the fast in-memory suite; PostgreSQL tests supplement it.
- No generic repository, event bus, or new frontend state library.

## Review Focus

- Completion/reopen on a board without a target list returns 409 and leaves the card unchanged (Task 5).
- A failed membership insert never leaves a consumed invite; a lost race never returns 500 (Task 2).
- `IN_PROGRESS` round-trips as exactly that string in the API and the database, not `INPROGRESS` (Task 4).
- The backfill migration assigns a single list `TODO` and handles boards whose positions are not contiguous (Task 4).

---

### Task 1: PostgreSQL Test Harness

**Files:**
- Create: `tests/api-postgres/Coordina.Api.Postgres.Tests.csproj`
- Create: `tests/api-postgres/Support/PostgresContainerFixture.cs`
- Create: `tests/api-postgres/Support/PostgresDatabase.cs`
- Create: `tests/api-postgres/Support/PostgresApiFactory.cs`
- Create: `tests/api-postgres/Integration/Persistence/MigrationSmokeTests.cs`
- Modify: `Coordina.sln`
- Modify: `Makefile`

- [x] **Step 1: Create the project and a failing smoke test**

  Mirror package versions from `tests/api/Coordina.Api.Tests.csproj`, add the current stable `Testcontainers.PostgreSql`, and reference `src/api/Coordina.Api.csproj`. `MigrationSmokeTests.AllMigrationsApplyToEmptyDatabase` creates a fresh database, calls `Database.MigrateAsync()`, and asserts `GetPendingMigrationsAsync()` is empty and `GetAppliedMigrationsAsync()` ends with the latest migration id.

- [x] **Step 2: Implement fixtures**

  `PostgresContainerFixture` (xUnit collection fixture) starts one `postgres:16` container. If startup fails, rethrow with a message starting `PostgreSQL integration tests require Docker:`. `CreateDatabaseAsync()` creates `coordina_test_<guid:N>` through the admin connection and returns a `PostgresDatabase` exposing `ConnectionString`, `CreateDbContext()`, and `MigrateAsync()`. On dispose, it calls `NpgsqlConnection.ClearAllPools()` and drops the database `WITH (FORCE)`.

- [x] **Step 3: Implement the real-store web factory**

  `PostgresApiFactory : WebApplicationFactory<Program>` takes a `PostgresDatabase`, uses environment `Testing` (which skips `AddPostgres` in `Program.cs`), and registers `AddDbContext<CoordinaDbContext>(o => o.UseNpgsql(connectionString))`. Do not call any `AddInMemory*StoreForTests`. Add a helper that registers a user and returns an authenticated `HttpClient`, reusing the existing auth endpoints.

- [x] **Step 4: Solution and Make targets**

  Add the project to `Coordina.sln`. Add `api-test-fast` (`dotnet test tests/api/...`) and `api-test-postgres` (`dotnet test tests/api-postgres/...`). Leave `api-test` running the whole solution, so it covers both suites. Add both new targets to `help`.

- [x] **Step 5: Run**

  Run: `make api-test-postgres`
  Expected: PASS, one test, container started and removed.

- [x] **Step 6: Commit** — `test: add PostgreSQL integration harness`

### Task 2: Atomic Invite Consumption

**Files:**
- Modify: `src/api/modules/workspaces/Application/IWorkspaceStore.cs`
- Modify: `src/api/modules/workspaces/Application/WorkspaceService.cs`
- Modify: `src/api/modules/workspaces/Infrastructure/PostgresWorkspaceStore.cs`
- Modify: `src/api/modules/workspaces/Infrastructure/InMemoryWorkspaceStore.cs`
- Create: `tests/api-postgres/Integration/Workspaces/WorkspaceInvitePostgresTests.cs`

- [x] **Step 1: Write failing PostgreSQL tests**

  - `ConcurrentJoin_ConsumesInviteExactlyOnce`: two users join with the same code via `Task.WhenAll`. Expect one 2xx, one 404/409, zero 500, one new membership row, `consumed_at` set.
  - `MembershipInsertFailure_DoesNotConsumeInvite`: insert the membership row directly through `CreateDbContext()` after the service's membership pre-check (or call the store directly) so the insert hits the unique constraint. Expect `consumed_at IS NULL`.

- [x] **Step 2: Confirm RED**

  Run: `dotnet test tests/api-postgres/Coordina.Api.Postgres.Tests.csproj --filter FullyQualifiedName~WorkspaceInvitePostgresTests -m:1`
  Expected: rollback test fails (invite consumed); race test returns a 500.

- [x] **Step 3: Implement**

  Add `ConsumeInviteStatus { Consumed, Unavailable, AlreadyMember }` and `ConsumeInviteOutcome(ConsumeInviteStatus Status, Workspace? Workspace)`. In `PostgresWorkspaceStore`, wrap the conditional `ExecuteUpdateAsync`, the member `Add`, and `SaveChangesAsync` in `Database.BeginTransactionAsync`. If 0 rows are updated, roll back and return `Unavailable`. On `DbUpdateException` with `PostgresException { SqlState: "23505" }`, roll back, `ChangeTracker.Clear()`, and return `AlreadyMember`. Remove the `InvalidOperationException` throws. Mirror the outcomes in `InMemoryWorkspaceStore` under its existing lock. In `WorkspaceService.JoinAsync`, map `Unavailable` → the existing "no longer usable" result and `AlreadyMember` → the existing "already belong" conflict.

- [x] **Step 4: Run**

  Run: `make api-test`
  Expected: PASS, no 500 in the race test.

- [x] **Step 5: Commit** — `fix: consume workspace invites atomically`

### Task 3: Remove the Tasks Module

**Files:**
- Delete: `src/api/modules/tasks/` (whole folder)
- Delete: `tests/api/Integration/Tasks/`
- Modify: `src/api/Program.cs`
- Modify: `src/api/infrastructure/persistence/CoordinaDbContext.cs`
- Modify: `tests/api/Support/ApiTestApplicationFactory.cs`
- Create (generated): `src/api/infrastructure/persistence/Migrations/<timestamp>_RemoveLegacyTasks.cs` + `.Designer.cs`
- Modify (generated): `CoordinaDbContextModelSnapshot.cs`
- Modify: `tests/api-postgres/Integration/Persistence/MigrationSmokeTests.cs`

- [x] **Step 1: Add a failing schema assertion**

  In `MigrationSmokeTests`, assert `SELECT to_regclass('public.tasks')` is null after migrating. Run `make api-test-postgres` → FAIL.

- [x] **Step 2: Delete code and wiring**

  Remove the folders, the `using`/`AddTasksModule()`/`MapTasksEndpoints()` lines in `Program.cs`, the `Tasks` DbSet and its `using` in `CoordinaDbContext`, and `AddInMemoryTaskStoreForTests()` in the test factory. `dotnet build Coordina.sln` must succeed.

- [x] **Step 3: Generate the migration**

  Run: `make api-migration name=RemoveLegacyTasks`
  Check that `Up` only drops `tasks` and `Down` recreates it.

- [x] **Step 4: Run**

  Run: `make api-test && grep -rn "Modules.Tasks\|TaskEntity\|/tasks\"" src tests`
  Expected: tests PASS; grep returns nothing.

- [x] **Step 5: Commit** — `refactor: remove unused Tasks module`

### Task 4: Explicit List Status

**Files:**
- Create: `src/api/modules/boards/Domain/BoardTaskStatus.cs` (enum + `BoardTaskStatusCodec` with `Format`/`TryParse`)
- Modify: `src/api/modules/boards/Domain/ProjectBoardList.cs`
- Modify: `src/api/modules/boards/Domain/BoardTemplateCatalog.cs` (`BoardListTemplate` gains `TaskStatus`)
- Modify: `src/api/modules/boards/Application/IBoardStore.cs` (`BoardListSeed` gains `TaskStatus`; `CreateListAsync` takes a status)
- Modify: `src/api/modules/boards/Application/BoardRules.cs` (custom position rule, status parsing)
- Modify: `src/api/modules/boards/Application/BoardListService.cs`
- Modify: `src/api/modules/boards/Application/BoardResponseMapper.cs`
- Modify: `src/api/modules/boards/Contracts/CreateBoardListRequest.cs` (`string? TaskStatus`)
- Modify: `src/api/modules/boards/Contracts/BoardListResponse.cs` (`string TaskStatus`)
- Modify: `src/api/modules/boards/Infrastructure/BoardListEntity.cs`
- Modify: `src/api/modules/boards/Infrastructure/BoardListEntityConfiguration.cs`
- Modify: `src/api/modules/boards/Infrastructure/PostgresBoardStore.cs`
- Modify: `src/api/modules/boards/Infrastructure/InMemoryBoardStore.cs`
- Create (generated + hand-edited): `Migrations/<timestamp>_AddBoardListTaskStatus.cs` + `.Designer.cs`, snapshot
- Test: `tests/api/Integration/Boards/BoardEndpointsTests.cs`
- Create: `tests/api-postgres/Integration/Boards/BoardWorkflowPostgresTests.cs`

- [x] **Step 1: Write failing fast tests**

  `TemplateBoards_ExposeDeclaredStatuses` (Basic: TODO/IN_PROGRESS/DONE; BugTracking: TODO/IN_PROGRESS/IN_PROGRESS/DONE; AgileScrum: TODO/TODO/IN_PROGRESS/IN_PROGRESS/DONE), `CustomBoard_AssignsStatusesByPosition` (1, 2, and 4 lists), `CreateList_DefaultsToTodo`, `CreateList_AcceptsInProgress`, `CreateList_RejectsUnknownStatus`, `RenameList_DoesNotChangeTaskStatus`. Assert the exact string `IN_PROGRESS`.

- [x] **Step 2: Confirm RED**

  Run: `dotnet test tests/api/Coordina.Api.Tests.csproj --filter FullyQualifiedName~BoardEndpointsTests -m:1`

- [x] **Step 3: Implement domain, rules, and contracts**

  Declare the status on each `BoardListTemplate` entry. In `BuildListSeeds`, apply the custom position rule. Parse the optional request status with `BoardTaskStatusCodec.TryParse` (case-insensitive) and add a `TaskStatus` validation error on failure. Map through both stores and the response mapper.

- [x] **Step 4: Configure the column and generate the migration**

  Map `task_status` as `varchar(16)`, required, with a value converter that uses the codec, and a check constraint `ck_board_lists_task_status` on `('TODO','IN_PROGRESS','DONE')`. Run `make api-migration name=AddBoardListTaskStatus`. Then hand-edit `Up`: add the column as nullable, backfill, align cards, set it `NOT NULL`, and add the constraint:

  ```sql
  UPDATE board_lists AS l
  SET task_status = CASE
    WHEN r.rank = 1 THEN 'TODO'
    WHEN r.rank = r.total THEN 'DONE'
    ELSE 'IN_PROGRESS'
  END
  FROM (
    SELECT id,
      ROW_NUMBER() OVER (PARTITION BY board_id ORDER BY position, created_at, id) AS rank,
      COUNT(*) OVER (PARTITION BY board_id) AS total
    FROM board_lists
  ) AS r
  WHERE l.id = r.id;

  UPDATE board_cards AS c
  SET is_completed = TRUE, completed_at = COALESCE(c.completed_at, c.updated_at)
  FROM board_lists AS l
  WHERE c.list_id = l.id AND l.task_status = 'DONE' AND NOT c.is_completed;

  UPDATE board_cards AS c
  SET is_completed = FALSE, completed_at = NULL, completed_by_user_id = NULL
  FROM board_lists AS l
  WHERE c.list_id = l.id AND l.task_status <> 'DONE' AND c.is_completed;
  ```

- [x] **Step 5: Write the PostgreSQL migration test**

  In `BoardWorkflowPostgresTests.Backfill_AssignsStatusesByPosition`: migrate to `RemoveLegacyTasks`, insert one board with 1 list and one with 3 lists at positions 0/5/9, plus a completed card in the middle list and an open card in the last list. Migrate to latest, then assert statuses and card completion. Also add `ListStatus_RoundTripsThroughApi` against the real store.

- [x] **Step 6: Run**

  Run: `make api-test`
  Expected: PASS.

- [x] **Step 7: Commit** — `feat: make board list workflow status explicit`

### Task 5: Completion Follows List Status

**Files:**
- Create: `src/api/modules/boards/Application/BoardWorkflow.cs` (`FindFirstList(ProjectBoard, BoardTaskStatus)`)
- Modify: `src/api/modules/boards/Application/IBoardStore.cs` (`UpdateCardAsync` accepts an optional target list id)
- Modify: `src/api/modules/boards/Application/BoardCardService.cs`
- Modify: `src/api/modules/boards/Infrastructure/PostgresBoardStore.cs`
- Modify: `src/api/modules/boards/Infrastructure/InMemoryBoardStore.cs`
- Test: `tests/api/Integration/Boards/BoardEndpointsTests.cs`
- Modify: `tests/api-postgres/Integration/Boards/BoardWorkflowPostgresTests.cs`

- [x] **Step 1: Write failing fast tests**

  `CreateCard_InDoneList_IsCompleted`, `MoveCard_IntoDoneList_Completes`, `MoveCard_OutOfDoneList_Reopens`, `CompleteCard_MovesToFirstDoneList`, `ReopenCard_MovesToFirstTodoList`, `CompleteCard_WithoutDoneList_Returns409AndPreservesCard` (single-list custom board), `UpdateCard_WithoutCompletionChange_StaysInList`.

- [x] **Step 2: Confirm RED**

  Run: `dotnet test tests/api/Coordina.Api.Tests.csproj --filter FullyQualifiedName~BoardEndpointsTests -m:1`

- [x] **Step 3: Implement**

  - **Create:** the store sets completion from the destination list's status (`completedBy` is the creator). `CreateCardAsync` in the service passes the user id.
  - **Move:** both stores set or clear `IsCompleted`, `CompletedAt`, and `CompletedByUserId` from the target list's status, in the same save or critical section as the list change. `MoveCardAsync` needs the acting user id for `completedBy`; add it to the store signature.
  - **Update:** when `request.IsCompleted` differs from the current value, load the board, resolve `BoardWorkflow.FindFirstList` for `DONE`/`TODO`, and return `BoardResultStatus.Conflict` with the message `This board has no "Done" list. Add one to complete cards.` (or the `TODO` equivalent) if none exists. Otherwise pass the target list id to `UpdateCardAsync`, which appends the card to that list and applies the fields in one save.

- [x] **Step 4: Add PostgreSQL coverage**

  `Completion_InvariantHoldsThroughRealStore` (create in Done, move out, complete, reopen) and `Complete_WithoutDoneList_LeavesRowUnchanged` (compare the row before and after the call).

- [x] **Step 5: Run**

  Run: `make api-test`
  Expected: PASS.

- [x] **Step 6: Commit** — `feat: keep card completion consistent with list status`

### Task 6: Web App Reads Explicit Status

**Files:**
- Modify: `src/web/src/types/task.ts` (`BoardTaskStatus` type, `BoardList.taskStatus`)
- Modify: `src/web/src/features/tasks/board-utils.ts`
- Modify: `src/web/src/features/tasks/components/project-board.tsx`
- Create or modify: `src/web/src/features/tasks/board-utils.test.ts`
- Modify: any test fixtures that build `BoardList` objects

- [x] **Step 1: Write failing tests**

  Metrics count completed cards from `card.isCompleted`. A list titled `Done` with status `TODO` contributes nothing to `completed`. Renaming any list leaves the metrics unchanged. Active-work/WIP selection uses `taskStatus === "IN_PROGRESS"`.

- [x] **Step 2: Implement**

  Replace `isDoneListTitle` and `isActiveWorkListTitle` with status checks, then delete both functions. In `project-board.tsx:247`, set `wipLimit` from `list.taskStatus === "IN_PROGRESS"`. Make sure a 409 from a completion toggle shows the API message through the existing error notice. Check that `use-card-panel-form.ts` resyncs from the returned card instead of keeping its optimistic `isCompleted`.

- [x] **Step 3: Run**

  Run: `pnpm --dir src/web test && pnpm --dir src/web typecheck && pnpm --dir src/web lint && grep -rn "ListTitle(" src/web/src`
  Expected: PASS; grep returns nothing.

- [x] **Step 4: Commit** — `feat(web): use explicit list status for board metrics`

### Task 7: Documentation and Full Verification

**Files:**
- Modify: `README.md` (module table: remove `tasks`; Boards owns project work; test commands)
- Modify: `docs/architecture.mdx` (remove the `tasks/ Flat project tasks (legacy surface)` line)
- Modify: `docs/testing.mdx` (fast vs PostgreSQL suites, Docker requirement, Make targets)
- Modify: `docs/api-reference.mdx` (`taskStatus` on lists, optional on create, completion/move semantics, 409)

- [x] **Step 1: Update docs**

- [ ] **Step 2: Full gate**

  Run: `make format && make quality`
  Expected: PASS (formatting, lint, native tests, fast + PostgreSQL API tests, Vitest, Playwright, builds).

- [ ] **Step 3: Audit**

  Check each of the 6 spec acceptance criteria against test names and command output. Run `git status --short` and `git diff --check`.

- [x] **Step 4: Commit** — `docs: document explicit board workflow and PostgreSQL tests`
