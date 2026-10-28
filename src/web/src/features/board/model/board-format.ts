import { dateFromKey, todayDateKey } from "@/features/board/model/dates"
import type { BoardCardPriority } from "@/types/task"

export function priorityDot(priority: BoardCardPriority | null) {
  return priority === "HIGH"
    ? "bg-rose-500"
    : priority === "MEDIUM"
      ? "bg-amber-500"
      : priority === "LOW"
        ? "bg-teal-500"
        : "bg-zinc-300 dark:bg-zinc-600"
}

export function priorityBar(priority: BoardCardPriority | null) {
  return priority === "HIGH"
    ? "bg-rose-500"
    : priority === "MEDIUM"
      ? "bg-amber-500"
      : priority === "LOW"
        ? "bg-teal-500"
        : "bg-zinc-300 dark:bg-zinc-700"
}

export function labelClass(label: string) {
  return [
    "bg-sky-500/10 text-sky-800 dark:bg-sky-400/15 dark:text-sky-100",
    "bg-violet-500/10 text-violet-800 dark:bg-violet-400/15 dark:text-violet-100",
    "bg-teal-500/10 text-teal-800 dark:bg-teal-400/15 dark:text-teal-100",
    "bg-fuchsia-500/10 text-fuchsia-800 dark:bg-fuchsia-400/15 dark:text-fuchsia-100",
  ][labelTone(label)]
}

export function panelLabelClass(label: string) {
  return [
    "bg-sky-100 text-sky-900 dark:bg-sky-300/20 dark:text-sky-100",
    "bg-violet-100 text-violet-900 dark:bg-violet-300/20 dark:text-violet-100",
    "bg-teal-100 text-teal-900 dark:bg-teal-300/20 dark:text-teal-100",
    "bg-fuchsia-100 text-fuchsia-900 dark:bg-fuchsia-300/20 dark:text-fuchsia-100",
  ][labelTone(label)]
}

export function dateBadgeClass(value: string, today = todayDateKey()) {
  if (value < today) {
    return "bg-rose-500/10 text-rose-700 dark:text-rose-200"
  }

  if (value === today) {
    return "bg-amber-500/10 text-amber-700 dark:text-amber-200"
  }

  return "bg-teal-500/10 text-teal-700 dark:text-teal-200"
}

export function initials(value?: string | null) {
  if (!value) {
    return "?"
  }

  return value
    .split(/\s|@/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join("")
}

export function shortDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    month: "short",
    day: "numeric",
  }).format(dateFromKey(value))
}

export function userLabel(user: { name: string | null; email: string | null }) {
  return user.name ?? user.email ?? "Workspace member"
}

export function formatDateTime(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value))
}

function labelTone(label: string) {
  return (
    [...label].reduce((sum, character) => sum + character.charCodeAt(0), 0) % 4
  )
}
