import { useCallback, useMemo, useState } from "react"

import {
  type BoardFilters,
  type BoardFocus,
  countBoardCards,
  filterBoard,
} from "@/features/board/model/board-selectors"
import type { Board, BoardCardPriority } from "@/types/task"

export function useBoardFilters(
  board: Board | null,
  sessionUserId: string | null
) {
  const [search, setSearch] = useState("")
  const [focus, setFocus] = useState<BoardFocus>("all")
  const [priorityFilter, setPriorityFilter] = useState<
    BoardCardPriority | "ALL"
  >("ALL")
  const [labelFilter, setLabelFilter] = useState<string | "ALL">("ALL")
  const [assigneeFilter, setAssigneeFilter] = useState<string | "ALL">("ALL")

  const boardFilters = useMemo<BoardFilters>(
    () => ({
      assigneeId: assigneeFilter,
      focus,
      label: labelFilter,
      priority: priorityFilter,
      sessionUserId,
    }),
    [assigneeFilter, focus, labelFilter, priorityFilter, sessionUserId]
  )
  const filteredBoard = useMemo(
    () => filterBoard(board, search, boardFilters),
    [board, boardFilters, search]
  )
  const filteredCardCount = useMemo(
    () => countBoardCards(filteredBoard),
    [filteredBoard]
  )
  const activeFilterCount = [
    search.trim(),
    focus !== "all",
    priorityFilter !== "ALL",
    labelFilter !== "ALL",
    assigneeFilter !== "ALL",
  ].filter(Boolean).length

  const clearFilters = useCallback(() => {
    setSearch("")
    setFocus("all")
    setPriorityFilter("ALL")
    setLabelFilter("ALL")
    setAssigneeFilter("ALL")
  }, [])

  return {
    activeFilterCount,
    assigneeFilter,
    clearFilters,
    filteredBoard,
    filteredCardCount,
    focus,
    labelFilter,
    priorityFilter,
    search,
    setAssigneeFilter,
    setFocus,
    setLabelFilter,
    setPriorityFilter,
    setSearch,
  }
}
