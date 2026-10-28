import {
  CheckCircle,
  CircleNotch,
  FunnelSimple,
  MagnifyingGlass,
  Plus,
  Target,
  X,
} from "@phosphor-icons/react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { isActiveWorkList } from "@/features/board/model/board-selectors"
import { BoardFilterMenu } from "@/features/board/components/board-filter-menu"
import { BoardFocusTabs } from "@/features/board/components/board-focus-tabs"
import { BoardListColumn } from "@/features/board/components/board-list-column"
import {
  BoardSetupOnboarding,
  BoardStateMessage,
} from "@/features/board/components/board-messages"
import { BoardPlanningPanel } from "@/features/board/components/board-planning-panel"
import { BoardSkeleton } from "@/features/board/components/board-skeleton"
import { CardSidePanel } from "@/features/board/components/card-side-panel"
import { useProjectBoard } from "@/features/board/hooks/use-project-board"
import type { Project } from "@/types/project"
import type { Workspace } from "@/types/workspace"

type ProjectBoardProps = {
  project: Project
  workspace: Workspace
}

export function ProjectBoard({ project, workspace }: ProjectBoardProps) {
  const {
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
    planningPreferences,
    quickAddListId,
    quickTitle,
    selectedCard,
    setDraggingCardId,
    setIsAddingList,
    setIsPlanningPanelOpen,
    setNewListTitle,
    setPlanningPreferences,
    setQuickAddListId,
    setQuickTitle,
    setSelectedCardId,
    wipAlerts,
  } = useProjectBoard(project, workspace)
  const {
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
  } = filters

  if (isLoading) {
    return <BoardSkeleton />
  }

  if (!board) {
    return isReadOnly ? (
      <BoardStateMessage
        title="Board unavailable"
        description="Completed projects are read only."
      />
    ) : (
      <BoardSetupOnboarding
        isCreating={isCreatingBoard}
        projectName={project.name}
        onCreate={(template, customListTitles) =>
          void handleCreateBoard(template, customListTitles)
        }
      />
    )
  }

  return (
    <div className="min-h-480px flex h-[calc(100svh-5.75rem)] flex-col overflow-hidden rounded-md border border-zinc-950/10 bg-zinc-50 shadow-xs dark:border-white/10 dark:bg-zinc-950/70">
      <div className="grid shrink-0 gap-2 border-b border-zinc-950/10 bg-white px-2 py-2 dark:border-white/10 dark:bg-zinc-950">
        <div className="flex flex-col gap-2 xl:flex-row xl:items-center">
          <div className="flex min-w-0 flex-1 items-center gap-2">
            <p className="truncate text-sm font-semibold">{project.name}</p>
            <Badge className="h-5 rounded-sm px-1.5 font-mono text-[10px]">
              {board.template}
            </Badge>
            {isReadOnly ? (
              <Badge className="h-5 rounded-sm bg-zinc-950/5 px-1.5 text-[10px] text-muted-foreground dark:bg-white/10">
                READ ONLY
              </Badge>
            ) : null}
            {activeFilterCount > 0 ? (
              <Badge className="h-5 rounded-sm border-sky-500/20 bg-sky-500/10 px-1.5 text-[10px] text-sky-700 dark:text-sky-200">
                {filteredCardCount}/{cardCount}
              </Badge>
            ) : null}
          </div>
          <div className="flex min-w-0 flex-col gap-2 md:flex-row md:items-center">
            <label className="dark:bg-white/0.06 flex h-8 min-w-0 items-center gap-2 rounded-md border border-zinc-950/10 bg-zinc-50 px-2 text-xs md:w-64 dark:border-white/10">
              <MagnifyingGlass className="size-3.5 text-muted-foreground" />
              <Input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Search cards"
                className="h-6 border-0 bg-transparent px-0 text-xs shadow-none focus-visible:ring-0"
              />
            </label>
            <BoardFilterMenu
              activeFilterCount={activeFilterCount}
              assigneeFilter={assigneeFilter}
              labelFilter={labelFilter}
              labels={boardLabels}
              members={members}
              priorityFilter={priorityFilter}
              onAssigneeFilterChange={setAssigneeFilter}
              onClear={clearFilters}
              onLabelFilterChange={setLabelFilter}
              onPriorityFilterChange={setPriorityFilter}
            />
            {canShowPlanningPanel ? (
              <Button
                type="button"
                variant={isPlanningPanelOpen ? "default" : "outline"}
                size="sm"
                className="h-8 rounded-md px-2 text-xs"
                aria-pressed={isPlanningPanelOpen}
                onClick={() => setIsPlanningPanelOpen((isOpen) => !isOpen)}
              >
                <Target
                  className="size-3.5"
                  weight={isPlanningPanelOpen ? "bold" : "regular"}
                />
                Insights
              </Button>
            ) : null}
          </div>
          <div className="flex items-center gap-2">
            <Button
              type="button"
              size="sm"
              className="h-8 rounded-md px-2 text-xs"
              disabled={isReadOnly}
              onClick={() => setIsAddingList(true)}
            >
              <Plus className="size-3.5" weight="bold" />
              List
            </Button>
            {notice ? (
              <div className="hidden h-8 items-center gap-1.5 rounded-md border border-teal-500/20 bg-teal-500/10 px-2 text-[11px] font-medium text-teal-700 sm:flex dark:text-teal-200">
                <CheckCircle className="size-3.5" weight="fill" />
                {notice}
              </div>
            ) : null}
            {isMutating ? (
              <CircleNotch className="size-4 animate-spin text-muted-foreground" />
            ) : null}
          </div>
        </div>
        <BoardFocusTabs
          focus={focus}
          metrics={metrics}
          myCardCount={myCardCount}
          onFocusChange={setFocus}
        />
        {isPlanningPanelVisible ? (
          <BoardPlanningPanel
            graph={boardGraph}
            labels={boardLabels}
            metrics={metrics}
            preferences={planningPreferences}
            onPreferencesChange={setPlanningPreferences}
            onSelectCard={setSelectedCardId}
            wipAlerts={wipAlerts}
          />
        ) : null}
        {activeFilterCount > 0 ? (
          <div className="flex flex-wrap items-center gap-2 rounded-md border border-sky-500/15 bg-sky-500/5 px-2 py-1.5 text-xs text-sky-800 dark:text-sky-100">
            <FunnelSimple className="size-3.5" weight="bold" />
            <span className="font-medium">{filteredCardCount} visible</span>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              className="h-6 rounded-md px-2 text-xs text-sky-800 hover:bg-sky-500/10 dark:text-sky-100"
              onClick={clearFilters}
            >
              <X className="size-3.5" />
              Clear
            </Button>
          </div>
        ) : null}
      </div>

      {error ? (
        <div className="mx-2 mt-2 rounded-md border border-destructive/25 bg-white px-3 py-2 text-xs text-destructive dark:bg-zinc-950">
          {error}
        </div>
      ) : null}

      <div className="flex min-h-0 flex-1 gap-2 overflow-x-auto p-2">
        {filteredBoard?.lists.map((list) => {
          const sourceList = board.lists.find((item) => item.id === list.id)
          const totalCardCount = sourceList?.cards.length ?? list.cards.length
          const wipLimit = isActiveWorkList(list) ? 5 : undefined

          return (
            <BoardListColumn
              key={list.id}
              draggingCardId={draggingCardId}
              isQuickAdding={quickAddListId === list.id}
              isReadOnly={isReadOnly}
              isSaving={isMutating}
              list={list}
              quickTitle={quickTitle}
              totalCardCount={totalCardCount}
              wipLimit={wipLimit}
              onCardClick={(card) => setSelectedCardId(card.id)}
              onDragCard={(cardId) => setDraggingCardId(cardId)}
              onDropCard={(cardId) => void handleMoveCard(cardId, list.id)}
              onQuickAdd={() => void handleQuickAdd(list)}
              onQuickTitleChange={setQuickTitle}
              onRename={(title) => void handleRenameList(list, title)}
              onToggleQuickAdd={() => {
                setQuickAddListId(quickAddListId === list.id ? null : list.id)
                setQuickTitle("")
              }}
            />
          )
        })}
        {!isReadOnly && isAddingList ? (
          <div className="w-72 shrink-0 rounded-md border border-zinc-950/10 bg-white p-2 shadow-xs dark:border-white/10 dark:bg-zinc-950">
            <div className="flex gap-2">
              <Input
                value={newListTitle}
                onChange={(event) => setNewListTitle(event.target.value)}
                placeholder="New list"
                className="h-8 rounded-md text-xs"
                onKeyDown={(event) => {
                  if (event.key === "Enter") {
                    void handleCreateList()
                  }
                }}
              />
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="size-8 rounded-md"
                onClick={() => {
                  setIsAddingList(false)
                  setNewListTitle("")
                }}
              >
                <X className="size-3.5" />
              </Button>
              <Button
                type="button"
                size="sm"
                className="h-8 rounded-md px-2"
                disabled={!newListTitle.trim() || isMutating}
                onClick={() => void handleCreateList()}
              >
                <Plus className="size-3.5" />
              </Button>
            </div>
          </div>
        ) : !isReadOnly ? (
          <button
            type="button"
            className="dark:bg-white/0.04 dark:hover:bg-white/0.07 grid h-12 w-72 shrink-0 place-items-center rounded-md border border-dashed border-zinc-950/15 bg-white/70 text-xs font-semibold text-muted-foreground transition-colors hover:bg-white dark:border-white/15"
            onClick={() => setIsAddingList(true)}
          >
            <span className="inline-flex items-center gap-2">
              <Plus className="size-3.5" />
              Add another list
            </span>
          </button>
        ) : null}
      </div>

      <CardSidePanel
        canDelete={canDeleteCards}
        card={selectedCard}
        dependencyAnalysis={dependencyAnalysis}
        dependencyCandidates={dependencyCandidates}
        isLoadingDependencyAnalysis={isLoadingDependencyAnalysis}
        isReadOnly={isReadOnly}
        isSaving={isMutating}
        members={members}
        availableLabels={boardLabels}
        onAddComment={handleAddComment}
        onAddDependency={handleAddDependency}
        onClose={() => setSelectedCardId(null)}
        onCreateSubtask={handleCreateSubtask}
        onDelete={(card) => void handleDeleteCard(card)}
        onDeleteDependency={handleDeleteDependency}
        onDeleteSubtask={handleDeleteSubtask}
        onDuplicate={(card) => void handleDuplicateCard(card)}
        onSave={handleSaveCard}
        onUpdateSubtask={handleUpdateSubtask}
      />
    </div>
  )
}
