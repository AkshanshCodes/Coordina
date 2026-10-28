import {
  CheckCircle,
  CircleNotch,
  CopySimple,
  Trash,
} from "@phosphor-icons/react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  formatDateTime,
  initials,
  userLabel,
} from "@/features/board/board-utils"
import { CardCommentsSection } from "@/features/board/components/card-comments-section"
import { CardDependenciesSection } from "@/features/board/components/card-dependencies-section"
import { CardDetailsFields } from "@/features/board/components/card-details-fields"
import { CardSubtasksSection } from "@/features/board/components/card-subtasks-section"
import { useCardPanelForm } from "@/features/board/hooks/use-card-panel-form"
import type {
  BoardCard,
  BoardCardDependencyAnalysis,
  BoardCardInput,
  BoardCardSubtask,
} from "@/types/task"
import type { WorkspaceMember } from "@/types/workspace"

type CardSidePanelProps = {
  availableLabels: string[]
  canDelete: boolean
  card: BoardCard | null
  dependencyAnalysis: BoardCardDependencyAnalysis | null
  dependencyCandidates: BoardCard[]
  isLoadingDependencyAnalysis: boolean
  isReadOnly: boolean
  isSaving: boolean
  members: WorkspaceMember[]
  onAddComment: (card: BoardCard, body: string) => Promise<void>
  onAddDependency: (card: BoardCard, dependsOnCardId: string) => Promise<void>
  onClose: () => void
  onCreateSubtask: (card: BoardCard, title: string) => Promise<void>
  onDelete: (card: BoardCard) => void
  onDeleteDependency: (
    card: BoardCard,
    dependsOnCardId: string
  ) => Promise<void>
  onDeleteSubtask: (card: BoardCard, subtask: BoardCardSubtask) => Promise<void>
  onDuplicate: (card: BoardCard) => void
  onSave: (card: BoardCard, input: BoardCardInput) => Promise<void>
  onUpdateSubtask: (
    card: BoardCard,
    subtask: BoardCardSubtask,
    input: { title?: string; isCompleted?: boolean }
  ) => Promise<void>
}

export function CardSidePanel({
  availableLabels,
  canDelete,
  card,
  dependencyAnalysis,
  dependencyCandidates,
  isLoadingDependencyAnalysis,
  isReadOnly,
  isSaving,
  members,
  onAddComment,
  onAddDependency,
  onClose,
  onCreateSubtask,
  onDelete,
  onDeleteDependency,
  onDeleteSubtask,
  onDuplicate,
  onSave,
  onUpdateSubtask,
}: CardSidePanelProps) {
  const form = useCardPanelForm({
    availableLabels,
    card,
    isReadOnly,
    onAddComment,
    onAddDependency,
    onCreateSubtask,
    onSave,
  })
  const {
    addComment,
    addDependency,
    addSubtask,
    assigneeIds,
    commentInput,
    dependencyId,
    hasChanges,
    isCompleted,
    saveState,
    setAssigneeIds,
    setCommentInput,
    setDependencyId,
    setSubtaskInput,
    submit,
    subtaskInput,
  } = form

  if (!card) {
    return null
  }

  return (
    <Dialog open={card !== null} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="grid h-[calc(100svh-2rem)] max-h-[calc(100svh-2rem)] max-w-5xl grid-rows-[auto_minmax(0,1fr)_auto] gap-0 overflow-hidden p-0">
        <DialogHeader className="min-w-0 border-b border-zinc-950/10 bg-zinc-50 px-5 py-4 dark:border-white/10 dark:bg-white/[0.035]">
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0">
              <DialogTitle className="truncate">{card.title}</DialogTitle>
              <DialogDescription>
                {isReadOnly
                  ? "Read only"
                  : hasChanges
                    ? "Unsaved changes"
                    : "Up to date"}
              </DialogDescription>
            </div>
            <Badge
              className={
                card.isCompleted
                  ? "border-teal-500/20 bg-teal-500/10 text-teal-700 dark:text-teal-200"
                  : "border-zinc-500/20 bg-zinc-500/10 text-zinc-700 dark:text-zinc-200"
              }
            >
              {card.isCompleted ? "COMPLETED" : "OPEN"}
            </Badge>
          </div>
        </DialogHeader>

        <div className="grid min-h-0 overflow-y-auto overscroll-contain lg:grid-cols-[minmax(0,1fr)_320px]">
          <div className="grid gap-4 p-5">
            <CardDetailsFields form={form} isReadOnly={isReadOnly} />
            <CardSubtasksSection
              addSubtask={addSubtask}
              card={card}
              isReadOnly={isReadOnly}
              isSaving={isSaving}
              onDeleteSubtask={onDeleteSubtask}
              onUpdateSubtask={onUpdateSubtask}
              setSubtaskInput={setSubtaskInput}
              subtaskInput={subtaskInput}
            />
            <CardCommentsSection
              addComment={addComment}
              card={card}
              commentInput={commentInput}
              isReadOnly={isReadOnly}
              isSaving={isSaving}
              setCommentInput={setCommentInput}
            />
          </div>

          <aside className="grid content-start gap-4 border-t border-zinc-950/10 bg-zinc-50 p-5 lg:border-t-0 lg:border-l dark:border-white/10 dark:bg-white/[0.035]">
            <section className="grid gap-2">
              <h3 className="text-sm font-semibold">Status</h3>
              <Button
                type="button"
                variant={isCompleted ? "outline" : "default"}
                className="h-9 justify-start rounded-md"
                disabled={isReadOnly || isSaving}
                onClick={() => void submit(!isCompleted)}
              >
                {isSaving ? (
                  <CircleNotch className="size-4 animate-spin" />
                ) : (
                  <CheckCircle className="size-4" weight="fill" />
                )}
                {isCompleted ? "Reopen card" : "Mark completed"}
              </Button>
              {card.completedBy ? (
                <p className="text-xs leading-5 text-muted-foreground">
                  Completed by {userLabel(card.completedBy)}
                  {card.completedAt
                    ? ` on ${formatDateTime(card.completedAt)}`
                    : ""}
                </p>
              ) : null}
            </section>

            <section className="grid gap-2">
              <h3 className="text-sm font-semibold">Assignees</h3>
              <div className="grid gap-1">
                {members.map((member) => {
                  const active = assigneeIds.includes(member.userId)

                  return (
                    <button
                      key={member.userId}
                      type="button"
                      disabled={isReadOnly}
                      className={`flex h-9 items-center gap-2 rounded-md border px-2 text-left text-xs disabled:pointer-events-none disabled:opacity-80 ${
                        active
                          ? "border-teal-500/30 bg-teal-500/10 dark:border-teal-300/35"
                          : "border-zinc-950/10 bg-white dark:border-white/10 dark:bg-white/[0.04]"
                      }`}
                      onClick={() =>
                        setAssigneeIds((current) =>
                          active
                            ? current.filter((id) => id !== member.userId)
                            : [...current, member.userId]
                        )
                      }
                    >
                      <span className="grid size-6 place-items-center rounded-full bg-zinc-950 text-[10px] font-semibold text-white dark:bg-white dark:text-zinc-950">
                        {initials(member.name ?? member.email)}
                      </span>
                      <span className="min-w-0 flex-1 truncate">
                        {member.name ?? member.email ?? "Workspace member"}
                      </span>
                    </button>
                  )
                })}
              </div>
            </section>

            <CardDependenciesSection
              addDependency={addDependency}
              card={card}
              dependencyAnalysis={dependencyAnalysis}
              dependencyCandidates={dependencyCandidates}
              dependencyId={dependencyId}
              isLoadingDependencyAnalysis={isLoadingDependencyAnalysis}
              isReadOnly={isReadOnly}
              isSaving={isSaving}
              onDeleteDependency={onDeleteDependency}
              setDependencyId={setDependencyId}
            />
          </aside>
        </div>

        <DialogFooter className="min-w-0 border-t border-zinc-950/10 bg-zinc-50 p-4 dark:border-white/10 dark:bg-white/[0.035]">
          <div className="mr-auto flex gap-2">
            {canDelete && !isReadOnly ? (
              <Button
                type="button"
                variant="destructive"
                className="h-9 rounded-md px-3"
                disabled={isSaving}
                onClick={() => onDelete(card)}
              >
                <Trash className="size-4" />
                Delete
              </Button>
            ) : null}
            {!isReadOnly ? (
              <Button
                type="button"
                variant="outline"
                className="h-9 rounded-md px-3"
                disabled={isSaving}
                onClick={() => onDuplicate(card)}
              >
                <CopySimple className="size-4" />
                Duplicate
              </Button>
            ) : null}
          </div>
          <Button
            type="button"
            className="h-9 rounded-md px-4"
            disabled={
              isReadOnly || isSaving || saveState === "saving" || !hasChanges
            }
            onClick={() => void submit()}
          >
            {saveState === "saving" || isSaving ? (
              <CircleNotch className="size-4 animate-spin" />
            ) : saveState === "saved" ? (
              <CheckCircle className="size-4" weight="fill" />
            ) : null}
            {saveState === "saved" ? "Saved" : "Save"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
