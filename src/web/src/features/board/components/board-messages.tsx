import { Kanban } from "@phosphor-icons/react"

import { TemplateChooser } from "@/features/board/components/template-chooser"
import type { BoardTemplate } from "@/types/task"

type BoardSetupOnboardingProps = {
  isCreating: boolean
  onCreate: (template: BoardTemplate, customListTitles?: string[]) => void
  projectName: string
}

export function BoardSetupOnboarding({
  isCreating,
  onCreate,
  projectName,
}: BoardSetupOnboardingProps) {
  return (
    <div className="grid gap-3 rounded-md border border-zinc-950/10 bg-zinc-50 p-3 shadow-xs dark:border-white/10 dark:bg-zinc-950/70">
      <div className="dark:bg-white/0.045 rounded-md border border-zinc-950/10 bg-white p-4 dark:border-white/10">
        <div className="flex items-start gap-3">
          <span className="grid size-10 shrink-0 place-items-center rounded-md bg-zinc-950 text-white dark:bg-white dark:text-zinc-950">
            <Kanban className="size-5" weight="bold" />
          </span>
          <div className="min-w-0">
            <p className="text-sm font-semibold">Set up {projectName}</p>
            <p className="mt-1 max-w-2xl text-sm leading-6 text-muted-foreground">
              Pick a starting workflow for this project board. The project
              already exists; this step only creates its first board structure.
            </p>
          </div>
        </div>
      </div>
      <TemplateChooser isCreating={isCreating} onCreate={onCreate} />
    </div>
  )
}

type BoardStateMessageProps = {
  description: string
  title: string
}

export function BoardStateMessage({
  description,
  title,
}: BoardStateMessageProps) {
  return (
    <div className="grid min-h-64 place-items-center rounded-md border border-dashed border-zinc-950/15 bg-white px-4 py-12 text-center dark:border-white/15 dark:bg-white/[0.035]">
      <div className="max-w-sm">
        <h2 className="text-sm font-semibold">{title}</h2>
        <p className="mt-2 text-sm leading-6 text-muted-foreground">
          {description}
        </p>
      </div>
    </div>
  )
}
