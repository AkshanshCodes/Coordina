import { CalendarBlank, Plus, Tag, X } from "@phosphor-icons/react"

import { Button } from "@/components/ui/button"
import { Calendar } from "@/components/ui/calendar"
import {
  Field,
  FieldError,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { boardCardPriorities } from "@/features/board/model/board-constants"
import {
  panelLabelClass,
  shortDate,
  todayDateKey,
} from "@/features/board/board-utils"
import type { CardPanelForm } from "@/features/board/hooks/use-card-panel-form"

type CardDetailsFieldsProps = {
  form: CardPanelForm
  isReadOnly: boolean
}

export function CardDetailsFields({
  form,
  isReadOnly,
}: CardDetailsFieldsProps) {
  const {
    addLabel,
    calendarContainerRef,
    canCreateLabel,
    description,
    dueDate,
    error,
    isCalendarOpen,
    isLabelMenuOpen,
    labelInput,
    labelMenuContainerRef,
    labelSuggestions,
    labels,
    normalizedLabelInput,
    priority,
    setDescription,
    setDueDate,
    setIsCalendarOpen,
    setIsLabelMenuOpen,
    setLabelInput,
    setLabels,
    setPriority,
    setTitle,
    title,
    toggleLabel,
  } = form

  return (
    <FieldGroup>
      <Field>
        <FieldLabel htmlFor="card-title">Title</FieldLabel>
        <Input
          id="card-title"
          value={title}
          disabled={isReadOnly}
          onChange={(event) => setTitle(event.target.value)}
          className="h-9 rounded-md bg-white text-sm dark:bg-white/[0.06]"
        />
      </Field>
      <Field>
        <FieldLabel htmlFor="card-description">Description</FieldLabel>
        <Textarea
          id="card-description"
          value={description}
          disabled={isReadOnly}
          onChange={(event) => setDescription(event.target.value)}
          className="min-h-28 rounded-md bg-white text-sm dark:bg-white/[0.06]"
        />
      </Field>
      <div className="grid gap-3 sm:grid-cols-2">
        <Field>
          <FieldLabel>Priority</FieldLabel>
          <div className="flex flex-wrap gap-1.5">
            {boardCardPriorities.map((item) => (
              <Button
                key={item.value || "none"}
                type="button"
                variant={priority === item.value ? "default" : "outline"}
                size="sm"
                className="h-8 rounded-md px-2 text-xs"
                disabled={isReadOnly}
                onClick={() => setPriority(item.value)}
              >
                {item.label}
              </Button>
            ))}
          </div>
        </Field>
        <Field>
          <div className="flex items-center justify-between gap-2">
            <FieldLabel>Due date</FieldLabel>
            {dueDate && !isReadOnly ? (
              <Button
                type="button"
                variant="ghost"
                size="sm"
                className="h-7 rounded-md px-2 text-xs"
                onClick={() => setDueDate("")}
              >
                Clear
              </Button>
            ) : null}
          </div>
          <div ref={calendarContainerRef} className="relative">
            <Button
              type="button"
              variant="outline"
              className="h-9 w-full justify-start rounded-md bg-white px-2 text-sm dark:bg-white/[0.06]"
              disabled={isReadOnly}
              onClick={() => {
                setIsLabelMenuOpen(false)
                setIsCalendarOpen((current) => !current)
              }}
            >
              <CalendarBlank className="size-4" />
              {dueDate ? shortDate(dueDate) : "Pick a due date"}
            </Button>
            {isCalendarOpen ? (
              <Calendar
                value={dueDate}
                minValue={todayDateKey()}
                className="absolute right-0 z-20 mt-1 w-full max-w-[288px] shadow-xl"
                onChange={(date) => {
                  setDueDate(date ?? "")
                  setIsCalendarOpen(false)
                }}
              />
            ) : null}
          </div>
        </Field>
      </div>
      <Field>
        <FieldLabel htmlFor="card-labels">Labels</FieldLabel>
        <div ref={labelMenuContainerRef} className="relative flex gap-2">
          <Input
            id="card-labels"
            value={labelInput}
            disabled={isReadOnly}
            onChange={(event) => setLabelInput(event.target.value)}
            onFocus={() => {
              setIsCalendarOpen(false)
              setIsLabelMenuOpen(true)
            }}
            placeholder="frontend"
            className="h-9 rounded-md bg-white text-sm dark:bg-white/[0.06]"
            onKeyDown={(event) => {
              if (event.key === "Enter" || event.key === ",") {
                event.preventDefault()
                addLabel()
              }
            }}
          />
          <Button
            type="button"
            variant="outline"
            size="icon"
            className="size-9 rounded-md"
            aria-label="Show existing labels"
            disabled={isReadOnly}
            onClick={() => {
              setIsCalendarOpen(false)
              setIsLabelMenuOpen((current) => !current)
            }}
          >
            <Tag className="size-4" />
          </Button>
          {isLabelMenuOpen && !isReadOnly ? (
            <div className="absolute top-10 right-0 left-0 z-30 rounded-md border border-zinc-950/10 bg-white p-1.5 shadow-xl dark:border-white/10 dark:bg-zinc-900">
              {labelSuggestions.length > 0 ? (
                <div className="grid gap-1">
                  {labelSuggestions.map((label) => (
                    <button
                      key={label}
                      type="button"
                      className="flex h-8 items-center justify-between rounded-sm px-2 text-left text-xs hover:bg-zinc-950/5 dark:hover:bg-white/10"
                      onMouseDown={(event) => event.preventDefault()}
                      onClick={() => toggleLabel(label)}
                    >
                      <span
                        className={`rounded-sm px-2 py-0.5 font-semibold ${panelLabelClass(label)}`}
                      >
                        {label}
                      </span>
                      <Plus className="size-3.5 text-muted-foreground" />
                    </button>
                  ))}
                </div>
              ) : null}
              {canCreateLabel ? (
                <button
                  type="button"
                  className="mt-1 flex h-8 w-full items-center gap-2 rounded-sm px-2 text-left text-xs hover:bg-zinc-950/5 dark:hover:bg-white/10"
                  onMouseDown={(event) => event.preventDefault()}
                  onClick={addLabel}
                >
                  <Plus className="size-3.5" />
                  Create "{normalizedLabelInput}"
                </button>
              ) : labelSuggestions.length === 0 ? (
                <div className="px-2 py-2 text-xs text-muted-foreground">
                  No labels yet
                </div>
              ) : null}
            </div>
          ) : null}
        </div>
        {labels.length > 0 ? (
          <div className="flex flex-wrap gap-1.5">
            {labels.map((label) => (
              <button
                key={label}
                type="button"
                className={`rounded-sm px-2 py-1 text-[11px] font-semibold disabled:pointer-events-none ${panelLabelClass(label)}`}
                disabled={isReadOnly}
                onClick={() =>
                  setLabels((current) =>
                    current.filter((candidate) => candidate !== label)
                  )
                }
              >
                {label}
                {!isReadOnly ? <X className="ml-1 inline size-3" /> : null}
              </button>
            ))}
          </div>
        ) : null}
      </Field>
      {error ? <FieldError>{error}</FieldError> : null}
    </FieldGroup>
  )
}
