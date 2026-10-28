import { Plus } from "@phosphor-icons/react"

import { Button } from "@/components/ui/button"
import { Textarea } from "@/components/ui/textarea"
import {
  formatDateTime,
  initials,
  userLabel,
} from "@/features/board/board-utils"
import type { BoardCard } from "@/types/task"

type CardCommentsSectionProps = {
  addComment: () => Promise<void>
  card: BoardCard
  commentInput: string
  isReadOnly: boolean
  isSaving: boolean
  setCommentInput: (value: string) => void
}

export function CardCommentsSection({
  addComment,
  card,
  commentInput,
  isReadOnly,
  isSaving,
  setCommentInput,
}: CardCommentsSectionProps) {
  return (
    <section className="grid gap-2">
      <h3 className="text-sm font-semibold">Comments</h3>
      <div className="grid gap-2">
        {card.comments.length > 0 ? (
          card.comments.map((comment) => (
            <div
              key={comment.id}
              className="grid gap-1 rounded-md border border-zinc-950/10 bg-white p-3 text-sm dark:border-white/10 dark:bg-zinc-950"
            >
              <div className="flex items-center gap-2 text-xs text-muted-foreground">
                <span className="grid size-6 place-items-center rounded-full bg-zinc-950 text-[10px] font-semibold text-white dark:bg-white dark:text-zinc-950">
                  {initials(comment.author.name ?? comment.author.email)}
                </span>
                <span className="font-medium text-zinc-800 dark:text-zinc-100">
                  {userLabel(comment.author)}
                </span>
                <span>{formatDateTime(comment.createdAt)}</span>
              </div>
              <p className="leading-6 whitespace-pre-wrap">{comment.body}</p>
            </div>
          ))
        ) : (
          <p className="rounded-md border border-dashed border-zinc-950/10 px-3 py-4 text-sm text-muted-foreground dark:border-white/10">
            No comments yet.
          </p>
        )}
      </div>
      {!isReadOnly ? (
        <div className="grid gap-2">
          <Textarea
            value={commentInput}
            onChange={(event) => setCommentInput(event.target.value)}
            placeholder="Write a comment"
            className="min-h-20 rounded-md bg-white text-sm dark:bg-white/[0.06]"
          />
          <Button
            type="button"
            className="h-8 justify-self-start rounded-md px-3 text-xs"
            disabled={!commentInput.trim() || isSaving}
            onClick={() => void addComment()}
          >
            <Plus className="size-3.5" />
            Comment
          </Button>
        </div>
      ) : null}
    </section>
  )
}
