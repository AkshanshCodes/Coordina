import { daysUntilDateKey, todayDateKey } from "@/features/board/model/dates"
import { normalizeLabels } from "@/features/board/model/labels"
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
  filters?: BoardFilters,
  today = todayDateKey()
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
          cardMatchesSearch(card, query) &&
          cardMatchesFilters(card, filters, today)
      ),
    })),
  }
}

export function getBoardMetrics(
  board: Board | null,
  today = todayDateKey()
): BoardMetrics {
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
      const isOverdue = isCardOverdue(card, today)
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

      if (isCardDueSoon(card, today)) {
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

export function isCardDueSoon(card: BoardCard, today = todayDateKey()) {
  if (card.isCompleted || !card.dueDate || isCardOverdue(card, today)) {
    return false
  }

  return daysUntilDateKey(card.dueDate, today) <= 7
}

export function isCardOverdue(card: BoardCard, today = todayDateKey()) {
  return Boolean(!card.isCompleted && card.dueDate && card.dueDate < today)
}

export function isBlockedCard(card: BoardCard) {
  return card.labels.some((label) =>
    ["blocked", "blocker", "bloque"].includes(label.toLowerCase())
  )
}

export function isActiveWorkList(list: Pick<BoardList, "taskStatus">) {
  return list.taskStatus === "IN_PROGRESS"
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

function cardMatchesFilters(
  card: BoardCard,
  filters: BoardFilters | undefined,
  today: string
) {
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
      (isCardOverdue(card, today) ||
        isBlockedCard(card) ||
        card.priority === "HIGH")
    )
  }

  if (filters.focus === "due-soon") {
    return isCardDueSoon(card, today) || isCardOverdue(card, today)
  }

  if (filters.focus === "unassigned") {
    return card.assignees.length === 0
  }

  return true
}
