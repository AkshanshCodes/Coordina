import {
  ClockCountdown,
  Kanban,
  Target,
  type Icon,
  UsersThree,
  WarningCircle,
} from "@phosphor-icons/react"

import { Button } from "@/components/ui/button"
import type {
  BoardFocus,
  BoardMetrics,
} from "@/features/board/model/board-selectors"

type BoardFocusTabsProps = {
  focus: BoardFocus
  metrics: BoardMetrics
  myCardCount: number
  onFocusChange: (focus: BoardFocus) => void
}

export function BoardFocusTabs({
  focus,
  metrics,
  myCardCount,
  onFocusChange,
}: BoardFocusTabsProps) {
  const riskCount = metrics.risk
  const items: Array<{
    id: BoardFocus
    icon: Icon
    label: string
    value: number
  }> = [
    { id: "all", icon: Kanban, label: "All", value: metrics.total },
    { id: "mine", icon: UsersThree, label: "Mine", value: myCardCount },
    { id: "risk", icon: WarningCircle, label: "At risk", value: riskCount },
    {
      id: "due-soon",
      icon: ClockCountdown,
      label: "Due",
      value: metrics.dueSoon + metrics.overdue,
    },
    {
      id: "unassigned",
      icon: Target,
      label: "Unassigned",
      value: metrics.unassigned,
    },
  ]

  return (
    <div className="flex gap-1 overflow-x-auto">
      {items.map((item) => {
        const Icon = item.icon

        return (
          <Button
            key={item.id}
            type="button"
            variant={focus === item.id ? "default" : "outline"}
            size="sm"
            className="h-8 shrink-0 rounded-md px-2 text-xs"
            aria-pressed={focus === item.id}
            onClick={() => onFocusChange(item.id)}
          >
            <Icon
              className="size-3.5"
              weight={focus === item.id ? "bold" : "regular"}
            />
            {item.label}
            <span className="rounded-sm bg-white/18 px-1 text-[10px] dark:bg-zinc-950/20">
              {item.value}
            </span>
          </Button>
        )
      })}
    </div>
  )
}
