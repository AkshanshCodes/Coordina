import { describe, expect, it } from "vitest"

import {
  filterBoard,
  getBoardMetrics,
  getWipAlerts,
  isCardDueSoon,
  isCardOverdue,
  type BoardFilters,
} from "@/features/board/model/board-selectors"
import {
  makeBoard,
  makeCard,
  makeList,
  manyCards,
} from "@/features/board/model/board-test-fixtures"

const today = "2026-10-28"
const noFilters: BoardFilters = {
  assigneeId: "ALL",
  focus: "all",
  label: "ALL",
  priority: "ALL",
  sessionUserId: "me",
}

describe("getBoardMetrics", () => {
  it("counts completion from cards, not list titles", () => {
    const metrics = getBoardMetrics(
      makeBoard([
        makeList("Done", "TODO", [makeCard({ id: "misleading-title" })]),
        makeList("Shipped", "DONE", [
          makeCard({ id: "finished", isCompleted: true }),
        ]),
      ]),
      today
    )

    expect(metrics.completed).toBe(1)
    expect(metrics.active).toBe(1)
    expect(metrics.completionRate).toBe(50)
  })

  it("is unchanged when lists are renamed", () => {
    const original = makeBoard([
      makeList("To Do", "TODO", [makeCard({ id: "a" })]),
      makeList("Done", "DONE", [makeCard({ id: "b", isCompleted: true })]),
    ])
    const renamed = makeBoard(
      original.lists.map((list, index) => ({
        ...list,
        title: `Column ${index}`,
      }))
    )

    expect(getBoardMetrics(renamed, today)).toEqual(
      getBoardMetrics(original, today)
    )
  })

  it("flags open overdue, blocked, or high-priority cards as risk", () => {
    const metrics = getBoardMetrics(
      makeBoard([
        makeList("Doing", "IN_PROGRESS", [
          makeCard({ id: "late", dueDate: "2026-10-27" }),
          makeCard({ id: "blocked", labels: ["Blocked"] }),
          makeCard({ id: "urgent", priority: "HIGH" }),
          makeCard({ id: "calm" }),
        ]),
        makeList("Done", "DONE", [
          makeCard({ id: "done-urgent", priority: "HIGH", isCompleted: true }),
        ]),
      ]),
      today
    )

    expect(metrics.risk).toBe(3)
    expect(metrics.overdue).toBe(1)
    expect(metrics.blocked).toBe(1)
    expect(metrics.highPriority).toBe(2)
  })
})

describe("getWipAlerts", () => {
  it("only applies to in-progress lists", () => {
    const alerts = getWipAlerts(
      makeBoard([
        makeList("Backlog", "TODO", manyCards("todo", 8)),
        makeList("Doing", "IN_PROGRESS", manyCards("doing", 6)),
        makeList("Done", "DONE", manyCards("done", 9)),
      ])
    )

    expect(alerts.map((alert) => alert.title)).toEqual(["Doing"])
  })

  it("follows status after a rename", () => {
    const alerts = getWipAlerts(
      makeBoard([makeList("Done", "IN_PROGRESS", manyCards("doing", 6))])
    )

    expect(alerts).toHaveLength(1)
  })
})

describe("card dates", () => {
  it("uses the injected day for overdue and due soon", () => {
    const card = makeCard({ dueDate: "2026-11-02" })

    expect(isCardOverdue(card, today)).toBe(false)
    expect(isCardDueSoon(card, today)).toBe(true)
    expect(isCardOverdue(card, "2026-11-03")).toBe(true)
    expect(isCardDueSoon(card, "2026-10-01")).toBe(false)
  })

  it("never marks completed cards as late", () => {
    const card = makeCard({ dueDate: "2026-10-01", isCompleted: true })

    expect(isCardOverdue(card, today)).toBe(false)
    expect(isCardDueSoon(card, today)).toBe(false)
  })
})

describe("filterBoard", () => {
  const board = makeBoard([
    makeList("Doing", "IN_PROGRESS", [
      makeCard({
        id: "mine",
        title: "API contract",
        labels: ["backend"],
        assignees: [{ userId: "me", name: "Me", email: "me@coordina.test" }],
      }),
      makeCard({ id: "theirs", title: "Landing page", labels: ["ui"] }),
    ]),
  ])
  const cardIds = (filtered: ReturnType<typeof filterBoard>) =>
    filtered?.lists.flatMap((list) => list.cards.map((card) => card.id))

  it("returns the same board when nothing filters", () => {
    expect(filterBoard(board, " ", noFilters, today)).toBe(board)
  })

  it("matches search across title and labels", () => {
    expect(cardIds(filterBoard(board, "backend", noFilters, today))).toEqual([
      "mine",
    ])
    expect(cardIds(filterBoard(board, "landing", noFilters, today))).toEqual([
      "theirs",
    ])
  })

  it("applies focus, label, and assignee filters", () => {
    expect(
      cardIds(filterBoard(board, "", { ...noFilters, focus: "mine" }, today))
    ).toEqual(["mine"])
    expect(
      cardIds(filterBoard(board, "", { ...noFilters, label: "UI" }, today))
    ).toEqual(["theirs"])
    expect(
      cardIds(
        filterBoard(board, "", { ...noFilters, focus: "unassigned" }, today)
      )
    ).toEqual(["theirs"])
  })
})
