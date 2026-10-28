import { useCallback, useEffect, useMemo, useState } from "react"

import { ApiError } from "@/lib/api"
import { useAuth } from "@/features/auth/auth-context"
import {
  countBoardCards,
  countUserCards,
  findCard,
  getBoardLabels,
  getBoardMetrics,
  getWipAlerts,
} from "@/features/board/model/board-selectors"
import {
  moveCardInBoard,
  removeCardFromBoard,
  renameListInBoard,
} from "@/features/board/model/board-updates"
import {
  addCardComment,
  addCardDependency,
  createBoard,
  createCard,
  createList,
  deleteCard,
  deleteCardDependency,
  deleteSubtask,
  createSubtask,
  getBoardGraph,
  getCardDependencyAnalysis,
  getDefaultBoard,
  moveCard,
  updateCard,
  updateList,
  updateSubtask,
} from "@/features/board/api/board-api"
import { useBoardFilters } from "@/features/board/hooks/use-board-filters"
import type { Project } from "@/types/project"
import type {
  Board,
  BoardCard,
  BoardCardDependencyAnalysis,
  BoardCardInput,
  BoardCardSubtask,
  BoardGraph,
  BoardList,
  BoardTemplate,
} from "@/types/task"
import { listWorkspaceMembers } from "@/features/workspaces/workspace-api"
import type { Workspace, WorkspaceMember } from "@/types/workspace"

export function useProjectBoard(project: Project, workspace: Workspace) {
  const { session, signOut } = useAuth()
  const [board, setBoard] = useState<Board | null>(null)
  const [boardGraph, setBoardGraph] = useState<BoardGraph | null>(null)
  const [dependencyAnalysis, setDependencyAnalysis] =
    useState<BoardCardDependencyAnalysis | null>(null)
  const [members, setMembers] = useState<WorkspaceMember[]>([])
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [isCreatingBoard, setIsCreatingBoard] = useState(false)
  const [isMutating, setIsMutating] = useState(false)
  const [quickAddListId, setQuickAddListId] = useState<string | null>(null)
  const [quickTitle, setQuickTitle] = useState("")
  const [newListTitle, setNewListTitle] = useState("")
  const [isAddingList, setIsAddingList] = useState(false)
  const [draggingCardId, setDraggingCardId] = useState<string | null>(null)
  const [selectedCardId, setSelectedCardId] = useState<string | null>(null)
  const [isLoadingDependencyAnalysis, setIsLoadingDependencyAnalysis] =
    useState(false)
  const [notice, setNotice] = useState<string | null>(null)
  const [isPlanningPanelOpen, setIsPlanningPanelOpen] = useState(false)

  const isReadOnly = project.status === "COMPLETED"
  const sessionUserId = session?.user.id ?? null
  const filters = useBoardFilters(board, sessionUserId)
  const { clearFilters } = filters

  const selectedCard = useMemo(
    () =>
      board?.lists
        .flatMap((list) => list.cards)
        .find((card) => card.id === selectedCardId) ?? null,
    [board, selectedCardId]
  )
  const dependencyCandidates = useMemo(
    () =>
      board?.lists
        .flatMap((list) => list.cards)
        .filter((card) => card.id !== selectedCardId) ?? [],
    [board, selectedCardId]
  )
  const boardLabels = useMemo(() => getBoardLabels(board), [board])
  const metrics = useMemo(() => getBoardMetrics(board), [board])
  const wipAlerts = useMemo(() => getWipAlerts(board), [board])
  const cardCount = useMemo(() => countBoardCards(board), [board])
  const myCardCount = useMemo(
    () => countUserCards(board, sessionUserId),
    [board, sessionUserId]
  )
  const canDeleteCards =
    !isReadOnly &&
    (workspace.role === "OWNER" || project.projectOwnerId === session?.user.id)
  const canShowPlanningPanel = cardCount > 0
  const isPlanningPanelVisible = canShowPlanningPanel && isPlanningPanelOpen

  const handleRequestError = useCallback(
    (requestError: unknown, fallback: string) => {
      if (requestError instanceof ApiError) {
        if (requestError.status === 404) {
          setError(null)
          setBoard(null)
          return
        }

        setError(requestError.message)

        if (requestError.status === 401) {
          signOut()
        }
        return
      }

      setError(fallback)
    },
    [signOut]
  )

  const loadBoard = useCallback(async () => {
    if (!session) {
      setBoard(null)
      return
    }

    setIsLoading(true)
    setError(null)

    try {
      const [loadedBoard, loadedMembers] = await Promise.all([
        getDefaultBoard(session.accessToken, workspace.id, project.id).catch(
          (requestError) => {
            if (
              requestError instanceof ApiError &&
              requestError.status === 404
            ) {
              return null
            }

            throw requestError
          }
        ),
        listWorkspaceMembers(session.accessToken, workspace.id),
      ])
      setBoard(loadedBoard)
      setMembers(loadedMembers)
      setBoardGraph(null)
    } catch (requestError) {
      handleRequestError(requestError, "Unable to load board.")
    } finally {
      setIsLoading(false)
    }
  }, [handleRequestError, project.id, session, workspace.id])

  useEffect(() => {
    queueMicrotask(() => {
      setBoard(null)
      setBoardGraph(null)
      setDependencyAnalysis(null)
      setSelectedCardId(null)
      clearFilters()
      void loadBoard()
    })
  }, [clearFilters, loadBoard])

  useEffect(() => {
    if (!session || !board) {
      queueMicrotask(() => setBoardGraph(null))
      return
    }

    let ignore = false
    const accessToken = session.accessToken
    const boardId = board.id

    async function loadGraph() {
      try {
        const graph = await getBoardGraph(
          accessToken,
          workspace.id,
          project.id,
          boardId
        )

        if (!ignore) {
          setBoardGraph(graph)
        }
      } catch {
        if (!ignore) {
          setBoardGraph(null)
        }
      }
    }

    void loadGraph()

    return () => {
      ignore = true
    }
  }, [board, project.id, session, workspace.id])

  useEffect(() => {
    if (!session || !board || !selectedCardId) {
      queueMicrotask(() => {
        setDependencyAnalysis(null)
        setIsLoadingDependencyAnalysis(false)
      })
      return
    }

    let ignore = false
    const accessToken = session.accessToken
    const boardId = board.id
    const cardId = selectedCardId
    queueMicrotask(() => {
      if (!ignore) {
        setIsLoadingDependencyAnalysis(true)
      }
    })

    async function loadAnalysis() {
      try {
        const analysis = await getCardDependencyAnalysis(
          accessToken,
          workspace.id,
          project.id,
          boardId,
          cardId
        )

        if (!ignore) {
          setDependencyAnalysis(analysis)
        }
      } catch {
        if (!ignore) {
          setDependencyAnalysis(null)
        }
      } finally {
        if (!ignore) {
          setIsLoadingDependencyAnalysis(false)
        }
      }
    }

    void loadAnalysis()

    return () => {
      ignore = true
    }
  }, [board, project.id, selectedCardId, session, workspace.id])

  useEffect(() => {
    if (!notice) {
      return
    }

    const timeout = window.setTimeout(() => setNotice(null), 1800)
    return () => window.clearTimeout(timeout)
  }, [notice])

  function showNotice(message: string) {
    setNotice(message)
  }

  async function runBoardMutation(
    work: () => Promise<void>,
    options: { failureMessage: string; notice: string; rethrow?: boolean }
  ) {
    setIsMutating(true)
    setError(null)

    try {
      await work()
      showNotice(options.notice)
    } catch (requestError) {
      handleRequestError(requestError, options.failureMessage)

      if (options.rethrow) {
        throw requestError
      }
    } finally {
      setIsMutating(false)
    }
  }

  async function handleCreateBoard(
    template: BoardTemplate,
    customListTitles?: string[]
  ) {
    if (!session || isReadOnly) {
      return
    }

    setIsCreatingBoard(true)
    setError(null)

    try {
      setBoard(
        await createBoard(session.accessToken, workspace.id, project.id, {
          name: `${project.name} Board`,
          template,
          customListTitles,
        })
      )
      showNotice("Board created")
    } catch (requestError) {
      handleRequestError(requestError, "Board could not be created.")
    } finally {
      setIsCreatingBoard(false)
    }
  }

  async function handleCreateList() {
    if (!session || !board || !newListTitle.trim() || isReadOnly) {
      return
    }

    await runBoardMutation(
      async () => {
        setBoard(
          await createList(
            session.accessToken,
            workspace.id,
            project.id,
            board.id,
            newListTitle
          )
        )
        setNewListTitle("")
        setIsAddingList(false)
      },
      { failureMessage: "List could not be created.", notice: "List created" }
    )
  }

  async function handleRenameList(list: BoardList, title: string) {
    if (
      !session ||
      !board ||
      !title.trim() ||
      title === list.title ||
      isReadOnly
    ) {
      return
    }

    setBoard(renameListInBoard(board, list.id, title))

    try {
      setBoard(
        await updateList(
          session.accessToken,
          workspace.id,
          project.id,
          board.id,
          list.id,
          title
        )
      )
    } catch (requestError) {
      setBoard(board)
      handleRequestError(requestError, "List could not be renamed.")
    }
  }

  async function handleQuickAdd(list: BoardList) {
    if (!session || !board || !quickTitle.trim() || isReadOnly) {
      return
    }

    await runBoardMutation(
      async () => {
        setBoard(
          await createCard(
            session.accessToken,
            workspace.id,
            project.id,
            board.id,
            list.id,
            { title: quickTitle }
          )
        )
        setQuickTitle("")
        setQuickAddListId(null)
      },
      { failureMessage: "Card could not be created.", notice: "Card created" }
    )
  }

  async function handleMoveCard(cardId: string, listId: string) {
    if (!session || !board || isReadOnly) {
      return
    }

    const card = findCard(board, cardId)

    if (!card || card.listId === listId) {
      return
    }

    const previousBoard = board
    setBoard(moveCardInBoard(board, cardId, listId))

    try {
      setBoard(
        await moveCard(
          session.accessToken,
          workspace.id,
          project.id,
          board.id,
          cardId,
          listId
        )
      )
      showNotice("Card moved")
    } catch (requestError) {
      setBoard(previousBoard)
      handleRequestError(requestError, "Card could not be moved.")
    } finally {
      setDraggingCardId(null)
    }
  }

  async function handleSaveCard(card: BoardCard, input: BoardCardInput) {
    if (!session || !board || isReadOnly) {
      return
    }

    await runBoardMutation(
      async () => {
        setBoard(
          await updateCard(
            session.accessToken,
            workspace.id,
            project.id,
            board.id,
            card.id,
            input
          )
        )
      },
      {
        failureMessage: "Card could not be updated.",
        notice: "Card saved",
        rethrow: true,
      }
    )
  }

  async function handleDeleteCard(card: BoardCard) {
    if (!session || !board || !canDeleteCards || isReadOnly) {
      return
    }

    await runBoardMutation(
      async () => {
        await deleteCard(
          session.accessToken,
          workspace.id,
          project.id,
          board.id,
          card.id
        )
        setBoard(removeCardFromBoard(board, card.id))
        setSelectedCardId(null)
      },
      { failureMessage: "Card could not be deleted.", notice: "Card deleted" }
    )
  }

  async function handleDuplicateCard(card: BoardCard) {
    if (!session || !board || isReadOnly) {
      return
    }

    await runBoardMutation(
      async () => {
        setBoard(
          await createCard(
            session.accessToken,
            workspace.id,
            project.id,
            board.id,
            card.listId,
            {
              assigneeIds: card.assignees.map((assignee) => assignee.userId),
              description: card.description ?? "",
              dueDate: card.dueDate,
              labels: card.labels,
              priority: card.priority ?? "",
              title: `Copy of ${card.title}`,
            }
          )
        )
      },
      {
        failureMessage: "Card could not be duplicated.",
        notice: "Card duplicated",
      }
    )
  }

  async function handleAddComment(card: BoardCard, body: string) {
    if (!session || !board || isReadOnly) {
      return
    }

    await runBoardMutation(
      async () => {
        setBoard(
          await addCardComment(
            session.accessToken,
            workspace.id,
            project.id,
            board.id,
            card.id,
            body
          )
        )
      },
      {
        failureMessage: "Comment could not be added.",
        notice: "Comment added",
        rethrow: true,
      }
    )
  }

  async function handleCreateSubtask(card: BoardCard, title: string) {
    if (!session || !board || isReadOnly) {
      return
    }

    await runBoardMutation(
      async () => {
        setBoard(
          await createSubtask(
            session.accessToken,
            workspace.id,
            project.id,
            board.id,
            card.id,
            title
          )
        )
      },
      {
        failureMessage: "Subtask could not be added.",
        notice: "Subtask added",
        rethrow: true,
      }
    )
  }

  async function handleUpdateSubtask(
    card: BoardCard,
    subtask: BoardCardSubtask,
    input: { title?: string; isCompleted?: boolean }
  ) {
    if (!session || !board || isReadOnly) {
      return
    }

    await runBoardMutation(
      async () => {
        setBoard(
          await updateSubtask(
            session.accessToken,
            workspace.id,
            project.id,
            board.id,
            card.id,
            subtask.id,
            input
          )
        )
      },
      {
        failureMessage: "Subtask could not be updated.",
        notice: "Subtask updated",
        rethrow: true,
      }
    )
  }

  async function handleDeleteSubtask(
    card: BoardCard,
    subtask: BoardCardSubtask
  ) {
    if (!session || !board || isReadOnly) {
      return
    }

    await runBoardMutation(
      async () => {
        await deleteSubtask(
          session.accessToken,
          workspace.id,
          project.id,
          board.id,
          card.id,
          subtask.id
        )
        await loadBoard()
      },
      {
        failureMessage: "Subtask could not be deleted.",
        notice: "Subtask deleted",
      }
    )
  }

  async function handleAddDependency(card: BoardCard, dependsOnCardId: string) {
    if (!session || !board || isReadOnly) {
      return
    }

    await runBoardMutation(
      async () => {
        setBoard(
          await addCardDependency(
            session.accessToken,
            workspace.id,
            project.id,
            board.id,
            card.id,
            dependsOnCardId
          )
        )
      },
      {
        failureMessage: "Dependency could not be added.",
        notice: "Dependency added",
        rethrow: true,
      }
    )
  }

  async function handleDeleteDependency(
    card: BoardCard,
    dependsOnCardId: string
  ) {
    if (!session || !board || isReadOnly) {
      return
    }

    await runBoardMutation(
      async () => {
        await deleteCardDependency(
          session.accessToken,
          workspace.id,
          project.id,
          board.id,
          card.id,
          dependsOnCardId
        )
        await loadBoard()
      },
      {
        failureMessage: "Dependency could not be removed.",
        notice: "Dependency removed",
      }
    )
  }

  return {
    board,
    boardGraph,
    boardLabels,
    canDeleteCards,
    canShowPlanningPanel,
    cardCount,
    dependencyAnalysis,
    dependencyCandidates,
    draggingCardId,
    error,
    filters,
    handleAddComment,
    handleAddDependency,
    handleCreateBoard,
    handleCreateList,
    handleCreateSubtask,
    handleDeleteCard,
    handleDeleteDependency,
    handleDeleteSubtask,
    handleDuplicateCard,
    handleMoveCard,
    handleQuickAdd,
    handleRenameList,
    handleSaveCard,
    handleUpdateSubtask,
    isAddingList,
    isCreatingBoard,
    isLoading,
    isLoadingDependencyAnalysis,
    isMutating,
    isPlanningPanelOpen,
    isPlanningPanelVisible,
    isReadOnly,
    members,
    metrics,
    myCardCount,
    newListTitle,
    notice,
    quickAddListId,
    quickTitle,
    selectedCard,
    setDraggingCardId,
    setIsAddingList,
    setIsPlanningPanelOpen,
    setNewListTitle,
    setQuickAddListId,
    setQuickTitle,
    setSelectedCardId,
    wipAlerts,
  }
}
