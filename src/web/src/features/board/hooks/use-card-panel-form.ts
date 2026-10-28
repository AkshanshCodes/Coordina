import { useEffect, useMemo, useState } from "react"

import { useDismissibleLayer } from "@/features/board/hooks/use-dismissible-layer"
import {
  addDraftLabel,
  canCreateDraftLabel,
  type CardDraft,
  draftFromCard,
  hasDraftChanges,
  submittedDraftLabels,
  suggestDraftLabels,
  toCardInput,
  toggleDraftLabel,
  validateDraft,
} from "@/features/board/model/card-draft"
import { normalizeLabel } from "@/features/board/model/labels"
import type { BoardCard, BoardCardInput, BoardCardPriority } from "@/types/task"

type UseCardPanelFormOptions = {
  availableLabels: string[]
  card: BoardCard | null
  isReadOnly: boolean
  onAddComment: (card: BoardCard, body: string) => Promise<void>
  onAddDependency: (card: BoardCard, dependsOnCardId: string) => Promise<void>
  onCreateSubtask: (card: BoardCard, title: string) => Promise<void>
  onSave: (card: BoardCard, input: BoardCardInput) => Promise<void>
}

export function useCardPanelForm({
  availableLabels,
  card,
  isReadOnly,
  onAddComment,
  onAddDependency,
  onCreateSubtask,
  onSave,
}: UseCardPanelFormOptions) {
  const [title, setTitle] = useState("")
  const [description, setDescription] = useState("")
  const [priority, setPriority] = useState<BoardCardPriority | "">("")
  const [dueDate, setDueDate] = useState("")
  const [labels, setLabels] = useState<string[]>([])
  const [labelInput, setLabelInput] = useState("")
  const [assigneeIds, setAssigneeIds] = useState<string[]>([])
  const [isCompleted, setIsCompleted] = useState(false)
  const [commentInput, setCommentInput] = useState("")
  const [subtaskInput, setSubtaskInput] = useState("")
  const [dependencyId, setDependencyId] = useState("")
  const [error, setError] = useState<string | null>(null)
  const [saveState, setSaveState] = useState<"idle" | "saving" | "saved">(
    "idle"
  )
  const calendar = useDismissibleLayer()
  const labelMenu = useDismissibleLayer()
  const setIsCalendarOpen = calendar.setIsOpen
  const setIsLabelMenuOpen = labelMenu.setIsOpen

  const draft = useMemo<CardDraft>(
    () => ({
      title,
      description,
      priority,
      dueDate,
      labels,
      labelInput,
      assigneeIds,
      isCompleted,
    }),
    [
      assigneeIds,
      description,
      dueDate,
      isCompleted,
      labelInput,
      labels,
      priority,
      title,
    ]
  )
  const hasChanges = useMemo(() => hasDraftChanges(draft, card), [card, draft])
  const labelSuggestions = useMemo(
    () => suggestDraftLabels(availableLabels, labels, labelInput),
    [availableLabels, labelInput, labels]
  )
  const normalizedLabelInput = normalizeLabel(labelInput)
  const canCreateLabel = canCreateDraftLabel(
    availableLabels,
    labels,
    labelInput
  )

  useEffect(() => {
    queueMicrotask(() => {
      const next = draftFromCard(card)
      setTitle(next.title)
      setDescription(next.description)
      setPriority(next.priority)
      setDueDate(next.dueDate)
      setLabels(next.labels)
      setLabelInput(next.labelInput)
      setAssigneeIds(next.assigneeIds)
      setIsCompleted(next.isCompleted)
      setCommentInput("")
      setSubtaskInput("")
      setDependencyId("")
      setIsCalendarOpen(false)
      setIsLabelMenuOpen(false)
      setSaveState("idle")
      setError(null)
    })
  }, [card, setIsCalendarOpen, setIsLabelMenuOpen])

  async function submit(nextCompleted = isCompleted) {
    const currentCard = card

    if (isReadOnly || !currentCard) {
      return
    }

    const validationError = validateDraft(draft)

    if (validationError) {
      setError(validationError)
      return
    }

    const submittedLabels = submittedDraftLabels(draft)
    setLabels(submittedLabels)
    setLabelInput("")
    setError(null)
    setSaveState("saving")
    setIsCompleted(nextCompleted)

    try {
      await onSave(
        currentCard,
        toCardInput(draft, submittedLabels, nextCompleted)
      )
      setSaveState("saved")
      window.setTimeout(() => setSaveState("idle"), 1400)
    } catch {
      setIsCompleted(currentCard.isCompleted)
      setSaveState("idle")
    }
  }

  async function runCardAction(
    input: string,
    action: (card: BoardCard) => Promise<void>,
    onSuccess: () => void,
    failureMessage: string
  ) {
    const currentCard = card

    if (isReadOnly || !currentCard || !input.trim()) {
      return
    }

    setError(null)

    try {
      await action(currentCard)
      onSuccess()
    } catch {
      setError(failureMessage)
    }
  }

  function addComment() {
    return runCardAction(
      commentInput,
      (currentCard) => onAddComment(currentCard, commentInput),
      () => setCommentInput(""),
      "Comment could not be added."
    )
  }

  function addSubtask() {
    return runCardAction(
      subtaskInput,
      (currentCard) => onCreateSubtask(currentCard, subtaskInput),
      () => setSubtaskInput(""),
      "Subtask could not be added."
    )
  }

  function addDependency() {
    return runCardAction(
      dependencyId,
      (currentCard) => onAddDependency(currentCard, dependencyId),
      () => setDependencyId(""),
      "Dependency could not be added."
    )
  }

  function addLabel() {
    if (isReadOnly) {
      return
    }

    const next = addDraftLabel(labels, labelInput)

    if (!next) {
      return
    }

    setLabels(next)
    setLabelInput("")
    setIsLabelMenuOpen(false)
  }

  function toggleLabel(label: string) {
    if (isReadOnly) {
      return
    }

    setLabels((current) => toggleDraftLabel(current, label))
    setLabelInput("")
    setIsLabelMenuOpen(false)
  }

  return {
    addComment,
    addDependency,
    addLabel,
    addSubtask,
    assigneeIds,
    calendarContainerRef: calendar.containerRef,
    canCreateLabel,
    commentInput,
    dependencyId,
    description,
    dueDate,
    error,
    hasChanges,
    isCalendarOpen: calendar.isOpen,
    isCompleted,
    isLabelMenuOpen: labelMenu.isOpen,
    labelInput,
    labelMenuContainerRef: labelMenu.containerRef,
    labelSuggestions,
    labels,
    normalizedLabelInput,
    priority,
    saveState,
    setAssigneeIds,
    setCommentInput,
    setDependencyId,
    setDescription,
    setDueDate,
    setIsCalendarOpen,
    setIsLabelMenuOpen,
    setLabelInput,
    setLabels,
    setPriority,
    setSubtaskInput,
    setTitle,
    submit,
    subtaskInput,
    title,
    toggleLabel,
  }
}

export type CardPanelForm = ReturnType<typeof useCardPanelForm>
