import { describe, expect, it } from "vitest"

import {
  makeBoard,
  makeCard,
  makeList,
} from "@/features/board/model/board-test-fixtures"
import {
  moveCardInBoard,
  removeCardFromBoard,
  renameListInBoard,
} from "@/features/board/model/board-updates"

const board = makeBoard([
  makeList("Todo", "TODO", [makeCard({ id: "a" }), makeCard({ id: "b" })]),
  makeList("Doing", "IN_PROGRESS", [makeCard({ id: "c" })]),
  makeList("Done", "DONE"),
])

describe("moveCardInBoard", () => {
  it("appends the card to the target list without mutating input", () => {
    const snapshot = structuredClone(board)

    const moved = moveCardInBoard(board, "a", "Doing")

    expect(moved.lists[0].cards.map((card) => card.id)).toEqual(["b"])
    expect(moved.lists[1].cards.map((card) => card.id)).toEqual(["c", "a"])
    expect(moved.lists[1].cards[1].listId).toBe("Doing")
    expect(board).toEqual(snapshot)
  })

  it("returns the same board for an unknown card", () => {
    expect(moveCardInBoard(board, "missing", "Done")).toBe(board)
  })
})

describe("removeCardFromBoard", () => {
  it("drops the card from every list", () => {
    const updated = removeCardFromBoard(board, "c")

    expect(updated.lists[1].cards).toEqual([])
    expect(board.lists[1].cards).toHaveLength(1)
  })
})

describe("renameListInBoard", () => {
  it("renames one list and keeps the others by reference", () => {
    const renamed = renameListInBoard(board, "Doing", "Review")

    expect(renamed.lists[1].title).toBe("Review")
    expect(renamed.lists[1].taskStatus).toBe("IN_PROGRESS")
    expect(renamed.lists[0]).toBe(board.lists[0])
    expect(renamed.lists[2]).toBe(board.lists[2])
  })
})
