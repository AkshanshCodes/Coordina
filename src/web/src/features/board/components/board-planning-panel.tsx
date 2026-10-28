import {
  CheckCircle,
  ClockCountdown,
  GitBranch,
  type Icon,
  Target,
  UsersThree,
  WarningCircle,
} from "@phosphor-icons/react"

import type {
  BoardMetrics,
  WipAlert,
} from "@/features/board/model/board-selectors"
import {
  type PlanTone,
  getNoFitExplanation,
  getPlanExplanation,
  getPlanTags,
  getShortPlanReason,
  getToneClass,
  pickPlanFocus,
} from "@/features/board/model/planning-insights"
import {
  focusTimeOptions,
  formatMinutes,
  type PlanningPreferences,
} from "@/features/board/model/planning-preferences"
import type { BoardGraph, BoardGraphPlanItem } from "@/types/task"

type BoardPlanningPanelProps = {
  graph: BoardGraph | null
  labels: string[]
  metrics: BoardMetrics
  preferences: PlanningPreferences
  onPreferencesChange: (preferences: PlanningPreferences) => void
  onSelectCard: (cardId: string) => void
  wipAlerts: WipAlert[]
}

export function BoardPlanningPanel({
  graph,
  labels,
  metrics,
  preferences,
  onPreferencesChange,
  onSelectCard,
  wipAlerts,
}: BoardPlanningPanelProps) {
  const planItems = graph?.planItems ?? []
  const {
    primary: primaryItem,
    queued: queuedItems,
    isNoFitAlternative,
  } = pickPlanFocus(planItems, preferences.availableMinutes)
  const blockedPlanCount = planItems.filter((item) => !item.isActionable).length
  const criticalPath = graph?.criticalPath ?? []
  const criticalPathStart = criticalPath[0] ?? null
  const healthStats = [
    {
      icon: CheckCircle,
      label: "Done",
      tone: "teal",
      value: `${metrics.completionRate}%`,
    },
    {
      icon: ClockCountdown,
      label: metrics.overdue > 0 ? "Overdue" : "Due soon",
      tone: metrics.overdue > 0 ? "rose" : "amber",
      value:
        metrics.overdue > 0 ? String(metrics.overdue) : String(metrics.dueSoon),
    },
    {
      icon: WarningCircle,
      label: "Blocked",
      tone: metrics.blocked > 0 ? "rose" : "zinc",
      value: String(metrics.blocked),
    },
    {
      icon: UsersThree,
      label: "No owner",
      tone: metrics.unassigned > 0 ? "sky" : "zinc",
      value: String(metrics.unassigned),
    },
  ] satisfies HealthStatProps[]

  return (
    <div className="grid gap-3 rounded-md border border-zinc-950/10 bg-white p-3 text-xs shadow-xs xl:grid-cols-[minmax(0,1.35fr)_minmax(280px,0.65fr)] dark:border-white/10 dark:bg-white/[0.035]">
      <section className="min-w-0">
        <div className="mb-2 flex items-center justify-between gap-2">
          <div className="min-w-0">
            <p className="text-sm font-semibold">Suggested next step</p>
            <p className="mt-0.5 text-[11px] text-muted-foreground">
              Picked from your work, time, dates, blockers, and impact.
            </p>
          </div>
          {graph?.isAcyclic === false ? (
            <PlanTag tone="rose">Dependency cycle</PlanTag>
          ) : (
            <PlanTag tone="teal">Up to date</PlanTag>
          )}
        </div>
        <PlanningControls
          labels={labels}
          preferences={preferences}
          onPreferencesChange={onPreferencesChange}
        />
        {primaryItem ? (
          <div className="grid gap-2">
            <button
              type="button"
              className="group flex min-w-0 items-start gap-3 rounded-md border border-teal-500/20 bg-teal-500/[0.07] p-3 text-left transition-colors hover:bg-teal-500/10 dark:border-teal-300/20"
              onClick={() => onSelectCard(primaryItem.card.id)}
            >
              <span className="grid size-9 shrink-0 place-items-center rounded-md bg-teal-600 text-white shadow-xs dark:bg-teal-300 dark:text-teal-950">
                <Target className="size-4" weight="bold" />
              </span>
              <span className="min-w-0 flex-1">
                <span className="block truncate text-sm font-semibold">
                  {primaryItem.card.title}
                </span>
                <span className="mt-1 block text-[11px] leading-5 text-muted-foreground">
                  {isNoFitAlternative
                    ? getNoFitExplanation(
                        primaryItem,
                        preferences.availableMinutes
                      )
                    : getPlanExplanation(primaryItem)}
                </span>
                <span className="mt-2 flex flex-wrap gap-1.5">
                  {getPlanTags(primaryItem, preferences).map((tag) => (
                    <PlanTag key={tag.label} tone={tag.tone}>
                      {tag.label}
                    </PlanTag>
                  ))}
                </span>
              </span>
            </button>
            {queuedItems.length > 0 ? (
              <div className="grid gap-1.5 md:grid-cols-2">
                {queuedItems.map((item, index) => (
                  <PlanItemButton
                    key={item.card.id}
                    item={item}
                    onSelectCard={onSelectCard}
                    rank={index + 2}
                  />
                ))}
              </div>
            ) : null}
          </div>
        ) : (
          <div className="rounded-md border border-dashed border-zinc-950/10 bg-zinc-50 px-3 py-4 text-muted-foreground dark:border-white/10 dark:bg-zinc-950">
            {preferences.availableMinutes > 0 &&
            planItems.some((item) => item.isActionable)
              ? `Nothing ready fits ${formatMinutes(
                  preferences.availableMinutes
                )}. Add effort estimates to ready cards or pick a longer focus block.`
              : "No suggested next step yet. Add dates, priorities, or dependencies to help the planner rank work."}
          </div>
        )}
      </section>

      <section className="min-w-0 rounded-md border border-zinc-950/[0.06] bg-zinc-50 p-2.5 dark:border-white/10 dark:bg-zinc-950/40">
        <div className="mb-2 flex items-center justify-between gap-2">
          <p className="font-semibold">Board health</p>
          {wipAlerts.length > 0 ? (
            <PlanTag tone="amber">{wipAlerts.length} WIP alert</PlanTag>
          ) : (
            <PlanTag tone="teal">Flow stable</PlanTag>
          )}
        </div>
        <div className="mb-3">
          <div className="mb-1 flex items-center justify-between text-[11px] text-muted-foreground">
            <span>{metrics.completed} completed</span>
            <span>{metrics.active} active</span>
          </div>
          <div className="h-1.5 overflow-hidden rounded-full bg-zinc-950/10 dark:bg-white/10">
            <div
              className="h-full rounded-full bg-teal-600 dark:bg-teal-300"
              style={{ width: `${metrics.completionRate}%` }}
            />
          </div>
        </div>
        <div className="grid grid-cols-2 gap-1.5">
          {healthStats.map((stat) => (
            <HealthStat key={stat.label} {...stat} />
          ))}
        </div>
        {criticalPathStart && criticalPath.length > 1 ? (
          <button
            type="button"
            className="mt-2 flex w-full min-w-0 items-center gap-2 rounded-md border border-sky-500/15 bg-sky-500/[0.07] px-2.5 py-2 text-left transition-colors hover:bg-sky-500/10"
            onClick={() => onSelectCard(criticalPathStart.id)}
          >
            <GitBranch className="size-4 shrink-0 text-sky-700 dark:text-sky-200" />
            <span className="min-w-0 flex-1">
              <span className="block truncate font-medium">
                Main path starts here
              </span>
              <span className="block truncate text-[11px] text-muted-foreground">
                {criticalPathStart.title} · {criticalPath.length} cards
              </span>
            </span>
          </button>
        ) : null}
        {blockedPlanCount > 0 ? (
          <p className="mt-2 text-[11px] leading-5 text-muted-foreground">
            {blockedPlanCount} planned item{blockedPlanCount === 1 ? "" : "s"}{" "}
            waiting on another task.
          </p>
        ) : null}
      </section>
    </div>
  )
}

function PlanningControls({
  labels,
  preferences,
  onPreferencesChange,
}: {
  labels: string[]
  preferences: PlanningPreferences
  onPreferencesChange: (preferences: PlanningPreferences) => void
}) {
  const selectClass =
    "h-7 min-w-0 rounded-md border border-zinc-950/10 bg-white px-1.5 text-[11px] dark:border-white/10 dark:bg-zinc-950"

  return (
    <div className="mb-2 flex flex-wrap items-center gap-2 text-[11px] text-muted-foreground">
      <label className="flex items-center gap-1.5">
        Focus
        <select
          aria-label="Available focus time"
          className={selectClass}
          value={preferences.availableMinutes}
          onChange={(event) =>
            onPreferencesChange({
              ...preferences,
              availableMinutes: Number(event.target.value),
            })
          }
        >
          {focusTimeOptions.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </label>
      <label className="flex items-center gap-1.5">
        Context
        <select
          aria-label="Planning context"
          className={selectClass}
          value={preferences.context ?? ""}
          onChange={(event) =>
            onPreferencesChange({
              ...preferences,
              context: event.target.value || null,
            })
          }
        >
          <option value="">All labels</option>
          {labels.map((label) => (
            <option key={label} value={label}>
              #{label}
            </option>
          ))}
        </select>
      </label>
    </div>
  )
}

function PlanItemButton({
  item,
  onSelectCard,
  rank,
}: {
  item: BoardGraphPlanItem
  onSelectCard: (cardId: string) => void
  rank: number
}) {
  return (
    <button
      type="button"
      className="flex h-12 min-w-0 items-center gap-2 rounded-md border border-zinc-950/[0.08] bg-zinc-50 px-2.5 text-left transition-colors hover:bg-zinc-100 dark:border-white/10 dark:bg-zinc-950/70 dark:hover:bg-white/[0.06]"
      onClick={() => onSelectCard(item.card.id)}
    >
      <span className="grid size-6 shrink-0 place-items-center rounded-md bg-zinc-950/5 font-mono text-[11px] font-semibold text-muted-foreground dark:bg-white/10">
        {rank}
      </span>
      <span className="min-w-0 flex-1">
        <span className="block truncate font-medium">{item.card.title}</span>
        <span className="block truncate text-[11px] text-muted-foreground">
          {getShortPlanReason(item)}
        </span>
      </span>
    </button>
  )
}

function PlanTag({
  children,
  tone,
}: {
  children: React.ReactNode
  tone: PlanTone
}) {
  return (
    <span
      className={`inline-flex h-5 items-center rounded-sm px-1.5 text-[11px] font-medium ${getToneClass(
        tone
      )}`}
    >
      {children}
    </span>
  )
}

type HealthStatProps = {
  icon: Icon
  label: string
  tone: PlanTone
  value: string
}

function HealthStat({
  icon: IconComponent,
  label,
  tone,
  value,
}: HealthStatProps) {
  return (
    <div className="flex min-w-0 items-center gap-2 rounded-md border border-zinc-950/[0.06] bg-white px-2 py-2 dark:border-white/10 dark:bg-zinc-950">
      <span
        className={`grid size-6 shrink-0 place-items-center rounded-md ${getToneClass(
          tone
        )}`}
      >
        <IconComponent className="size-3.5" weight="bold" />
      </span>
      <span className="min-w-0">
        <span className="block text-sm leading-none font-semibold">
          {value}
        </span>
        <span className="mt-0.5 block truncate text-[11px] text-muted-foreground">
          {label}
        </span>
      </span>
    </div>
  )
}
