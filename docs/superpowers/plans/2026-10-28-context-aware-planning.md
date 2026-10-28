# Context-Aware Planning Plan (Lot 3)

**Spec:** `docs/superpowers/specs/2026-10-28-context-aware-planning-design.md`

### Task 1: Native planner inputs, options, and scoring

- [x] Add native tests (RED): effort fit/unknown/too long, assignment with both options, in-progress momentum, context match, node-id tie-break, extreme blocker count.
- [x] Extend `WorkItem`, `WorkRecommendation`, add `PlanningOptions`; extend the C structs and `coordina_graph_plan_work`; update `native_bridge`; 64-bit clamped scoring; node-id tie-break; version `0.4.0`.
- [x] `make graph-engine-test`; commit `feat(graph): add context-aware planning signals`.

### Task 2: .NET wrapper

- [x] Wrapper tests (RED): preferences change ranking; new fields round-trip; null options default.
- [x] Extend `GraphWorkItem`, `GraphWorkRecommendation`, add `GraphPlanningOptions`/`GraphAssignment`; mirror structs; update `IGraphEngine.PlanWork`; keep the existing call site compiling with default options.
- [x] `make api-test-fast`; commit `feat(graph): expose planning options to .NET`.

### Task 3: Card effort

- [x] Fast and PostgreSQL tests (RED): create with effort, update, clear, reject 4 and 2881.
- [x] Domain/entity/config/mutation/requests/response/stores; migration `AddCardEstimatedMinutes` with check constraint.
- [x] `make api-test`; commit `feat: add card effort estimate`.

### Task 4: Planning API

- [x] Endpoint tests (RED): ranking with preferences, blocked never primary, shortest alternative, validation, determinism.
- [x] `BoardPlanningPreferences` parsing/validation; work items from user/list status/labels; structured plan item fields; reasons.
- [x] `make api-test`; commit `feat: expose context-aware recommendations`.

### Task 5: Web

- [x] Unit tests (RED): `planningQuery`, `pickPlanFocus`, plan tags, card draft effort.
- [x] Types, API query, graph reload on preferences, effort field, panel controls and no-fit message.
- [x] Web gate; commit `feat(web): add focus-aware planning controls`.

### Task 6: Docs

- [x] `docs/boards.mdx` planning section, README planning notes, engine version.
- [x] Commit `docs: document context-aware planning`.
