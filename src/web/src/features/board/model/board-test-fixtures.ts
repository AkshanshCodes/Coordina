import type {
  Board,
  BoardCard,
  BoardGraphPlanItem,
  BoardList,
  BoardTaskStatus,
} from "@/types/task"

const timestamp = "2026-10-28T10:00:00Z"

export function makeCard(overrides: Partial<BoardCard> = {}): BoardCard {
  const id = overrides.id ?? "card"

  return {
    id,
    boardId: "board",
    listId: "list",
    title: id,
    description: null,
    priority: null,
    dueDate: null,
    estimatedMinutes: null,
    labels: [],
    isCompleted: false,
    completedAt: null,
    completedBy: null,
    position: 0,
    createdAt: timestamp,
    updatedAt: timestamp,
    assignees: [],
    comments: [],
    subtasks: [],
    dependencies: [],
    ...overrides,
  }
}

export function makeList(
  title: string,
  taskStatus: BoardTaskStatus,
  cards: BoardCard[] = []
): BoardList {
  return {
    id: title,
    boardId: "board",
    title,
    position: 0,
    taskStatus,
    createdAt: timestamp,
    updatedAt: timestamp,
    cards: cards.map((card) => ({ ...card, listId: title })),
  }
}

export function makeBoard(lists: BoardList[]): Board {
  return {
    id: "board",
    projectId: "project",
    name: "Board",
    template: "CUSTOM",
    createdAt: timestamp,
    updatedAt: timestamp,
    lists,
  }
}

export function manyCards(prefix: string, count: number) {
  return Array.from({ length: count }, (_, index) =>
    makeCard({ id: `${prefix}-${index}` })
  )
}

export function makePlanItem(
  overrides: Partial<BoardGraphPlanItem> & { id?: string } = {}
): BoardGraphPlanItem {
  const { id = "plan", ...rest } = overrides

  return {
    card: {
      id,
      listId: "list",
      title: id,
      isCompleted: false,
      dependencyCount: 0,
      dependentCount: 0,
      dependencyDepth: 0,
      dependentDepth: 0,
      transitiveDependentCount: 0,
      isCriticalPath: false,
    },
    score: 10,
    isActionable: true,
    blockerCount: 0,
    estimatedMinutes: null,
    fitsAvailableTime: true,
    isAssignedToMe: false,
    isContextMatch: false,
    isInProgress: false,
    isShortestAlternative: false,
    reasons: ["Ready now"],
    ...rest,
  }
}
