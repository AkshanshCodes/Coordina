import { act, renderHook } from "@testing-library/react"
import { useState } from "react"
import { afterEach, describe, expect, it, vi } from "vitest"

import * as boardApi from "@/features/board/api/board-api"
import { useBoardMutations } from "@/features/board/hooks/use-board-mutations"
import {
  makeBoard,
  makeCard,
  makeList,
} from "@/features/board/model/board-test-fixtures"
import { ApiError } from "@/lib/api"
import type { Board } from "@/types/task"

vi.mock("@/features/board/api/board-api")

const initialBoard = makeBoard([
  makeList("Todo", "TODO", [makeCard({ id: "a" })]),
  makeList("Doing", "IN_PROGRESS"),
])

function renderMutations() {
  const onError = vi.fn()
  const reload = vi.fn().mockResolvedValue(undefined)
  const hook = renderHook(() => {
    const [board, setBoard] = useState<Board | null>(initialBoard)
    const mutations = useBoardMutations({
      accessToken: "token",
      workspaceId: "workspace",
      projectId: "project",
      board,
      setBoard,
      reload,
      isReadOnly: false,
      onError,
      clearError: vi.fn(),
    })

    return { board, setBoard, mutations }
  })

  return { ...hook, onError, reload }
}

describe("useBoardMutations", () => {
  afterEach(() => {
    vi.resetAllMocks()
  })

  it("restores the previous board when a move fails", async () => {
    vi.mocked(boardApi.moveCard).mockRejectedValue(
      new ApiError("Server unavailable", 503)
    )
    const { result, onError } = renderMutations()

    await act(async () => {
      await result.current.mutations.moveCard("a", "Doing")
    })

    expect(result.current.board).toBe(initialBoard)
    expect(onError).toHaveBeenCalledWith(
      expect.any(ApiError),
      "Card could not be moved."
    )
  })

  it("keeps a newer board when an older move fails", async () => {
    let rejectMove!: (error: unknown) => void
    vi.mocked(boardApi.moveCard).mockReturnValue(
      new Promise((_resolve, reject) => {
        rejectMove = reject
      })
    )
    const newerBoard = makeBoard([makeList("Fresh", "TODO")])
    const { result } = renderMutations()

    let pendingMove!: Promise<boolean>
    act(() => {
      pendingMove = result.current.mutations.moveCard("a", "Doing")
    })
    act(() => {
      result.current.setBoard(newerBoard)
    })
    await act(async () => {
      rejectMove(new ApiError("Conflict", 409))
      await pendingMove
    })

    expect(result.current.board).toBe(newerBoard)
  })

  it("reloads instead of clearing the board when a card is gone", async () => {
    vi.mocked(boardApi.updateCard).mockRejectedValue(
      new ApiError("Not found", 404)
    )
    const { result, onError, reload } = renderMutations()

    await act(async () => {
      await result.current.mutations
        .saveCard(makeCard({ id: "a" }), { title: "Edited" })
        .catch(() => undefined)
    })

    expect(reload).toHaveBeenCalledTimes(1)
    expect(onError).not.toHaveBeenCalled()
    expect(result.current.board).toBe(initialBoard)
  })
})
