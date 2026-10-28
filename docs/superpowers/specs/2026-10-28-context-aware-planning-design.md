# Context-Aware Planning Design (Lot 3)

## Intent

The planning panel answers "what should I work on next?". Today it ranks ready
cards by due date, priority, dependency impact, critical path, subtask progress,
and position, and gives the same answer to every member. Lot 3 makes the answer
depend on who is asking and how much time they have, while keeping it
deterministic and explainable.

## Current State (verified 2026-10-28)

- `work_planner.cpp` scores a card from urgency, priority, unlock impact,
  critical-path bonus, subtask progress, and a 70-point penalty per blocker. Ties
  break on actionability, score, critical path, and position. There is no final
  tie-break on node id.
- Score components are `uint32` sums. They are bounded in practice, but
  `blockers * 70` is not guarded.
- `coordina_graph_plan_work` takes work items without user or time context. The
  only consumer of the C ABI is `NativeGuidGraphEngine` in the same repository,
  shipped in the same build.
- `BoardDependencyGraph.AnalyzeBoard` builds work items and returns reasons as free
  text, capped at four. The web app derives tags by parsing those strings
  (`reason.startsWith("Unlocks")`).
- Cards have no effort estimate. `GET .../graph` takes no parameters.
- Since Lot 1, each list has an explicit `taskStatus`, which gives a reliable
  "already in progress" signal.

## Goals

1. Optional effort estimate on cards (`estimatedMinutes`, 5–2,880, `null` = unknown).
2. Planner inputs: effort, assignment relative to the requester, in-progress state, label context match.
3. Planner options: available focus minutes, prefer my work, allow unassigned work.
4. Structured explanation fields in the API, so the UI never parses reason strings.
5. Focus-time and context controls in the planning panel. When nothing ready fits, show the shortest ready alternative.
6. A fully deterministic order, with node id as the final tie-break, and overflow-safe arithmetic.

## Non-goals

- Multi-user capacity planning, calendars, or saved preferences (preferences stay local to the page).
- Machine learning or adaptive weights.
- C++ folder reorganization, batch ABI, benchmarks (deferred until there is a measured need).

## Design

### Card effort

`board_cards.estimated_minutes integer NULL` with check
`estimated_minutes IS NULL OR estimated_minutes BETWEEN 5 AND 2880`. It flows
through the domain, entity, `BoardCardMutation`, and the create/update requests
and responses. The update request follows the `dueDate` pattern:
`estimatedMinutes` sets the value and `clearEstimatedMinutes: true` clears it.
Out-of-range values return a validation problem.

### Native planner

`coordina_graph_work_item` gains (all `uint32_t`):

| Field | Meaning |
| --- | --- |
| `estimated_minutes` | `0` = unknown |
| `assignment` | `0` unassigned, `1` assigned to requester, `2` assigned only to others |
| `is_in_progress` | card sits in an `IN_PROGRESS` list |
| `is_context_match` | card has the requested label |

New `coordina_graph_planning_options { uint32_t available_minutes; uint32_t prefer_assigned; uint32_t include_unassigned; }`,
passed by pointer to `coordina_graph_plan_work`. A null pointer means
"unrestricted, prefer assigned, include unassigned". The signature changes in
place and the engine version moves to `0.4.0`. There is no `_v2` or legacy export,
because the only consumer ships with the engine.

`coordina_graph_work_recommendation` gains `effort_score`, `assignment_score`,
`momentum_score`, `context_score`, and booleans `fits_available_time`,
`is_assignment_match`, `is_context_match`, `is_in_progress`.

Scoring (new components added to the existing ones, computed in `int64_t` and
clamped to `[1, UINT32_MAX]`):

| Signal | Effect |
| --- | --- |
| In progress | +30 (finish what is started) |
| Assigned to requester, `prefer_assigned` | +40 |
| Assigned only to others, `prefer_assigned` | −80 |
| Unassigned and `include_unassigned = 0` | −80 |
| Context match | +45 |
| Known effort ≤ available minutes | +40, `fits_available_time = 1` |
| Known effort > available minutes | −60 |
| Unknown effort with a time limit | 0, `fits_available_time = 0` (no artificial fit) |
| No time limit | 0, `fits_available_time = 1` |

Penalties lower a card's rank but never remove it, so work assigned to others
stays visible. Sort order: actionable first, then score, critical path, position,
and node id.

### API

`GET .../graph?availableMinutes=&context=&preferAssigned=&includeUnassigned=`

- `availableMinutes`: `0` (default, unrestricted) or 5–2,880.
- `context`: normalized like card labels (trim, strip `#`, spaces → `-`, max 32), matched case-insensitively.
- `preferAssigned`, `includeUnassigned`: default `true`.
- Invalid values return `400` with a validation problem; nothing is silently clamped.

The service builds work items from the authenticated user id and the list
statuses. Each plan item adds:

```json
{
  "estimatedMinutes": 30,
  "fitsAvailableTime": true,
  "isAssignedToMe": true,
  "isContextMatch": true,
  "isInProgress": false,
  "isShortestAlternative": false
}
```

When `availableMinutes > 0` and no actionable item fits, the actionable item with
the smallest known estimate gets `isShortestAlternative = true`. The free-text
`reasons` stay for compatibility, but the UI reads the structured fields.

### Web app

- `PlanningPreferences { availableMinutes, context }`, local state in `useProjectBoard`. `planningQuery()` serializes only non-default values.
- `useBoardResource` reloads the graph when the preferences change, reusing its stale-response guard. The board itself is not reloaded.
- The card panel adds an Effort field with presets: Unknown, 15m, 30m, 1h, 2h, 4h, 1 day.
- The planning panel header adds a Focus control (Any, 30m, 1h, 2h, half day, day) and a Context control (All labels plus board labels).
- `pickPlanFocus(planItems, availableMinutes)` (pure) returns the primary item, the queued items, and whether the primary is a no-fit alternative. In that case the panel says so instead of presenting it as a perfect match.
- Tags come from structured fields: "Fits 30m", "Assigned to you", "#backend", "In progress", "Effort unknown".

## Acceptance Criteria

1. Native tests: effort fit (30/60), unknown effort, assignment (me/other/unassigned with options), in-progress momentum, context match, node-id tie-break, no overflow with extreme blocker counts.
2. .NET wrapper round-trips the new fields; struct layout matches the header.
3. API tests: an assigned, context-matching 30-minute card outranks an equivalent 2-hour card under a 60-minute budget; blocked work is never primary; the no-fit case flags the shortest actionable alternative; invalid preferences return 400; repeated calls return the same order.
4. `estimatedMinutes` round-trips, can be cleared, rejects out-of-range values (fast and PostgreSQL suites).
5. Web: `planningQuery`, `pickPlanFocus`, card draft effort, and tag derivation have unit tests.
6. `make graph-engine-test`, `make api-test`, and the web gate pass.
