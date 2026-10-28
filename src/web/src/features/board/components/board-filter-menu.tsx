import { Check, FunnelSimple, X } from "@phosphor-icons/react"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { priorityLabel } from "@/features/board/model/board-constants"
import type { BoardCardPriority } from "@/types/task"
import type { WorkspaceMember } from "@/types/workspace"

type BoardFilterMenuProps = {
  activeFilterCount: number
  assigneeFilter: string | "ALL"
  labelFilter: string | "ALL"
  labels: string[]
  members: WorkspaceMember[]
  priorityFilter: BoardCardPriority | "ALL"
  onAssigneeFilterChange: (value: string | "ALL") => void
  onClear: () => void
  onLabelFilterChange: (value: string | "ALL") => void
  onPriorityFilterChange: (value: BoardCardPriority | "ALL") => void
}

export function BoardFilterMenu({
  activeFilterCount,
  assigneeFilter,
  labelFilter,
  labels,
  members,
  priorityFilter,
  onAssigneeFilterChange,
  onClear,
  onLabelFilterChange,
  onPriorityFilterChange,
}: BoardFilterMenuProps) {
  const priorityOptions: BoardCardPriority[] = ["HIGH", "MEDIUM", "LOW"]

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant={activeFilterCount > 0 ? "default" : "outline"}
          size="sm"
          className="h-8 rounded-md px-2 text-xs"
        >
          <FunnelSimple className="size-3.5" weight="bold" />
          Filters
          {activeFilterCount > 0 ? (
            <span className="rounded-sm bg-white/18 px-1 text-[10px]">
              {activeFilterCount}
            </span>
          ) : null}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent
        align="end"
        className="max-h-96 w-72 overflow-y-auto rounded-md"
      >
        <DropdownMenuLabel>Priority</DropdownMenuLabel>
        <FilterMenuItem
          active={priorityFilter === "ALL"}
          onSelect={() => onPriorityFilterChange("ALL")}
        >
          Any priority
        </FilterMenuItem>
        {priorityOptions.map((priority) => (
          <FilterMenuItem
            key={priority}
            active={priorityFilter === priority}
            onSelect={() => onPriorityFilterChange(priority)}
          >
            {priorityLabel(priority)}
          </FilterMenuItem>
        ))}
        <DropdownMenuSeparator />
        <DropdownMenuLabel>Labels</DropdownMenuLabel>
        <FilterMenuItem
          active={labelFilter === "ALL"}
          onSelect={() => onLabelFilterChange("ALL")}
        >
          Any label
        </FilterMenuItem>
        {labels.length > 0 ? (
          labels.map((label) => (
            <FilterMenuItem
              key={label}
              active={labelFilter === label}
              onSelect={() => onLabelFilterChange(label)}
            >
              {label}
            </FilterMenuItem>
          ))
        ) : (
          <DropdownMenuItem disabled>No labels</DropdownMenuItem>
        )}
        <DropdownMenuSeparator />
        <DropdownMenuLabel>Assignee</DropdownMenuLabel>
        <FilterMenuItem
          active={assigneeFilter === "ALL"}
          onSelect={() => onAssigneeFilterChange("ALL")}
        >
          Any assignee
        </FilterMenuItem>
        {members.map((member) => (
          <FilterMenuItem
            key={member.userId}
            active={assigneeFilter === member.userId}
            onSelect={() => onAssigneeFilterChange(member.userId)}
          >
            {member.name ?? member.email ?? "Workspace member"}
          </FilterMenuItem>
        ))}
        {activeFilterCount > 0 ? (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuItem onSelect={onClear}>
              <X className="size-4" />
              Clear filters
            </DropdownMenuItem>
          </>
        ) : null}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

function FilterMenuItem({
  active,
  children,
  onSelect,
}: {
  active: boolean
  children: React.ReactNode
  onSelect: () => void
}) {
  return (
    <DropdownMenuItem onSelect={onSelect}>
      {active ? (
        <Check className="size-4" weight="bold" />
      ) : (
        <span className="size-4" />
      )}
      <span className="min-w-0 flex-1 truncate">{children}</span>
    </DropdownMenuItem>
  )
}
