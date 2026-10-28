import { todayDateKey } from "@/features/board/model/dates"
import {
  normalizeLabel,
  normalizeLabels,
  sameLabels,
  sameStrings,
} from "@/features/board/model/labels"
import type { BoardCard, BoardCardInput, BoardCardPriority } from "@/types/task"

export const maxCardLabels = 8
const maxLabelSuggestions = 10

export type CardDraft = {
  title: string
  description: string
  priority: BoardCardPriority | ""
  dueDate: string
  labels: string[]
  labelInput: string
  assigneeIds: string[]
  isCompleted: boolean
}

export function draftFromCard(card: BoardCard | null): CardDraft {
  return {
    title: card?.title ?? "",
    description: card?.description ?? "",
    priority: card?.priority ?? "",
    dueDate: card?.dueDate ?? "",
    labels: normalizeLabels(card?.labels ?? []),
    labelInput: "",
    assigneeIds: card?.assignees.map((assignee) => assignee.userId) ?? [],
    isCompleted: card?.isCompleted ?? false,
  }
}

export function hasDraftChanges(draft: CardDraft, card: BoardCard | null) {
  if (!card) {
    return false
  }

  return (
    draft.title.trim() !== card.title ||
    draft.description.trim() !== (card.description ?? "") ||
    draft.priority !== (card.priority ?? "") ||
    draft.dueDate !== (card.dueDate ?? "") ||
    draft.isCompleted !== card.isCompleted ||
    !sameLabels(
      normalizeLabels([...draft.labels, draft.labelInput]),
      normalizeLabels(card.labels)
    ) ||
    !sameStrings(
      [...draft.assigneeIds].sort(),
      card.assignees.map((assignee) => assignee.userId).sort()
    )
  )
}

export function validateDraft(draft: CardDraft, today = todayDateKey()) {
  if (!draft.title.trim()) {
    return "Card title is required."
  }

  if (draft.dueDate && draft.dueDate < today) {
    return "Due date cannot be in the past."
  }

  return null
}

export function submittedDraftLabels(draft: CardDraft) {
  return normalizeLabels([...draft.labels, draft.labelInput]).slice(
    0,
    maxCardLabels
  )
}

export function toCardInput(
  draft: CardDraft,
  labels: string[],
  isCompleted: boolean
): BoardCardInput {
  return {
    title: draft.title,
    description: draft.description,
    priority: draft.priority,
    dueDate: draft.dueDate || null,
    labels,
    assigneeIds: draft.assigneeIds,
    isCompleted,
  }
}

export function addDraftLabel(labels: string[], input: string) {
  const next = normalizeLabel(input)

  return next
    ? normalizeLabels([...labels, next]).slice(0, maxCardLabels)
    : null
}

export function toggleDraftLabel(labels: string[], label: string) {
  const exists = labels.some(
    (candidate) => candidate.toLowerCase() === label.toLowerCase()
  )

  if (exists) {
    return labels.filter(
      (candidate) => candidate.toLowerCase() !== label.toLowerCase()
    )
  }

  return normalizeLabels([...labels, label]).slice(0, maxCardLabels)
}

export function suggestDraftLabels(
  available: string[],
  selected: string[],
  input: string
) {
  const query = normalizeLabel(input)?.toLowerCase() ?? ""

  return available
    .filter((label) => !includesLabel(selected, label))
    .filter((label) => !query || label.toLowerCase().includes(query))
    .slice(0, maxLabelSuggestions)
}

export function canCreateDraftLabel(
  available: string[],
  selected: string[],
  input: string
) {
  const normalized = normalizeLabel(input)

  return Boolean(
    normalized &&
    !includesLabel(available, normalized) &&
    !includesLabel(selected, normalized)
  )
}

function includesLabel(labels: string[], label: string) {
  return labels.some(
    (candidate) => candidate.toLowerCase() === label.toLowerCase()
  )
}
