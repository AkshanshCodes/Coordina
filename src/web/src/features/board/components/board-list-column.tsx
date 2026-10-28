import {
  CalendarBlank,
  CheckCircle,
  Plus,
  TextAlignLeft,
  WarningCircle,
} from "@phosphor-icons/react"
import { useEffect, useState } from "react"

import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { isBlockedCard } from "@/features/board/model/board-selectors"
import {
  dateBadgeClass,
  initials,
  labelClass,
  priorityBar,
  priorityDot,
  shortDate,
} from "@/features/board/model/board-format"
import { cn } from "@/lib/utils"
import type { BoardCard, BoardList } from "@/types/task"

type BoardListColumnProps = {
  draggingCardId: string | null
  isQuickAdding: boolean
  isReadOnly: boolean
  isSaving: boolean
  list: BoardList
  totalCardCount: number
  onCardClick: (card: BoardCard) => void
  onDragCard: (cardId: string) => void
  onDropCard: (cardId: string) => void
  onQuickAdd: () => void
  onQuickTitleChange: (title: string) => void
  onRename: (title: string) => void
  onToggleQuickAdd: () => void
  quickTitle: string
  wipLimit?: number
}

export function BoardListColumn({
  draggingCardId,
  isQuickAdding,
  isReadOnly,
  isSaving,
  list,
  totalCardCount,
  onCardClick,
  onDragCard,
  onDropCard,
  onQuickAdd,
  onQuickTitleChange,
  onRename,
  onToggleQuickAdd,
  quickTitle,
  wipLimit,
}: BoardListColumnProps) {
  const [title, setTitle] = useState(list.title)
  const [isDragOver, setIsDragOver] = useState(false)
  const hasFilteredCount = totalCardCount !== list.cards.length
  const isOverWipLimit = Boolean(wipLimit && totalCardCount > wipLimit)

  useEffect(() => {
    queueMicrotask(() => setTitle(list.title))
  }, [list.title])

  return (
    <section
      className={cn(
        "flex w-72 shrink-0 flex-col rounded-md border border-zinc-950/10 bg-zinc-100/80 shadow-xs transition-shadow dark:border-white/10 dark:bg-zinc-900/70",
        isDragOver ? "ring-2 ring-teal-500/50" : "",
        isOverWipLimit ? "border-amber-500/35" : ""
      )}
      onDragOver={(event) => {
        if (isReadOnly) {
          return
        }

        event.preventDefault()
        setIsDragOver(true)
      }}
      onDragLeave={(event) => {
        const relatedTarget = event.relatedTarget

        if (
          !(relatedTarget instanceof Node) ||
          !event.currentTarget.contains(relatedTarget)
        ) {
          setIsDragOver(false)
        }
      }}
      onDrop={(event) => {
        if (isReadOnly) {
          return
        }

        event.preventDefault()
        setIsDragOver(false)
        const cardId =
          event.dataTransfer.getData("text/plain") || draggingCardId

        if (cardId) {
          onDropCard(cardId)
        }
      }}
    >
      <div className="flex h-9 shrink-0 items-center gap-2 border-b border-zinc-950/10 px-2 dark:border-white/10">
        <Input
          value={title}
          disabled={isReadOnly}
          onChange={(event) => setTitle(event.target.value)}
          onBlur={() => onRename(title)}
          onKeyDown={(event) => {
            if (event.key === "Enter") {
              event.currentTarget.blur()
            }
          }}
          className="h-7 border-0 bg-transparent px-1 text-xs font-semibold shadow-none focus-visible:ring-0"
        />
        <span
          className={cn(
            "rounded-sm bg-zinc-950/5 px-1.5 py-0.5 text-[10px] text-muted-foreground dark:bg-white/10",
            isOverWipLimit
              ? "bg-amber-500/10 text-amber-700 dark:text-amber-200"
              : ""
          )}
          title={wipLimit ? `WIP ${totalCardCount}/${wipLimit}` : undefined}
        >
          {hasFilteredCount
            ? `${list.cards.length}/${totalCardCount}`
            : totalCardCount}
          {wipLimit && !hasFilteredCount ? `/${wipLimit}` : ""}
        </span>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="size-7 rounded-md"
          disabled={isReadOnly}
          onClick={onToggleQuickAdd}
        >
          <Plus className="size-3.5" />
        </Button>
      </div>
      <div className="min-h-0 flex-1 space-y-1.5 overflow-y-auto p-1.5">
        {isQuickAdding && !isReadOnly ? (
          <div className="rounded-md border border-zinc-950/10 bg-white p-1.5 shadow-xs dark:border-white/10 dark:bg-zinc-950">
            <Input
              value={quickTitle}
              autoFocus
              placeholder="Card title"
              className="h-8 rounded-md text-xs"
              onChange={(event) => onQuickTitleChange(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === "Enter") {
                  onQuickAdd()
                }
              }}
            />
            <Button
              type="button"
              size="sm"
              className="mt-1.5 h-7 rounded-md px-2 text-xs"
              disabled={!quickTitle.trim() || isSaving}
              onClick={onQuickAdd}
            >
              Add
            </Button>
          </div>
        ) : null}
        {list.cards.length > 0 ? (
          list.cards.map((card) => (
            <CompactCard
              key={card.id}
              card={card}
              isReadOnly={isReadOnly}
              onClick={() => onCardClick(card)}
              onDragStart={() => onDragCard(card.id)}
            />
          ))
        ) : (
          <div className="rounded-md border border-dashed border-zinc-950/10 bg-white/70 px-3 py-6 text-center text-xs text-muted-foreground dark:border-white/10 dark:bg-white/[0.03]">
            {hasFilteredCount ? "No matching cards" : "No cards"}
          </div>
        )}
      </div>
    </section>
  )
}

function CompactCard({
  card,
  isReadOnly,
  onClick,
  onDragStart,
}: {
  card: BoardCard
  isReadOnly: boolean
  onClick: () => void
  onDragStart: () => void
}) {
  const isBlocked = isBlockedCard(card)

  return (
    <Card
      draggable={!isReadOnly}
      className={`cursor-pointer overflow-hidden rounded-md border-zinc-950/10 bg-white text-zinc-950 shadow-xs transition-colors hover:bg-zinc-50 dark:border-white/10 dark:bg-zinc-950 dark:text-zinc-50 dark:hover:bg-zinc-900 ${
        card.isCompleted ? "opacity-78" : ""
      }`}
      onClick={onClick}
      onDragStart={(event) => {
        if (isReadOnly) {
          event.preventDefault()
          return
        }

        event.dataTransfer.effectAllowed = "move"
        event.dataTransfer.setData("text/plain", card.id)
        onDragStart()
      }}
    >
      <div className={`h-1 ${priorityBar(card.priority)}`} />
      <CardContent className="grid gap-1.5 p-2">
        <div className="flex items-start gap-2">
          <span
            className={`mt-1.5 size-2 shrink-0 rounded-full ${priorityDot(card.priority)}`}
          />
          <p
            className={`min-w-0 flex-1 text-xs leading-5 font-medium break-words ${
              card.isCompleted ? "text-muted-foreground line-through" : ""
            }`}
          >
            {card.title}
          </p>
          {card.isCompleted ? (
            <CheckCircle
              className="mt-0.5 size-4 shrink-0 text-teal-600 dark:text-teal-300"
              weight="fill"
            />
          ) : null}
        </div>
        <div className="flex flex-wrap items-center gap-1.5 text-[10px] text-muted-foreground">
          {card.subtasks.length > 0 ? (
            <span className="rounded-sm bg-teal-500/10 px-1.5 py-0.5 font-semibold text-teal-700 dark:text-teal-200">
              {card.subtasks.filter((subtask) => subtask.isCompleted).length}/
              {card.subtasks.length}
            </span>
          ) : null}
          {card.comments.length > 0 ? (
            <span className="rounded-sm bg-zinc-950/5 px-1.5 py-0.5 font-semibold dark:bg-white/10">
              {card.comments.length} comments
            </span>
          ) : null}
          {card.dependencies.length > 0 ? (
            <span className="rounded-sm bg-amber-500/10 px-1.5 py-0.5 font-semibold text-amber-700 dark:text-amber-200">
              {card.dependencies.length} deps
            </span>
          ) : null}
          {card.priority ? (
            <span className="rounded-sm bg-zinc-950/5 px-1.5 py-0.5 font-semibold dark:bg-white/10">
              {card.priority}
            </span>
          ) : null}
          {card.description ? (
            <span className="inline-flex items-center gap-1 rounded-sm bg-zinc-950/5 px-1.5 py-0.5 dark:bg-white/10">
              <TextAlignLeft className="size-3" />
              Notes
            </span>
          ) : null}
          {isBlocked ? (
            <span className="inline-flex items-center gap-1 rounded-sm bg-rose-500/10 px-1.5 py-0.5 font-semibold text-rose-700 dark:text-rose-200">
              <WarningCircle className="size-3" weight="fill" />
              Blocked
            </span>
          ) : null}
        </div>
        {card.labels.length > 0 ? (
          <div className="flex flex-wrap gap-1">
            {card.labels.slice(0, 3).map((label) => (
              <span
                key={label}
                className={`rounded-sm px-1.5 py-0.5 text-[10px] font-semibold ${labelClass(label)}`}
              >
                {label}
              </span>
            ))}
          </div>
        ) : null}
        <div className="flex items-center justify-between gap-2">
          <div className="flex -space-x-1">
            {card.assignees.slice(0, 4).map((assignee) => (
              <span
                key={assignee.userId}
                className="grid size-5 place-items-center rounded-full border border-white bg-zinc-950 text-[9px] font-semibold text-white dark:border-zinc-950 dark:bg-white dark:text-zinc-950"
                title={assignee.name ?? assignee.email ?? "Member"}
              >
                {initials(assignee.name ?? assignee.email)}
              </span>
            ))}
            {card.assignees.length > 4 ? (
              <span className="grid size-5 place-items-center rounded-full border border-white bg-zinc-100 text-[9px] font-semibold text-zinc-700 dark:border-zinc-950 dark:bg-zinc-800 dark:text-zinc-200">
                +{card.assignees.length - 4}
              </span>
            ) : null}
          </div>
          {card.dueDate ? (
            <span
              className={`flex items-center gap-1 rounded-sm px-1 py-0.5 text-[10px] font-medium ${dateBadgeClass(card.dueDate)}`}
            >
              <CalendarBlank className="size-3" />
              {shortDate(card.dueDate)}
            </span>
          ) : null}
        </div>
      </CardContent>
    </Card>
  )
}
