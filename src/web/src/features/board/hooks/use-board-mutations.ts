import { type Dispatch, type SetStateAction, useEffect, useState } from "react"

import * as boardApi from "@/features/board/api/board-api"
import { isNotFound } from "@/features/board/hooks/use-board-resource"
import { findCard } from "@/features/board/model/board-selectors"
import {
  moveCardInBoard,
  removeCardFromBoard,
  renameListInBoard,
} from "@/features/board/model/board-updates"
import type {
  Board,
  BoardCard,
  BoardCardInput,
  BoardCardSubtask,
  BoardList,
  BoardTemplate,
} from "@/types/task"

type UseBoardMutationsOptions = {
  accessToken: string | null
  workspaceId: string
  projectId: string
  board: Board | null
  setBoard: Dispatch<SetStateAction<Board | null>>
  reload: () => Promise<void>
  isReadOnly: boolean
  onError: (error: unknown, fallback: string) => void
  clearError: () => void
}

type MutationMessages = {
  failureMessage: string
  notice?: string
}

const noticeDuration = 1800

export function useBoardMutations({
  accessToken,
  workspaceId,
  projectId,
  board,
  setBoard,
  reload,
  isReadOnly,
  onError,
  clearError,
}: UseBoardMutationsOptions) {
  const [isMutating, setIsMutating] = useState(false)
  const [isCreatingBoard, setIsCreatingBoard] = useState(false)
  const [notice, setNotice] = useState<string | null>(null)

  useEffect(() => {
    if (!notice) {
      return
    }

    const timeout = window.setTimeout(() => setNotice(null), noticeDuration)
    return () => window.clearTimeout(timeout)
  }, [notice])

  const target =
    accessToken && board && !isReadOnly
      ? { accessToken, boardId: board.id }
      : null

  function fail(requestError: unknown, failureMessage: string) {
    if (isNotFound(requestError)) {
      void reload()
      return
    }

    onError(requestError, failureMessage)
  }

  async function run(
    work: () => Promise<void>,
    { failureMessage, notice: successNotice }: MutationMessages,
    rethrow = false
  ) {
    setIsMutating(true)
    clearError()

    try {
      await work()

      if (successNotice) {
        setNotice(successNotice)
      }

      return true
    } catch (requestError) {
      fail(requestError, failureMessage)

      if (rethrow) {
        throw requestError
      }

      return false
    } finally {
      setIsMutating(false)
    }
  }

  async function runOptimistic(
    previous: Board,
    optimistic: Board,
    request: () => Promise<Board>,
    { failureMessage, notice: successNotice }: MutationMessages
  ) {
    setBoard(optimistic)

    try {
      setBoard(await request())

      if (successNotice) {
        setNotice(successNotice)
      }

      return true
    } catch (requestError) {
      setBoard((current) => (current === optimistic ? previous : current))
      fail(requestError, failureMessage)
      return false
    }
  }

  async function createBoard(
    name: string,
    template: BoardTemplate,
    customListTitles?: string[]
  ) {
    if (!accessToken || isReadOnly) {
      return
    }

    setIsCreatingBoard(true)
    clearError()

    try {
      setBoard(
        await boardApi.createBoard(accessToken, workspaceId, projectId, {
          name,
          template,
          customListTitles,
        })
      )
      setNotice("Board created")
    } catch (requestError) {
      fail(requestError, "Board could not be created.")
    } finally {
      setIsCreatingBoard(false)
    }
  }

  async function createList(title: string) {
    if (!target || !title.trim()) {
      return false
    }

    return run(
      async () => {
        setBoard(
          await boardApi.createList(
            target.accessToken,
            workspaceId,
            projectId,
            target.boardId,
            title
          )
        )
      },
      { failureMessage: "List could not be created.", notice: "List created" }
    )
  }

  async function renameList(list: BoardList, title: string) {
    if (!target || !board || !title.trim() || title === list.title) {
      return false
    }

    return runOptimistic(
      board,
      renameListInBoard(board, list.id, title),
      () =>
        boardApi.updateList(
          target.accessToken,
          workspaceId,
          projectId,
          target.boardId,
          list.id,
          title
        ),
      { failureMessage: "List could not be renamed." }
    )
  }

  async function addCard(
    listId: string,
    input: BoardCardInput,
    messages: MutationMessages
  ) {
    if (!target || !input.title.trim()) {
      return false
    }

    return run(async () => {
      setBoard(
        await boardApi.createCard(
          target.accessToken,
          workspaceId,
          projectId,
          target.boardId,
          listId,
          input
        )
      )
    }, messages)
  }

  function quickAddCard(listId: string, title: string) {
    return addCard(
      listId,
      { title },
      { failureMessage: "Card could not be created.", notice: "Card created" }
    )
  }

  function duplicateCard(card: BoardCard) {
    return addCard(
      card.listId,
      {
        assigneeIds: card.assignees.map((assignee) => assignee.userId),
        description: card.description ?? "",
        dueDate: card.dueDate,
        labels: card.labels,
        priority: card.priority ?? "",
        title: `Copy of ${card.title}`,
      },
      {
        failureMessage: "Card could not be duplicated.",
        notice: "Card duplicated",
      }
    )
  }

  async function moveCard(cardId: string, listId: string) {
    if (!target || !board) {
      return false
    }

    const card = findCard(board, cardId)

    if (!card || card.listId === listId) {
      return false
    }

    return runOptimistic(
      board,
      moveCardInBoard(board, cardId, listId),
      () =>
        boardApi.moveCard(
          target.accessToken,
          workspaceId,
          projectId,
          target.boardId,
          cardId,
          listId
        ),
      { failureMessage: "Card could not be moved.", notice: "Card moved" }
    )
  }

  async function saveCard(card: BoardCard, input: BoardCardInput) {
    if (!target) {
      return
    }

    await run(
      async () => {
        setBoard(
          await boardApi.updateCard(
            target.accessToken,
            workspaceId,
            projectId,
            target.boardId,
            card.id,
            input
          )
        )
      },
      { failureMessage: "Card could not be updated.", notice: "Card saved" },
      true
    )
  }

  async function deleteCard(card: BoardCard) {
    if (!target) {
      return false
    }

    return run(
      async () => {
        await boardApi.deleteCard(
          target.accessToken,
          workspaceId,
          projectId,
          target.boardId,
          card.id
        )
        setBoard((current) =>
          current ? removeCardFromBoard(current, card.id) : current
        )
      },
      { failureMessage: "Card could not be deleted.", notice: "Card deleted" }
    )
  }

  async function replaceBoardFrom(
    request: (accessToken: string, boardId: string) => Promise<Board>,
    messages: MutationMessages
  ) {
    if (!target) {
      return
    }

    await run(
      async () => {
        setBoard(await request(target.accessToken, target.boardId))
      },
      messages,
      true
    )
  }

  async function removeThenReload(
    request: (accessToken: string, boardId: string) => Promise<void>,
    messages: MutationMessages
  ) {
    if (!target) {
      return
    }

    await run(async () => {
      await request(target.accessToken, target.boardId)
      await reload()
    }, messages)
  }

  function addComment(card: BoardCard, body: string) {
    return replaceBoardFrom(
      (token, boardId) =>
        boardApi.addCardComment(
          token,
          workspaceId,
          projectId,
          boardId,
          card.id,
          body
        ),
      { failureMessage: "Comment could not be added.", notice: "Comment added" }
    )
  }

  function createSubtask(card: BoardCard, title: string) {
    return replaceBoardFrom(
      (token, boardId) =>
        boardApi.createSubtask(
          token,
          workspaceId,
          projectId,
          boardId,
          card.id,
          title
        ),
      { failureMessage: "Subtask could not be added.", notice: "Subtask added" }
    )
  }

  function updateSubtask(
    card: BoardCard,
    subtask: BoardCardSubtask,
    input: { title?: string; isCompleted?: boolean }
  ) {
    return replaceBoardFrom(
      (token, boardId) =>
        boardApi.updateSubtask(
          token,
          workspaceId,
          projectId,
          boardId,
          card.id,
          subtask.id,
          input
        ),
      {
        failureMessage: "Subtask could not be updated.",
        notice: "Subtask updated",
      }
    )
  }

  function deleteSubtask(card: BoardCard, subtask: BoardCardSubtask) {
    return removeThenReload(
      (token, boardId) =>
        boardApi.deleteSubtask(
          token,
          workspaceId,
          projectId,
          boardId,
          card.id,
          subtask.id
        ),
      {
        failureMessage: "Subtask could not be deleted.",
        notice: "Subtask deleted",
      }
    )
  }

  function addDependency(card: BoardCard, dependsOnCardId: string) {
    return replaceBoardFrom(
      (token, boardId) =>
        boardApi.addCardDependency(
          token,
          workspaceId,
          projectId,
          boardId,
          card.id,
          dependsOnCardId
        ),
      {
        failureMessage: "Dependency could not be added.",
        notice: "Dependency added",
      }
    )
  }

  function deleteDependency(card: BoardCard, dependsOnCardId: string) {
    return removeThenReload(
      (token, boardId) =>
        boardApi.deleteCardDependency(
          token,
          workspaceId,
          projectId,
          boardId,
          card.id,
          dependsOnCardId
        ),
      {
        failureMessage: "Dependency could not be removed.",
        notice: "Dependency removed",
      }
    )
  }

  return {
    addComment,
    addDependency,
    createBoard,
    createList,
    createSubtask,
    deleteCard,
    deleteDependency,
    deleteSubtask,
    duplicateCard,
    isCreatingBoard,
    isMutating,
    moveCard,
    notice,
    quickAddCard,
    renameList,
    saveCard,
    updateSubtask,
  }
}
