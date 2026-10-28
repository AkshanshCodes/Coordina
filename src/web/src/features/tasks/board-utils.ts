import type {
  Board,
  BoardCard,
  BoardCardPriority,
  BoardList,
} from "@/types/task"

export type BoardFocus = "all" | "mine" | "risk" | "due-soon" | "unassigned"

export type BoardFilters = {
  assigneeId: string | "ALL"
  focus: BoardFocus
  label: string | "ALL"
  priority: BoardCardPriority | "ALL"
  sessionUserId?: string | null
}

export type BoardMetrics = {
  active: number
  blocked: number
  completed: number
  completionRate: number
  dueSoon: number
  highPriority: number
  overdue: number
  risk: number
  total: number
  unassigned: number
}

export type WipAlert = {
  count: number
  limit: number
  listId: string
  title: string
}

export function countBoardCards(board: Board | null) {
  return board?.lists.reduce((total, list) => total + list.cards.length, 0) ?? 0
}

export function getBoardLabels(board: Board | null) {
  return normalizeLabels(
    board?.lists.flatMap((list) => list.cards.flatMap((card) => card.labels)) ??
      []
  )
}

export function filterBoard(
  board: Board | null,
  search: string,
  filters?: BoardFilters
) {
  if (!board || !search.trim()) {
    if (!board || !filters || !hasActiveBoardFilters(filters)) {
      return board
    }
  }

  const query = search.trim().toLowerCase()

  return {
    ...board,
    lists: board.lists.map((list) => ({
      ...list,
      cards: list.cards.filter(
        (card) =>
          cardMatchesSearch(card, query) && cardMatchesFilters(card, filters)
      ),
    })),
  }
}

export function getBoardMetrics(board: Board | null): BoardMetrics {
  const metrics: BoardMetrics = {
    active: 0,
    blocked: 0,
    completed: 0,
    completionRate: 0,
    dueSoon: 0,
    highPriority: 0,
    overdue: 0,
    risk: 0,
    total: 0,
    unassigned: 0,
  }

  if (!board) {
    return metrics
  }

  for (const list of board.lists) {
    for (const card of list.cards) {
      const isBlocked = isBlockedCard(card)
      const isOverdue = isCardOverdue(card)
      const isCompleted = card.isCompleted

      metrics.total += 1

      if (isCompleted) {
        metrics.completed += 1
      } else {
        metrics.active += 1
      }

      if (card.priority === "HIGH") {
        metrics.highPriority += 1
      }

      if (isOverdue) {
        metrics.overdue += 1
      }

      if (isCardDueSoon(card)) {
        metrics.dueSoon += 1
      }

      if (card.assignees.length === 0) {
        metrics.unassigned += 1
      }

      if (isBlocked) {
        metrics.blocked += 1
      }

      if (
        !isCompleted &&
        (isOverdue || isBlocked || card.priority === "HIGH")
      ) {
        metrics.risk += 1
      }
    }
  }

  metrics.completionRate =
    metrics.total > 0
      ? Math.round((metrics.completed / metrics.total) * 100)
      : 0

  return metrics
}

export function getWipAlerts(board: Board | null, limit = 5): WipAlert[] {
  if (!board) {
    return []
  }

  return board.lists
    .filter(isActiveWorkList)
    .filter((list) => list.cards.length > limit)
    .map((list) => ({
      count: list.cards.length,
      limit,
      listId: list.id,
      title: list.title,
    }))
}

export function countUserCards(board: Board | null, userId?: string | null) {
  if (!board || !userId) {
    return 0
  }

  return board.lists.reduce(
    (total, list) =>
      total +
      list.cards.filter((card) =>
        card.assignees.some((assignee) => assignee.userId === userId)
      ).length,
    0
  )
}

export function findCard(board: Board, cardId: string) {
  return board.lists
    .flatMap((list) => list.cards)
    .find((card) => card.id === cardId)
}

export function moveCardInBoard(
  board: Board,
  cardId: string,
  listId: string
): Board {
  let movedCard: BoardCard | null = null

  const listsWithoutCard = board.lists.map((list) => {
    const cards = list.cards.filter((card) => {
      if (card.id === cardId) {
        movedCard = { ...card, listId }
        return false
      }

      return true
    })

    return { ...list, cards }
  })

  if (!movedCard) {
    return board
  }

  return {
    ...board,
    lists: listsWithoutCard.map((list) =>
      list.id === listId
        ? { ...list, cards: [...list.cards, movedCard as BoardCard] }
        : list
    ),
  }
}

export function removeCardFromBoard(board: Board, cardId: string): Board {
  return {
    ...board,
    lists: board.lists.map((list) => ({
      ...list,
      cards: list.cards.filter((card) => card.id !== cardId),
    })),
  }
}

export function renameListInBoard(
  board: Board,
  listId: string,
  title: string
): Board {
  return {
    ...board,
    lists: board.lists.map((list) =>
      list.id === listId ? { ...list, title } : list
    ),
  }
}

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

export function dateBadgeClass(value: string) {
  if (value < todayDateKey()) {
    return "bg-rose-500/10 text-rose-700 dark:text-rose-200"
  }

  if (value === todayDateKey()) {
    return "bg-amber-500/10 text-amber-700 dark:text-amber-200"
  }

  return "bg-teal-500/10 text-teal-700 dark:text-teal-200"
}

export function isCardDueSoon(card: BoardCard) {
  if (card.isCompleted || !card.dueDate || isCardOverdue(card)) {
    return false
  }

  return daysUntilDateKey(card.dueDate) <= 7
}

export function isCardOverdue(card: BoardCard) {
  return Boolean(
    !card.isCompleted && card.dueDate && card.dueDate < todayDateKey()
  )
}

export function isBlockedCard(card: BoardCard) {
  return card.labels.some((label) =>
    ["blocked", "blocker", "bloque"].includes(label.toLowerCase())
  )
}

export function isActiveWorkList(list: Pick<BoardList, "taskStatus">) {
  return list.taskStatus === "IN_PROGRESS"
}

export function normalizeLabels(labels: string[]) {
  return labels
    .map(normalizeLabel)
    .filter((label): label is string => Boolean(label))
    .filter(
      (label, index, allLabels) =>
        allLabels.findIndex(
          (candidate) => candidate.toLowerCase() === label.toLowerCase()
        ) === index
    )
}

export function normalizeLabel(label: string) {
  const normalized = label.trim().replace(/^#/, "").replace(/\s+/g, "-")
  return normalized ? normalized.slice(0, 32) : null
}

export function sameLabels(left: string[], right: string[]) {
  return sameStrings(
    left.map((label) => label.toLowerCase()).sort(),
    right.map((label) => label.toLowerCase()).sort()
  )
}

export function sameStrings(left: string[], right: string[]) {
  if (left.length !== right.length) {
    return false
  }

  return left.every((value, index) => value === right[index])
}

export function todayDateKey() {
  const date = new Date()
  const year = date.getFullYear()
  const month = `${date.getMonth() + 1}`.padStart(2, "0")
  const day = `${date.getDate()}`.padStart(2, "0")

  return `${year}-${month}-${day}`
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

function labelTone(label: string) {
  return (
    [...label].reduce((sum, character) => sum + character.charCodeAt(0), 0) % 4
  )
}

function hasActiveBoardFilters(filters: BoardFilters) {
  return (
    filters.focus !== "all" ||
    filters.priority !== "ALL" ||
    filters.label !== "ALL" ||
    filters.assigneeId !== "ALL"
  )
}

function cardMatchesSearch(card: BoardCard, query: string) {
  if (!query) {
    return true
  }

  return [
    card.title,
    card.description ?? "",
    card.priority ?? "",
    card.labels.join(" "),
    card.assignees
      .map((assignee) => `${assignee.name ?? ""} ${assignee.email ?? ""}`)
      .join(" "),
  ]
    .join(" ")
    .toLowerCase()
    .includes(query)
}

function cardMatchesFilters(card: BoardCard, filters?: BoardFilters) {
  if (!filters) {
    return true
  }

  if (filters.priority !== "ALL" && card.priority !== filters.priority) {
    return false
  }

  if (
    filters.label !== "ALL" &&
    !card.labels.some(
      (label) => label.toLowerCase() === filters.label.toLowerCase()
    )
  ) {
    return false
  }

  if (
    filters.assigneeId !== "ALL" &&
    !card.assignees.some((assignee) => assignee.userId === filters.assigneeId)
  ) {
    return false
  }

  if (filters.focus === "mine") {
    return Boolean(
      filters.sessionUserId &&
      card.assignees.some(
        (assignee) => assignee.userId === filters.sessionUserId
      )
    )
  }

  if (filters.focus === "risk") {
    return (
      !card.isCompleted &&
      (isCardOverdue(card) || isBlockedCard(card) || card.priority === "HIGH")
    )
  }

  if (filters.focus === "due-soon") {
    return isCardDueSoon(card) || isCardOverdue(card)
  }

  if (filters.focus === "unassigned") {
    return card.assignees.length === 0
  }

  return true
}

function dateFromKey(value: string) {
  const [year, month, day] = value.split("-").map(Number)

  return new Date(year, (month ?? 1) - 1, day ?? 1)
}

function daysUntilDateKey(value: string) {
  const today = dateFromKey(todayDateKey()).getTime()
  const target = dateFromKey(value).getTime()

  return Math.ceil((target - today) / 86_400_000)
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
