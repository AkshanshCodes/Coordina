export type BoardTemplate =
  | "BASIC"
  | "AGILE_SCRUM"
  | "BUG_TRACKING"
  | "PRODUCT_ROADMAP"
  | "CUSTOM"

export type BoardTemplateDefinition = {
  id: BoardTemplate
  name: string
  summary: string
  listTitles: string[]
  previewCards: string[]
}

export type BoardCardPriority = "LOW" | "MEDIUM" | "HIGH"

export type BoardTaskStatus = "TODO" | "IN_PROGRESS" | "DONE"

export type Board = {
  id: string
  projectId: string
  name: string
  template: BoardTemplate
  createdAt: string
  updatedAt: string
  lists: BoardList[]
}

export type BoardList = {
  id: string
  boardId: string
  title: string
  position: number
  taskStatus: BoardTaskStatus
  createdAt: string
  updatedAt: string
  cards: BoardCard[]
}

export type BoardCard = {
  id: string
  boardId: string
  listId: string
  title: string
  description: string | null
  priority: BoardCardPriority | null
  dueDate: string | null
  estimatedMinutes: number | null
  labels: string[]
  isCompleted: boolean
  completedAt: string | null
  completedBy: BoardCardUser | null
  position: number
  createdAt: string
  updatedAt: string
  assignees: BoardCardAssignee[]
  comments: BoardCardComment[]
  subtasks: BoardCardSubtask[]
  dependencies: BoardCardDependency[]
}

export type BoardCardUser = {
  userId: string
  name: string | null
  email: string | null
}

export type BoardCardAssignee = {
  userId: string
  name: string | null
  email: string | null
}

export type BoardCardComment = {
  id: string
  cardId: string
  author: BoardCardUser
  body: string
  createdAt: string
}

export type BoardCardSubtask = {
  id: string
  cardId: string
  title: string
  isCompleted: boolean
  completedAt: string | null
  completedBy: BoardCardUser | null
  position: number
  createdAt: string
  updatedAt: string
}

export type BoardCardDependency = {
  cardId: string
  title: string
  isCompleted: boolean
}

export type BoardGraph = {
  boardId: string
  isAcyclic: boolean
  availableMinutes: number
  context: string | null
  readyCards: BoardGraphCard[]
  unblockedCards: BoardGraphCard[]
  dependencyOrder: BoardGraphCard[]
  criticalPath: BoardGraphCard[]
  nextCards: BoardGraphCard[]
  planItems: BoardGraphPlanItem[]
}

export type BoardCardDependencyAnalysis = {
  cardId: string
  isStructurallyReady: boolean
  isUnblocked: boolean
  isOnCriticalPath: boolean
  dependencyDepth: number
  dependentDepth: number
  transitiveDependentCount: number
  blockingDependencies: BoardGraphCard[]
  suggestedDependencies: BoardGraphCard[]
  impactedDependents: BoardGraphCard[]
  directlyUnlockedDependents: BoardGraphCard[]
}

export type BoardGraphCard = {
  id: string
  listId: string
  title: string
  isCompleted: boolean
  dependencyCount: number
  dependentCount: number
  dependencyDepth: number
  dependentDepth: number
  transitiveDependentCount: number
  isCriticalPath: boolean
}

export type BoardGraphPlanItem = {
  card: BoardGraphCard
  score: number
  isActionable: boolean
  blockerCount: number
  estimatedMinutes: number | null
  fitsAvailableTime: boolean
  isAssignedToMe: boolean
  isContextMatch: boolean
  isInProgress: boolean
  isShortestAlternative: boolean
  reasons: string[]
}

export type BoardCardInput = {
  title: string
  description?: string
  priority?: BoardCardPriority | ""
  dueDate?: string | null
  estimatedMinutes?: number | null
  labels?: string[]
  assigneeIds?: string[]
  isCompleted?: boolean
}
