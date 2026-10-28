import {
  CheckCircle,
  CircleNotch,
  GitBranch,
  Plus,
  WarningCircle,
  X,
} from "@phosphor-icons/react"
import { useMemo } from "react"

import { Button } from "@/components/ui/button"
import type {
  BoardCard,
  BoardCardDependencyAnalysis,
  BoardGraphCard,
} from "@/types/task"

type CardDependenciesSectionProps = {
  addDependency: () => Promise<void>
  card: BoardCard
  dependencyAnalysis: BoardCardDependencyAnalysis | null
  dependencyCandidates: BoardCard[]
  dependencyId: string
  isLoadingDependencyAnalysis: boolean
  isReadOnly: boolean
  isSaving: boolean
  onDeleteDependency: (
    card: BoardCard,
    dependsOnCardId: string
  ) => Promise<void>
  setDependencyId: (value: string) => void
}

export function CardDependenciesSection({
  addDependency,
  card,
  dependencyAnalysis,
  dependencyCandidates,
  dependencyId,
  isLoadingDependencyAnalysis,
  isReadOnly,
  isSaving,
  onDeleteDependency,
  setDependencyId,
}: CardDependenciesSectionProps) {
  const existingDependencyIds = useMemo(
    () => new Set(card.dependencies.map((dependency) => dependency.cardId)),
    [card]
  )
  const fallbackDependencies = dependencyCandidates
    .filter((candidate) => !existingDependencyIds.has(candidate.id))
    .map(toGraphCard)
  const availableDependencies =
    dependencyAnalysis?.suggestedDependencies ?? fallbackDependencies
  const blockingDependencies =
    dependencyAnalysis?.blockingDependencies ??
    card.dependencies
      .filter((dependency) => !dependency.isCompleted)
      .map((dependency) => ({
        dependencyCount: 0,
        dependencyDepth: 0,
        dependentCount: 0,
        dependentDepth: 0,
        id: dependency.cardId,
        isCompleted: dependency.isCompleted,
        isCriticalPath: false,
        listId: card.listId,
        title: dependency.title,
        transitiveDependentCount: 0,
      }))
  const impactedDependents = dependencyAnalysis?.impactedDependents ?? []
  const directlyUnlockedDependents =
    dependencyAnalysis?.directlyUnlockedDependents ?? []
  const unlockPotential =
    dependencyAnalysis?.transitiveDependentCount ?? impactedDependents.length
  const isOnCriticalPath = dependencyAnalysis?.isOnCriticalPath ?? false
  const isCardUnblocked =
    dependencyAnalysis?.isUnblocked ??
    card.dependencies.every((dependency) => dependency.isCompleted)

  return (
    <>
      <section className="grid gap-2">
        <h3 className="text-sm font-semibold">Dependencies</h3>
        <div
          className={`grid gap-2 rounded-md border px-2.5 py-2 text-xs ${
            isCardUnblocked
              ? "border-teal-500/20 bg-teal-500/10 text-teal-800 dark:text-teal-100"
              : "border-amber-500/25 bg-amber-500/10 text-amber-900 dark:text-amber-100"
          }`}
        >
          <div className="flex items-center gap-2">
            {isLoadingDependencyAnalysis ? (
              <CircleNotch className="size-3.5 animate-spin" />
            ) : isCardUnblocked ? (
              <CheckCircle className="size-3.5" weight="fill" />
            ) : (
              <WarningCircle className="size-3.5" weight="fill" />
            )}
            <span className="font-semibold">
              {isLoadingDependencyAnalysis
                ? "Analyzing"
                : isCardUnblocked
                  ? "Unblocked"
                  : `${blockingDependencies.length} blocker${
                      blockingDependencies.length === 1 ? "" : "s"
                    }`}
            </span>
          </div>
          <div className="flex flex-wrap gap-1.5 text-[11px]">
            {isOnCriticalPath ? <span>critical path</span> : null}
            {unlockPotential > 0 ? (
              <span>{unlockPotential} downstream</span>
            ) : null}
            {directlyUnlockedDependents.length > 0 ? (
              <span>{directlyUnlockedDependents.length} unlock next</span>
            ) : null}
            <span>{availableDependencies.length} safe suggestions</span>
          </div>
        </div>
        {blockingDependencies.length > 0 ? (
          <div className="grid gap-1.5">
            {blockingDependencies.map((dependency) => (
              <DependencyCompactRow
                key={dependency.id}
                dependency={dependency}
                tone="blocked"
              />
            ))}
          </div>
        ) : null}
        {card.dependencies.length > 0 ? (
          <div className="grid gap-1.5">
            {card.dependencies.map((dependency) => (
              <div
                key={dependency.cardId}
                className="flex items-center gap-2 rounded-md border border-zinc-950/10 bg-white p-2 text-xs dark:border-white/10 dark:bg-zinc-950"
              >
                <CheckCircle
                  className={`size-4 ${
                    dependency.isCompleted
                      ? "text-teal-600 dark:text-teal-300"
                      : "text-muted-foreground"
                  }`}
                  weight={dependency.isCompleted ? "fill" : "regular"}
                />
                <span className="min-w-0 flex-1 truncate">
                  {dependency.title}
                </span>
                {!isReadOnly ? (
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="size-7 rounded-md"
                    disabled={isSaving}
                    onClick={() =>
                      void onDeleteDependency(card, dependency.cardId)
                    }
                  >
                    <X className="size-3.5" />
                  </Button>
                ) : null}
              </div>
            ))}
          </div>
        ) : (
          <p className="text-xs leading-5 text-muted-foreground">
            No dependency.
          </p>
        )}
        {!isReadOnly ? (
          <div className="flex gap-2">
            <select
              value={dependencyId}
              disabled={availableDependencies.length === 0 || isSaving}
              className="h-8 min-w-0 flex-1 rounded-md border border-zinc-950/10 bg-white px-2 text-xs dark:border-white/10 dark:bg-zinc-950"
              onChange={(event) => setDependencyId(event.target.value)}
            >
              <option value="">Select card</option>
              {availableDependencies.map((candidate) => (
                <option key={candidate.id} value={candidate.id}>
                  {candidate.title}
                </option>
              ))}
            </select>
            <Button
              type="button"
              size="sm"
              className="h-8 rounded-md px-2"
              disabled={!dependencyId || isSaving}
              onClick={() => void addDependency()}
            >
              <Plus className="size-3.5" />
            </Button>
          </div>
        ) : null}
      </section>

      {directlyUnlockedDependents.length > 0 ? (
        <section className="grid gap-2">
          <h3 className="text-sm font-semibold">Unlocks next</h3>
          <div className="grid gap-1.5">
            {directlyUnlockedDependents.slice(0, 5).map((dependent) => (
              <DependencyCompactRow
                key={dependent.id}
                dependency={dependent}
                tone="unlock"
              />
            ))}
          </div>
        </section>
      ) : null}

      {impactedDependents.length > 0 ? (
        <section className="grid gap-2">
          <h3 className="text-sm font-semibold">Impact</h3>
          <div className="grid gap-1.5">
            {impactedDependents.slice(0, 5).map((dependent) => (
              <DependencyCompactRow
                key={dependent.id}
                dependency={dependent}
                tone="impact"
              />
            ))}
          </div>
        </section>
      ) : null}
    </>
  )
}

function toGraphCard(card: BoardCard): BoardGraphCard {
  return {
    dependencyCount: card.dependencies.length,
    dependencyDepth: card.dependencies.length > 0 ? 1 : 0,
    dependentCount: 0,
    dependentDepth: 0,
    id: card.id,
    isCompleted: card.isCompleted,
    isCriticalPath: false,
    listId: card.listId,
    title: card.title,
    transitiveDependentCount: 0,
  }
}

function DependencyCompactRow({
  dependency,
  tone,
}: {
  dependency: BoardGraphCard
  tone: "blocked" | "impact" | "unlock"
}) {
  return (
    <div className="flex min-w-0 items-center gap-2 rounded-md border border-zinc-950/10 bg-white p-2 text-xs dark:border-white/10 dark:bg-zinc-950">
      {tone === "blocked" ? (
        <WarningCircle className="size-3.5 shrink-0 text-amber-600 dark:text-amber-300" />
      ) : tone === "unlock" ? (
        <CheckCircle className="size-3.5 shrink-0 text-teal-600 dark:text-teal-300" />
      ) : (
        <GitBranch className="size-3.5 shrink-0 text-sky-600 dark:text-sky-300" />
      )}
      <span className="min-w-0 flex-1 truncate">{dependency.title}</span>
    </div>
  )
}
