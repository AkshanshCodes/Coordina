import { useCallback, useEffect, useRef, useState } from "react"

import * as boardApi from "@/features/board/api/board-api"
import { listWorkspaceMembers } from "@/features/workspaces/workspace-api"
import { ApiError } from "@/lib/api"
import type {
  Board,
  BoardCardDependencyAnalysis,
  BoardGraph,
} from "@/types/task"
import type { WorkspaceMember } from "@/types/workspace"

type BoardScope = {
  accessToken: string | null
  workspaceId: string
  projectId: string
}

type UseBoardResourceOptions = BoardScope & {
  onError: (error: unknown, fallback: string) => void
}

export function useBoardResource({
  accessToken,
  workspaceId,
  projectId,
  onError,
}: UseBoardResourceOptions) {
  const [board, setBoard] = useState<Board | null>(null)
  const [members, setMembers] = useState<WorkspaceMember[]>([])
  const [boardGraph, setBoardGraph] = useState<BoardGraph | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const latestRequest = useRef(0)

  const reload = useCallback(async () => {
    const request = ++latestRequest.current
    const isCurrent = () => request === latestRequest.current

    if (!accessToken) {
      setBoard(null)
      return
    }

    setIsLoading(true)

    try {
      const [loadedBoard, loadedMembers] = await Promise.all([
        boardApi
          .getDefaultBoard(accessToken, workspaceId, projectId)
          .catch(nullWhenNotFound),
        listWorkspaceMembers(accessToken, workspaceId),
      ])

      if (!isCurrent()) {
        return
      }

      setBoard(loadedBoard)
      setMembers(loadedMembers)
      setBoardGraph(null)
    } catch (requestError) {
      if (!isCurrent()) {
        return
      }

      if (isNotFound(requestError)) {
        setBoard(null)
        return
      }

      onError(requestError, "Unable to load board.")
    } finally {
      if (isCurrent()) {
        setIsLoading(false)
      }
    }
  }, [accessToken, onError, projectId, workspaceId])

  useEffect(() => {
    queueMicrotask(() => {
      setBoard(null)
      setBoardGraph(null)
      void reload()
    })
  }, [reload])

  useEffect(() => {
    if (!accessToken || !board) {
      queueMicrotask(() => setBoardGraph(null))
      return
    }

    let ignore = false

    boardApi
      .getBoardGraph(accessToken, workspaceId, projectId, board.id)
      .then((graph) => {
        if (!ignore) {
          setBoardGraph(graph)
        }
      })
      .catch(() => {
        if (!ignore) {
          setBoardGraph(null)
        }
      })

    return () => {
      ignore = true
    }
  }, [accessToken, board, projectId, workspaceId])

  return { board, boardGraph, isLoading, members, reload, setBoard }
}

type UseCardDependencyAnalysisOptions = BoardScope & {
  board: Board | null
  cardId: string | null
}

export function useCardDependencyAnalysis({
  accessToken,
  workspaceId,
  projectId,
  board,
  cardId,
}: UseCardDependencyAnalysisOptions) {
  const [analysis, setAnalysis] = useState<BoardCardDependencyAnalysis | null>(
    null
  )
  const [isLoading, setIsLoading] = useState(false)

  useEffect(() => {
    if (!accessToken || !board || !cardId) {
      queueMicrotask(() => {
        setAnalysis(null)
        setIsLoading(false)
      })
      return
    }

    let ignore = false
    queueMicrotask(() => {
      if (!ignore) {
        setIsLoading(true)
      }
    })

    boardApi
      .getCardDependencyAnalysis(
        accessToken,
        workspaceId,
        projectId,
        board.id,
        cardId
      )
      .then((result) => {
        if (!ignore) {
          setAnalysis(result)
        }
      })
      .catch(() => {
        if (!ignore) {
          setAnalysis(null)
        }
      })
      .finally(() => {
        if (!ignore) {
          setIsLoading(false)
        }
      })

    return () => {
      ignore = true
    }
  }, [accessToken, board, cardId, projectId, workspaceId])

  return { analysis, isLoading }
}

export function isNotFound(error: unknown) {
  return error instanceof ApiError && error.status === 404
}

function nullWhenNotFound(error: unknown): null {
  if (isNotFound(error)) {
    return null
  }

  throw error
}
