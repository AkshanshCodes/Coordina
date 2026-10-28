# Board Frontend Decomposition Plan (Lot 2)

**Spec:** `docs/superpowers/specs/2026-10-28-board-frontend-decomposition-design.md`

## Constraints

- Components keep their markup; only imports change.
- `model/` files import no React.
- Each task ends green: `pnpm --dir src/web test && pnpm --dir src/web typecheck && pnpm --dir src/web lint`.

### Task 1: Rename the feature folder

- [x] `git mv src/web/src/features/tasks src/web/src/features/board`, move `task-api.ts` → `api/board-api.ts`, `plan-insights.ts` → `model/planning-insights.ts`, `board-constants.ts` → `model/`, hooks → `hooks/`; delete unused `task-types.ts`.
- [x] Rewrite `@/features/tasks/...` imports, including `projects/components/project-shell.tsx`.
- [x] Gate, then commit `refactor(web): rename board feature folder`.

### Task 2: Split pure model modules

- [x] Write `board-selectors.test.ts`, `board-updates.test.ts` against the new module paths (RED: modules missing).
- [x] Split `board-utils.ts` into `dates.ts`, `labels.ts`, `board-selectors.ts`, `board-updates.ts`, `board-format.ts`; add injectable `today`.
- [x] Update component imports; delete `board-utils.ts`.
- [x] Gate, then commit `refactor(web): split board model modules`.

### Task 3: Card draft and dismissible layer

- [x] Write `card-draft.test.ts` (RED).
- [x] Add `card-draft.ts`; add `use-dismissible-layer.ts`; make `use-card-panel-form.ts` use both while keeping its return shape.
- [x] Gate, then commit `refactor(web): extract card draft logic`.

### Task 4: Resource and mutation hooks

- [x] Write `use-board-resource.test.tsx` (stale project response) and `use-board-mutations.test.tsx` (rollback, conditional rollback, 404 reload) (RED).
- [x] Add `use-board-resource.ts` and `use-board-mutations.ts`; reduce `use-project-board.ts` to composition with the same return keys.
- [x] Gate plus `pnpm --dir src/web build`, then commit `refactor(web): split board loading and mutations`.

### Task 5: Docs

- [x] Update `docs/frontend.mdx` and the README web tree for `features/board`.
- [x] Commit `docs: describe board feature structure`.
