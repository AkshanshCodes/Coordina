import {
  formatMinutes,
  type PlanningPreferences,
} from "@/features/board/model/planning-preferences"
import type { BoardGraphPlanItem } from "@/types/task"

export type PlanTone = "amber" | "rose" | "sky" | "teal" | "zinc"

export function getPlanExplanation(item: BoardGraphPlanItem) {
  const unlockReason = getUnlockReason(item)
  const dueReason = getDueReason(item)
  const isCritical = isCriticalPlanItem(item)

  if (dueReason?.startsWith("Overdue")) {
    return "Do this first: it is overdue and ready to move without waiting on another task."
  }

  if (isCritical && unlockReason) {
    return `Do this first: it keeps the main path moving and ${formatUnlockReason(
      unlockReason
    )}.`
  }

  if (unlockReason) {
    return `Do this first: it is ready and ${formatUnlockReason(unlockReason)}.`
  }

  if (isCritical) {
    return "Do this first: it is the cleanest ready task on the main path."
  }

  if (dueReason) {
    return `Do this first: it is ready and ${dueReason.toLowerCase()}.`
  }

  return "Do this first: it is ready and has the strongest impact right now."
}

export type PlanFocus = {
  primary: BoardGraphPlanItem | null
  queued: BoardGraphPlanItem[]
  isNoFitAlternative: boolean
}

export function pickPlanFocus(
  items: BoardGraphPlanItem[],
  availableMinutes: number
): PlanFocus {
  const actionable = items.filter((item) => item.isActionable)
  const candidates =
    availableMinutes > 0
      ? actionable.filter((item) => item.fitsAvailableTime)
      : actionable

  if (candidates.length > 0) {
    return {
      primary: candidates[0],
      queued: candidates.slice(1, 3),
      isNoFitAlternative: false,
    }
  }

  const alternative =
    actionable.find((item) => item.isShortestAlternative) ?? null

  return {
    primary: alternative,
    queued: [],
    isNoFitAlternative: alternative !== null,
  }
}

export function getNoFitExplanation(
  item: BoardGraphPlanItem,
  availableMinutes: number
) {
  const effort =
    item.estimatedMinutes === null
      ? ""
      : ` (${formatMinutes(item.estimatedMinutes)})`

  return `Nothing ready fits ${formatMinutes(
    availableMinutes
  )}. This is the shortest ready task${effort}.`
}

export function getPlanTags(
  item: BoardGraphPlanItem,
  preferences: PlanningPreferences
) {
  const tags: { label: string; tone: PlanTone }[] = [
    { label: "Ready to start", tone: "teal" },
  ]
  const dueReason = getDueReason(item)
  const unlockReason = getUnlockReason(item)

  if (preferences.availableMinutes > 0) {
    if (item.estimatedMinutes === null) {
      tags.push({ label: "Effort unknown", tone: "zinc" })
    } else if (item.fitsAvailableTime) {
      tags.push({
        label: `Fits ${formatMinutes(preferences.availableMinutes)}`,
        tone: "teal",
      })
    }
  }

  if (item.isAssignedToMe) {
    tags.push({ label: "Assigned to you", tone: "sky" })
  }

  if (item.isInProgress) {
    tags.push({ label: "In progress", tone: "amber" })
  }

  if (item.isContextMatch && preferences.context) {
    tags.push({ label: `#${preferences.context}`, tone: "sky" })
  }

  if (dueReason) {
    tags.push({
      label: dueReason,
      tone: dueReason.startsWith("Overdue") ? "rose" : "amber",
    })
  }

  if (isCriticalPlanItem(item)) {
    tags.push({ label: "Main path", tone: "sky" })
  }

  if (unlockReason) {
    tags.push({ label: toUnlockTag(unlockReason), tone: "teal" })
  }

  if (item.reasons.includes("High priority")) {
    tags.push({ label: "High priority", tone: "rose" })
  }

  return tags.slice(0, 5)
}

export function getShortPlanReason(item: BoardGraphPlanItem) {
  const unlockReason = getUnlockReason(item)
  const dueReason = getDueReason(item)

  if (unlockReason) {
    return toUnlockTag(unlockReason)
  }

  if (isCriticalPlanItem(item)) {
    return "Keeps the main path moving"
  }

  if (dueReason) {
    return dueReason
  }

  if (item.reasons.includes("High priority")) {
    return "High priority"
  }

  return "Ready to start"
}

function getUnlockReason(item: BoardGraphPlanItem) {
  return item.reasons.find((reason) => reason.startsWith("Unlocks"))
}

function getDueReason(item: BoardGraphPlanItem) {
  return item.reasons.find(
    (reason) => reason.startsWith("Due") || reason.startsWith("Overdue")
  )
}

function isCriticalPlanItem(item: BoardGraphPlanItem) {
  return item.card.isCriticalPath || item.reasons.includes("Critical path")
}

function toUnlockTag(reason: string) {
  const match = reason.match(/^Unlocks (\d+)/)

  if (!match) {
    return reason
  }

  const count = Number(match[1])

  return `Unblocks ${count} task${count === 1 ? "" : "s"}`
}

function formatUnlockReason(reason: string) {
  const tag = toUnlockTag(reason)

  return tag.charAt(0).toLowerCase() + tag.slice(1)
}

export function getToneClass(tone: PlanTone) {
  if (tone === "rose") {
    return "bg-rose-500/10 text-rose-700 dark:text-rose-200"
  }

  if (tone === "amber") {
    return "bg-amber-500/10 text-amber-700 dark:text-amber-200"
  }

  if (tone === "teal") {
    return "bg-teal-500/10 text-teal-700 dark:text-teal-200"
  }

  if (tone === "sky") {
    return "bg-sky-500/10 text-sky-700 dark:text-sky-200"
  }

  return "bg-zinc-950/5 text-muted-foreground dark:bg-white/10"
}
