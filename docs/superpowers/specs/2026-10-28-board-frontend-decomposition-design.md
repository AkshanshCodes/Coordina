# Board Frontend Decomposition Design (Lot 2)

## Context

Lot 1 made board cards the only source of project work and gave lists an explicit
`taskStatus`. The web feature still carries the legacy name `features/tasks`, and
three files mix responsibilities that change for different reasons:

| File | Lines | Mixed responsibilities |
| --- | --- | --- |
| `use-project-board.ts` | 723 | Board/members/graph/analysis loading, 16 mutations, optimistic updates, error mapping, notices, UI state (quick add, new list, drag, selection, planning panel) |
| `board-utils.ts` | 480 | Selectors and metrics, immutable updates, label normalization, date helpers, Tailwind class helpers, formatting |
| `use-card-panel-form.ts` | 350 | Draft state, change detection, validation, label rules, DOM outside-click/Escape listeners, async submit actions |

Lot 3 (context-aware planning) will add planning preferences, effort fields, and
graph reload rules. Doing that on top of these files would make them worse.

## Intent

Split the board feature by reason for change, with pure logic testable without
React. Keep the components and the visual design unchanged.

## Defects fixed along the way (verified in current code)

1. **Stale board load.** `loadBoard` has no stale-response guard. Switching
   project A → B quickly can let A's response land after B's and display A's
   board under project B.
2. **Rollback overwrites newer state.** Failed optimistic moves and renames restore
   the snapshot captured before the request, even when another update has
   replaced the board since.
3. **A 404 on a card mutation hides the whole board.** `handleRequestError` treats
   any 404 as "no board" and sets the board to `null`, which shows the template
   chooser. Example: a card deleted by another member, then edited here.
   Mutations now reload the board on 404. Only a 404 on the board load itself
   means "no board yet".

## Non-goals

- No visual change, no component rewrite, no new state library.
- No change to API contracts or backend.
- No planning feature work (Lot 3).

## Target structure

```text
features/board/                    (renamed from features/tasks)
  api/
    board-api.ts                   HTTP calls only (was task-api.ts)
  model/                           pure, no React imports
    board-constants.ts             templates, priorities
    board-selectors.ts             filtering, metrics, WIP, counts, card predicates
    board-updates.ts               immutable optimistic transforms
    board-format.ts                Tailwind class helpers, initials, dates for display
    card-draft.ts                  draft from card, change detection, validation, label rules
    dates.ts                       date keys; `today` is injectable everywhere it matters
    labels.ts                      label normalization and comparison
    planning-insights.ts           recommendation explanations (was plan-insights.ts)
  hooks/
    use-board-resource.ts          board + members load (stale-safe), graph, card dependency analysis
    use-board-mutations.ts         server mutations, optimistic updates, safe rollback, notices
    use-board-filters.ts           unchanged behavior
    use-card-panel-form.ts         form orchestration over card-draft
    use-dismissible-layer.ts       outside-pointer/focus/Escape dismissal for one layer
    use-project-board.ts           composition + UI state; same return shape as today
  components/                      unchanged except import paths
```

`task-types.ts` has no importers and is deleted.

### Responsibilities

- **Mutations return outcomes, not UI resets.** `createList(title)` returns
  `true` on success. The composition hook clears the input and closes the form.
  The mutation hook knows nothing about quick-add or selection state.
- **Rollback is conditional.** An optimistic update records the optimistic board.
  On failure it restores the previous board only if the current state is still
  that optimistic board (`setBoard(current => current === optimistic ? previous : current)`).
- **Error mapping is split by origin.** Load errors: a 404 on the board means no
  board. Mutation errors: a 404 triggers a reload. In both cases a 401 signs out
  and other errors show the API message.
- **Time is injectable.** `isCardOverdue`, `isCardDueSoon`, `getBoardMetrics`,
  `filterBoard`, `dateBadgeClass`, and `validateDraft` accept `today` (default
  `todayDateKey()`).

## Tests

Characterization tests pin the current behavior before code moves, and new tests
cover the fixes:

- `model/board-selectors.test.ts`: metrics from `isCompleted`, WIP by status,
  filters (focus, label, assignee, search), overdue/due-soon with injected `today`.
- `model/board-updates.test.ts`: move/remove/rename are immutable and leave
  untouched lists by reference.
- `model/card-draft.test.ts`: draft from card, change detection (labels
  case-insensitive, assignee order), validation, label toggle and add limits.
- `hooks/use-board-resource.test.tsx`: a slower response for a previous project is
  ignored.
- `hooks/use-board-mutations.test.tsx`: a failed move restores the previous board;
  a failed move does not overwrite a newer board; a 404 reloads instead of clearing.

## Acceptance criteria

1. `features/tasks` no longer exists; `project-shell.tsx` imports from `features/board`.
2. No file under `model/` imports React.
3. `useProjectBoard` returns the same keys as before; `project-board.tsx` changes only its imports.
4. The three defects above have failing-then-passing tests.
5. `pnpm test`, `typecheck`, `lint`, `format:check`, and `build` pass.
