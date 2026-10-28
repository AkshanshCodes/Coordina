import type { BoardCardPriority, BoardTemplateDefinition } from "@/types/task"

export const boardTemplates: BoardTemplateDefinition[] = [
  {
    id: "BASIC",
    name: "Basic",
    listTitles: ["To Do", "In Progress", "Done"],
    previewCards: ["Clarify scope", "Create first card", "Board created"],
    summary: "Simple workflow for small delivery tracks.",
  },
  {
    id: "AGILE_SCRUM",
    name: "Agile Scrum",
    listTitles: ["Backlog", "Sprint", "In Progress", "Review", "Done"],
    previewCards: [
      "Define user story",
      "Plan sprint commitment",
      "Code review checklist",
    ],
    summary: "Sprint workflow with backlog and review stages.",
  },
  {
    id: "BUG_TRACKING",
    name: "Bug Tracking",
    listTitles: ["Reported", "Investigating", "Fixed", "Released"],
    previewCards: ["Crash on login", "Reproduce issue", "Patch published"],
    summary: "Triage flow for defects and release validation.",
  },
  {
    id: "PRODUCT_ROADMAP",
    name: "Product Roadmap",
    listTitles: ["Ideas", "Planned", "In Progress", "Shipped"],
    previewCards: ["Collect signal", "Write brief", "Measure adoption"],
    summary: "Roadmap pipeline from discovery to launch.",
  },
]

export const boardCardPriorities: Array<{
  value: BoardCardPriority | ""
  label: string
}> = [
  { value: "", label: "None" },
  { value: "LOW", label: "Low" },
  { value: "MEDIUM", label: "Medium" },
  { value: "HIGH", label: "High" },
]

export function priorityLabel(priority: BoardCardPriority) {
  return (
    boardCardPriorities.find((item) => item.value === priority)?.label ??
    priority
  )
}
