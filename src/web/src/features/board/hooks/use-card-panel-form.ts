import { useEffect, useMemo, useRef, useState } from "react"

import {
  normalizeLabel,
  normalizeLabels,
  sameLabels,
  sameStrings,
} from "@/features/board/model/labels"
import { todayDateKey } from "@/features/board/model/dates"
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
  const [isCalendarOpen, setIsCalendarOpen] = useState(false)
  const [isLabelMenuOpen, setIsLabelMenuOpen] = useState(false)
  const [saveState, setSaveState] = useState<"idle" | "saving" | "saved">(
    "idle"
  )
  const calendarContainerRef = useRef<HTMLDivElement>(null)
  const labelMenuContainerRef = useRef<HTMLDivElement>(null)

  const hasChanges = useMemo(() => {
    if (!card) {
      return false
    }

    const nextLabels = normalizeLabels([...labels, labelInput])
    const currentLabels = normalizeLabels(card.labels)

    return (
      title.trim() !== card.title ||
      description.trim() !== (card.description ?? "") ||
      priority !== (card.priority ?? "") ||
      dueDate !== (card.dueDate ?? "") ||
      isCompleted !== card.isCompleted ||
      !sameLabels(nextLabels, currentLabels) ||
      !sameStrings(
        [...assigneeIds].sort(),
        card.assignees.map((assignee) => assignee.userId).sort()
      )
    )
  }, [
    assigneeIds,
    card,
    description,
    dueDate,
    isCompleted,
    labelInput,
    labels,
    priority,
    title,
  ])

  const labelSuggestions = useMemo(() => {
    const query = normalizeLabel(labelInput)?.toLowerCase() ?? ""

    return availableLabels
      .filter(
        (label) =>
          !labels.some(
            (selectedLabel) =>
              selectedLabel.toLowerCase() === label.toLowerCase()
          )
      )
      .filter((label) => !query || label.toLowerCase().includes(query))
      .slice(0, 10)
  }, [availableLabels, labelInput, labels])
  const normalizedLabelInput = normalizeLabel(labelInput)
  const canCreateLabel = Boolean(
    normalizedLabelInput &&
    !availableLabels.some(
      (label) => label.toLowerCase() === normalizedLabelInput.toLowerCase()
    ) &&
    !labels.some(
      (label) => label.toLowerCase() === normalizedLabelInput.toLowerCase()
    )
  )

  useEffect(() => {
    queueMicrotask(() => {
      setTitle(card?.title ?? "")
      setDescription(card?.description ?? "")
      setPriority(card?.priority ?? "")
      setDueDate(card?.dueDate ?? "")
      setLabels(normalizeLabels(card?.labels ?? []))
      setLabelInput("")
      setAssigneeIds(card?.assignees.map((assignee) => assignee.userId) ?? [])
      setIsCompleted(card?.isCompleted ?? false)
      setCommentInput("")
      setSubtaskInput("")
      setDependencyId("")
      setIsCalendarOpen(false)
      setIsLabelMenuOpen(false)
      setSaveState("idle")
      setError(null)
    })
  }, [card])

  useEffect(() => {
    if (!isCalendarOpen && !isLabelMenuOpen) {
      return
    }

    function dismissLayersOutside(target: EventTarget | null) {
      if (!(target instanceof Node)) {
        return
      }

      if (!calendarContainerRef.current?.contains(target)) {
        setIsCalendarOpen(false)
      }

      if (!labelMenuContainerRef.current?.contains(target)) {
        setIsLabelMenuOpen(false)
      }
    }

    function handlePointerDown(event: PointerEvent) {
      dismissLayersOutside(event.target)
    }

    function handleFocusIn(event: FocusEvent) {
      dismissLayersOutside(event.target)
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key !== "Escape") {
        return
      }

      setIsCalendarOpen(false)
      setIsLabelMenuOpen(false)
      event.stopPropagation()
    }

    document.addEventListener("pointerdown", handlePointerDown, true)
    document.addEventListener("focusin", handleFocusIn, true)
    window.addEventListener("keydown", handleKeyDown, true)

    return () => {
      document.removeEventListener("pointerdown", handlePointerDown, true)
      document.removeEventListener("focusin", handleFocusIn, true)
      window.removeEventListener("keydown", handleKeyDown, true)
    }
  }, [isCalendarOpen, isLabelMenuOpen])

  async function submit(nextCompleted = isCompleted) {
    const currentCard = card

    if (isReadOnly || !currentCard) {
      return
    }

    if (!title.trim()) {
      setError("Card title is required.")
      return
    }

    if (dueDate && dueDate < todayDateKey()) {
      setError("Due date cannot be in the past.")
      return
    }

    const submittedLabels = normalizeLabels([...labels, labelInput]).slice(0, 8)
    setLabels(submittedLabels)
    setLabelInput("")
    setError(null)
    setSaveState("saving")
    setIsCompleted(nextCompleted)

    try {
      await onSave(currentCard, {
        title,
        description,
        priority,
        dueDate: dueDate || null,
        labels: submittedLabels,
        assigneeIds,
        isCompleted: nextCompleted,
      })
      setSaveState("saved")
      window.setTimeout(() => setSaveState("idle"), 1400)
    } catch {
      setIsCompleted(currentCard.isCompleted)
      setSaveState("idle")
    }
  }

  async function addComment() {
    const currentCard = card

    if (isReadOnly || !currentCard || !commentInput.trim()) {
      return
    }

    setError(null)

    try {
      await onAddComment(currentCard, commentInput)
      setCommentInput("")
    } catch {
      setError("Comment could not be added.")
    }
  }

  async function addSubtask() {
    const currentCard = card

    if (isReadOnly || !currentCard || !subtaskInput.trim()) {
      return
    }

    setError(null)

    try {
      await onCreateSubtask(currentCard, subtaskInput)
      setSubtaskInput("")
    } catch {
      setError("Subtask could not be added.")
    }
  }

  async function addDependency() {
    const currentCard = card

    if (isReadOnly || !currentCard || !dependencyId) {
      return
    }

    setError(null)

    try {
      await onAddDependency(currentCard, dependencyId)
      setDependencyId("")
    } catch {
      setError("Dependency could not be added.")
    }
  }

  function addLabel() {
    if (isReadOnly) {
      return
    }

    const next = normalizeLabel(labelInput)

    if (!next) {
      return
    }

    setLabels((current) => normalizeLabels([...current, next]).slice(0, 8))
    setLabelInput("")
    setIsLabelMenuOpen(false)
  }

  function toggleLabel(label: string) {
    if (isReadOnly) {
      return
    }

    setLabels((current) => {
      const exists = current.some(
        (candidate) => candidate.toLowerCase() === label.toLowerCase()
      )

      if (exists) {
        return current.filter(
          (candidate) => candidate.toLowerCase() !== label.toLowerCase()
        )
      }

      return normalizeLabels([...current, label]).slice(0, 8)
    })
    setLabelInput("")
    setIsLabelMenuOpen(false)
  }

  return {
    addComment,
    addDependency,
    addLabel,
    addSubtask,
    assigneeIds,
    calendarContainerRef,
    canCreateLabel,
    commentInput,
    dependencyId,
    description,
    dueDate,
    error,
    hasChanges,
    isCalendarOpen,
    isCompleted,
    isLabelMenuOpen,
    labelInput,
    labelMenuContainerRef,
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
