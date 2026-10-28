import { apiRequest } from "@/lib/api"
import type {
  Board,
  BoardCardDependencyAnalysis,
  BoardCardInput,
  BoardGraph,
  BoardTemplate,
} from "@/types/task"

export async function getDefaultBoard(
  accessToken: string,
  workspaceId: string,
  projectId: string
) {
  return apiRequest<Board>(
    `/workspaces/${workspaceId}/projects/${projectId}/board`,
    {
      accessToken,
      errorMessage: "Board request failed.",
    }
  )
}

export async function createBoard(
  accessToken: string,
  workspaceId: string,
  projectId: string,
  input: { name: string; template: BoardTemplate; customListTitles?: string[] }
) {
  return apiRequest<Board>(
    `/workspaces/${workspaceId}/projects/${projectId}/boards`,
    {
      accessToken,
      method: "POST",
      body: JSON.stringify(input),
      errorMessage: "Board request failed.",
    }
  )
}

export async function createList(
  accessToken: string,
  workspaceId: string,
  projectId: string,
  boardId: string,
  title: string
) {
  return apiRequest<Board>(
    `/workspaces/${workspaceId}/projects/${projectId}/boards/${boardId}/lists`,
    {
      accessToken,
      method: "POST",
      body: JSON.stringify({ title }),
      errorMessage: "Board request failed.",
    }
  )
}

export async function updateList(
  accessToken: string,
  workspaceId: string,
  projectId: string,
  boardId: string,
  listId: string,
  title: string
) {
  return apiRequest<Board>(
    `/workspaces/${workspaceId}/projects/${projectId}/boards/${boardId}/lists/${listId}`,
    {
      accessToken,
      method: "PATCH",
      body: JSON.stringify({ title }),
      errorMessage: "Board request failed.",
    }
  )
}

export async function createCard(
  accessToken: string,
  workspaceId: string,
  projectId: string,
  boardId: string,
  listId: string,
  input: BoardCardInput
) {
  return apiRequest<Board>(
    `/workspaces/${workspaceId}/projects/${projectId}/boards/${boardId}/lists/${listId}/cards`,
    {
      accessToken,
      method: "POST",
      body: JSON.stringify(normalizeCardInput(input)),
      errorMessage: "Board request failed.",
    }
  )
}

export async function updateCard(
  accessToken: string,
  workspaceId: string,
  projectId: string,
  boardId: string,
  cardId: string,
  input: BoardCardInput
) {
  return apiRequest<Board>(
    `/workspaces/${workspaceId}/projects/${projectId}/boards/${boardId}/cards/${cardId}`,
    {
      accessToken,
      method: "PATCH",
      body: JSON.stringify(normalizeCardUpdateInput(input)),
      errorMessage: "Board request failed.",
    }
  )
}

export async function moveCard(
  accessToken: string,
  workspaceId: string,
  projectId: string,
  boardId: string,
  cardId: string,
  listId: string
) {
  return apiRequest<Board>(
    `/workspaces/${workspaceId}/projects/${projectId}/boards/${boardId}/cards/${cardId}/move`,
    {
      accessToken,
      method: "PATCH",
      body: JSON.stringify({ listId }),
      errorMessage: "Board request failed.",
    }
  )
}

export async function deleteCard(
  accessToken: string,
  workspaceId: string,
  projectId: string,
  boardId: string,
  cardId: string
) {
  await apiRequest<void>(
    `/workspaces/${workspaceId}/projects/${projectId}/boards/${boardId}/cards/${cardId}`,
    {
      accessToken,
      method: "DELETE",
      errorMessage: "Board request failed.",
    }
  )
}

export async function getBoardGraph(
  accessToken: string,
  workspaceId: string,
  projectId: string,
  boardId: string
) {
  return apiRequest<BoardGraph>(
    `/workspaces/${workspaceId}/projects/${projectId}/boards/${boardId}/graph`,
    {
      accessToken,
      errorMessage: "Board graph request failed.",
    }
  )
}

export async function getCardDependencyAnalysis(
  accessToken: string,
  workspaceId: string,
  projectId: string,
  boardId: string,
  cardId: string
) {
  return apiRequest<BoardCardDependencyAnalysis>(
    `/workspaces/${workspaceId}/projects/${projectId}/boards/${boardId}/cards/${cardId}/dependency-analysis`,
    {
      accessToken,
      errorMessage: "Card dependency analysis request failed.",
    }
  )
}

export async function addCardComment(
  accessToken: string,
  workspaceId: string,
  projectId: string,
  boardId: string,
  cardId: string,
  body: string
) {
  return apiRequest<Board>(
    `/workspaces/${workspaceId}/projects/${projectId}/boards/${boardId}/cards/${cardId}/comments`,
    {
      accessToken,
      method: "POST",
      body: JSON.stringify({ body }),
      errorMessage: "Board request failed.",
    }
  )
}

export async function createSubtask(
  accessToken: string,
  workspaceId: string,
  projectId: string,
  boardId: string,
  cardId: string,
  title: string
) {
  return apiRequest<Board>(
    `/workspaces/${workspaceId}/projects/${projectId}/boards/${boardId}/cards/${cardId}/subtasks`,
    {
      accessToken,
      method: "POST",
      body: JSON.stringify({ title }),
      errorMessage: "Board request failed.",
    }
  )
}

export async function updateSubtask(
  accessToken: string,
  workspaceId: string,
  projectId: string,
  boardId: string,
  cardId: string,
  subtaskId: string,
  input: { title?: string; isCompleted?: boolean }
) {
  return apiRequest<Board>(
    `/workspaces/${workspaceId}/projects/${projectId}/boards/${boardId}/cards/${cardId}/subtasks/${subtaskId}`,
    {
      accessToken,
      method: "PATCH",
      body: JSON.stringify(input),
      errorMessage: "Board request failed.",
    }
  )
}

export async function deleteSubtask(
  accessToken: string,
  workspaceId: string,
  projectId: string,
  boardId: string,
  cardId: string,
  subtaskId: string
) {
  await apiRequest<void>(
    `/workspaces/${workspaceId}/projects/${projectId}/boards/${boardId}/cards/${cardId}/subtasks/${subtaskId}`,
    {
      accessToken,
      method: "DELETE",
      errorMessage: "Board request failed.",
    }
  )
}

export async function addCardDependency(
  accessToken: string,
  workspaceId: string,
  projectId: string,
  boardId: string,
  cardId: string,
  dependsOnCardId: string
) {
  return apiRequest<Board>(
    `/workspaces/${workspaceId}/projects/${projectId}/boards/${boardId}/cards/${cardId}/dependencies`,
    {
      accessToken,
      method: "POST",
      body: JSON.stringify({ dependsOnCardId }),
      errorMessage: "Board request failed.",
    }
  )
}

export async function deleteCardDependency(
  accessToken: string,
  workspaceId: string,
  projectId: string,
  boardId: string,
  cardId: string,
  dependsOnCardId: string
) {
  await apiRequest<void>(
    `/workspaces/${workspaceId}/projects/${projectId}/boards/${boardId}/cards/${cardId}/dependencies/${dependsOnCardId}`,
    {
      accessToken,
      method: "DELETE",
      errorMessage: "Board request failed.",
    }
  )
}

function normalizeCardInput(input: BoardCardInput) {
  return {
    ...input,
    priority: input.priority || null,
    dueDate: input.dueDate || null,
    labels: input.labels ?? [],
    assigneeIds: input.assigneeIds ?? [],
    isCompleted: input.isCompleted,
  }
}

function normalizeCardUpdateInput(input: BoardCardInput) {
  return {
    ...normalizeCardInput(input),
    clearDueDate: input.dueDate === null || input.dueDate === "",
    priority: input.priority ?? "",
  }
}
