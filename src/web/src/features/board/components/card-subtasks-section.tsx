import { Plus, X } from "@phosphor-icons/react"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { userLabel } from "@/features/board/board-utils"
import type { BoardCard, BoardCardSubtask } from "@/types/task"

type CardSubtasksSectionProps = {
  addSubtask: () => Promise<void>
  card: BoardCard
  isReadOnly: boolean
  isSaving: boolean
  onDeleteSubtask: (card: BoardCard, subtask: BoardCardSubtask) => Promise<void>
  onUpdateSubtask: (
    card: BoardCard,
    subtask: BoardCardSubtask,
    input: { title?: string; isCompleted?: boolean }
  ) => Promise<void>
  setSubtaskInput: (value: string) => void
  subtaskInput: string
}

export function CardSubtasksSection({
  addSubtask,
  card,
  isReadOnly,
  isSaving,
  onDeleteSubtask,
  onUpdateSubtask,
  setSubtaskInput,
  subtaskInput,
}: CardSubtasksSectionProps) {
  const completedSubtasks = card.subtasks.filter(
    (item) => item.isCompleted
  ).length
  const subtaskProgress =
    card.subtasks.length > 0
      ? Math.round((completedSubtasks / card.subtasks.length) * 100)
      : 0

  return (
    <section className="grid gap-2 rounded-md border border-zinc-950/10 bg-zinc-50 p-3 dark:border-white/10 dark:bg-white/[0.035]">
      <div className="flex items-center justify-between gap-2">
        <div>
          <h3 className="text-sm font-semibold">Subtasks</h3>
          <p className="text-xs text-muted-foreground">
            {card.subtasks.length > 0
              ? `${completedSubtasks}/${card.subtasks.length} done`
              : "No subtasks"}
          </p>
        </div>
        {card.subtasks.length > 0 ? (
          <span className="rounded-sm bg-zinc-950/5 px-2 py-1 text-xs font-semibold dark:bg-white/10">
            {subtaskProgress}%
          </span>
        ) : null}
      </div>
      {card.subtasks.length > 0 ? (
        <div className="h-1.5 overflow-hidden rounded-full bg-zinc-200 dark:bg-zinc-800">
          <div
            className="h-full bg-teal-500"
            style={{ width: `${subtaskProgress}%` }}
          />
        </div>
      ) : null}
      <div className="grid gap-1.5">
        {card.subtasks.map((subtask) => (
          <div
            key={subtask.id}
            className="flex items-center gap-2 rounded-md border border-zinc-950/10 bg-white p-2 dark:border-white/10 dark:bg-zinc-950"
          >
            <input
              type="checkbox"
              checked={subtask.isCompleted}
              disabled={isReadOnly || isSaving}
              className="size-4"
              onChange={() =>
                void onUpdateSubtask(card, subtask, {
                  isCompleted: !subtask.isCompleted,
                })
              }
            />
            <Input
              defaultValue={subtask.title}
              disabled={isReadOnly || isSaving}
              className="h-7 flex-1 border-0 bg-transparent px-1 text-xs shadow-none focus-visible:ring-0"
              onBlur={(event) => {
                const next = event.currentTarget.value.trim()
                if (next && next !== subtask.title) {
                  void onUpdateSubtask(card, subtask, { title: next })
                }
              }}
            />
            {subtask.completedBy ? (
              <span className="hidden text-[10px] text-muted-foreground md:inline">
                by {userLabel(subtask.completedBy)}
              </span>
            ) : null}
            {!isReadOnly ? (
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="size-7 rounded-md"
                disabled={isSaving}
                onClick={() => void onDeleteSubtask(card, subtask)}
              >
                <X className="size-3.5" />
              </Button>
            ) : null}
          </div>
        ))}
      </div>
      {!isReadOnly ? (
        <div className="flex gap-2">
          <Input
            value={subtaskInput}
            onChange={(event) => setSubtaskInput(event.target.value)}
            placeholder="Add a subtask"
            className="h-8 rounded-md bg-white text-xs dark:bg-white/[0.06]"
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                void addSubtask()
              }
            }}
          />
          <Button
            type="button"
            size="sm"
            className="h-8 rounded-md px-2"
            disabled={!subtaskInput.trim() || isSaving}
            onClick={() => void addSubtask()}
          >
            <Plus className="size-3.5" />
          </Button>
        </div>
      ) : null}
    </section>
  )
}
