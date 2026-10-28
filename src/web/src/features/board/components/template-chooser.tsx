import { CircleNotch, Plus } from "@phosphor-icons/react"
import { useMemo, useState } from "react"

import { Button } from "@/components/ui/button"
import { Field, FieldLabel } from "@/components/ui/field"
import { Input } from "@/components/ui/input"
import { boardTemplates } from "@/features/board/model/board-constants"
import {
  BoardTemplateOption,
  BoardTemplatePreview,
} from "@/features/board/components/board-template-preview"
import type { BoardTemplate } from "@/types/task"

type TemplateChooserProps = {
  disabled?: boolean
  isCreating: boolean
  onCreate: (template: BoardTemplate, customListTitles?: string[]) => void
}

export function TemplateChooser({
  disabled = false,
  isCreating,
  onCreate,
}: TemplateChooserProps) {
  const [customLists, setCustomLists] = useState("To Do, Doing, Review, Done")
  const customListTitles = useMemo(
    () =>
      customLists
        .split(",")
        .map((list) => list.trim())
        .filter(Boolean),
    [customLists]
  )
  const isDisabled = disabled || isCreating

  return (
    <div className="grid gap-3 rounded-md border border-zinc-950/10 bg-white p-3 shadow-xs dark:border-white/10 dark:bg-zinc-950">
      <div className="flex items-center justify-between gap-3">
        <div className="min-w-0">
          <div className="text-sm font-semibold">Start this board</div>
          <p className="mt-1 text-xs text-muted-foreground">
            Choose a previewed workflow or define custom list names.
          </p>
        </div>
        {isCreating ? <CircleNotch className="size-4 animate-spin" /> : null}
      </div>
      <div className="grid gap-2 md:grid-cols-2">
        {boardTemplates.map((template) => (
          <BoardTemplateOption
            key={template.id}
            disabled={isDisabled}
            listTitles={template.listTitles}
            name={template.name}
            previewCards={template.previewCards}
            summary={template.summary}
            onSelect={() => onCreate(template.id)}
          />
        ))}
      </div>
      <div className="grid gap-2 rounded-md border border-dashed border-zinc-950/15 bg-zinc-50 p-2 md:grid-cols-[1fr_auto] md:items-end dark:border-white/10 dark:bg-white/[0.035]">
        <Field>
          <FieldLabel htmlFor="custom-lists">Custom board lists</FieldLabel>
          <Input
            id="custom-lists"
            value={customLists}
            disabled={isDisabled}
            onChange={(event) => setCustomLists(event.target.value)}
            className="dark:bg-white/0.06 h-8 rounded-md bg-white text-xs"
          />
        </Field>
        <Button
          type="button"
          size="sm"
          className="h-8 rounded-md px-3 text-xs"
          disabled={isDisabled || customListTitles.length === 0}
          onClick={() => onCreate("CUSTOM", customListTitles)}
        >
          <Plus className="size-3.5" />
          Create custom
        </Button>
        <div className="md:col-span-2">
          <BoardTemplatePreview
            listTitles={customListTitles}
            previewCards={["Your first card", "Team note", "Done item"]}
          />
        </div>
      </div>
    </div>
  )
}
