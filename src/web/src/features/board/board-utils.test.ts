import { describe, expect, it } from "vitest"

import { getBoardMetrics, getWipAlerts } from "@/features/board/board-utils"
import type { Board, BoardCard, BoardList, BoardTaskStatus } from "@/types/task"

function card(id: string, isCompleted = false): BoardCard {
  return {
    id,
    boardId: "board",
    listId: "list",
    title: id,
    description: null,
    priority: null,
    dueDate: null,
    labels: [],
    isCompleted,
    completedAt: isCompleted ? "2026-10-28T10:00:00Z" : null,
    completedBy: null,
    position: 0,
    createdAt: "2026-10-28T10:00:00Z",
    updatedAt: "2026-10-28T10:00:00Z",
    assignees: [],
    comments: [],
    subtasks: [],
    dependencies: [],
  }
}

function list(
  title: string,
  taskStatus: BoardTaskStatus,
  cards: BoardCard[]
): BoardList {
  return {
    id: title,
    boardId: "board",
    title,
    position: 0,
    taskStatus,
    createdAt: "2026-10-28T10:00:00Z",
    updatedAt: "2026-10-28T10:00:00Z",
    cards,
  }
}

function board(lists: BoardList[]): Board {
  return {
    id: "board",
    projectId: "project",
    name: "Board",
    template: "CUSTOM",
    createdAt: "2026-10-28T10:00:00Z",
    updatedAt: "2026-10-28T10:00:00Z",
    lists,
  }
}

function manyCards(prefix: string, count: number) {
  return Array.from({ length: count }, (_, index) => card(`${prefix}-${index}`))
}

describe("getBoardMetrics", () => {
  it("counts completion from cards, not list titles", () => {
    const metrics = getBoardMetrics(
      board([
        list("Done", "TODO", [card("misleading-title")]),
        list("Shipped", "DONE", [card("finished", true)]),
      ])
    )

    expect(metrics.completed).toBe(1)
    expect(metrics.active).toBe(1)
  })

  it("is unchanged when lists are renamed", () => {
    const original = board([
      list("To Do", "TODO", [card("a")]),
      list("Done", "DONE", [card("b", true)]),
    ])
    const renamed = board(
      original.lists.map((item, index) => ({
        ...item,
        title: `Column ${index}`,
      }))
    )

    expect(getBoardMetrics(renamed)).toEqual(getBoardMetrics(original))
  })
})

describe("getWipAlerts", () => {
  it("only applies to in-progress lists", () => {
    const alerts = getWipAlerts(
      board([
        list("Backlog", "TODO", manyCards("todo", 8)),
        list("Doing", "IN_PROGRESS", manyCards("doing", 6)),
        list("Done", "DONE", manyCards("done", 9)),
      ])
    )

    expect(alerts.map((alert) => alert.title)).toEqual(["Doing"])
  })

  it("follows status after a rename", () => {
    const alerts = getWipAlerts(
      board([list("Done", "IN_PROGRESS", manyCards("doing", 6))])
    )

    expect(alerts).toHaveLength(1)
  })
})
