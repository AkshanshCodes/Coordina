import { act, renderHook, waitFor } from "@testing-library/react"
import { afterEach, describe, expect, it, vi } from "vitest"

import * as boardApi from "@/features/board/api/board-api"
import { useBoardResource } from "@/features/board/hooks/use-board-resource"
import { makeBoard } from "@/features/board/model/board-test-fixtures"
import * as workspaceApi from "@/features/workspaces/workspace-api"
import type { Board } from "@/types/task"

vi.mock("@/features/board/api/board-api")
vi.mock("@/features/workspaces/workspace-api")

function deferred<T>() {
  let resolve!: (value: T) => void
  const promise = new Promise<T>((done) => {
    resolve = done
  })

  return { promise, resolve }
}

describe("useBoardResource", () => {
  afterEach(() => {
    vi.resetAllMocks()
  })

  it("ignores a slower response for the previous project", async () => {
    const boardA = { ...makeBoard([]), id: "board-a", projectId: "a" }
    const boardB = { ...makeBoard([]), id: "board-b", projectId: "b" }
    const slowA = deferred<Board>()
    vi.mocked(boardApi.getDefaultBoard).mockImplementation(
      (_token, _workspaceId, projectId) =>
        projectId === "a" ? slowA.promise : Promise.resolve(boardB)
    )
    vi.mocked(boardApi.getBoardGraph).mockReturnValue(new Promise(() => {}))
    vi.mocked(workspaceApi.listWorkspaceMembers).mockResolvedValue([])
    const onError = vi.fn()

    const { result, rerender } = renderHook(
      ({ projectId }) =>
        useBoardResource({
          accessToken: "token",
          workspaceId: "workspace",
          projectId,
          onError,
        }),
      { initialProps: { projectId: "a" } }
    )
    await waitFor(() =>
      expect(boardApi.getDefaultBoard).toHaveBeenCalledTimes(1)
    )

    rerender({ projectId: "b" })
    await waitFor(() => expect(result.current.board?.id).toBe("board-b"))

    await act(async () => {
      slowA.resolve(boardA)
      await slowA.promise
    })

    expect(result.current.board?.id).toBe("board-b")
    expect(onError).not.toHaveBeenCalled()
  })

  it("reloads only the graph when planning preferences change", async () => {
    const board = makeBoard([])
    vi.mocked(boardApi.getDefaultBoard).mockResolvedValue(board)
    vi.mocked(boardApi.getBoardGraph).mockReturnValue(new Promise(() => {}))
    vi.mocked(workspaceApi.listWorkspaceMembers).mockResolvedValue([])

    const { rerender } = renderHook(
      ({ availableMinutes }) =>
        useBoardResource({
          accessToken: "token",
          workspaceId: "workspace",
          projectId: "project",
          onError: vi.fn(),
          planning: { availableMinutes, context: null },
        }),
      { initialProps: { availableMinutes: 0 } }
    )
    await waitFor(() => expect(boardApi.getBoardGraph).toHaveBeenCalledTimes(1))

    rerender({ availableMinutes: 60 })
    await waitFor(() => expect(boardApi.getBoardGraph).toHaveBeenCalledTimes(2))

    expect(boardApi.getDefaultBoard).toHaveBeenCalledTimes(1)
    expect(vi.mocked(boardApi.getBoardGraph).mock.calls[1][4]).toEqual({
      availableMinutes: 60,
      context: null,
    })
  })
})
