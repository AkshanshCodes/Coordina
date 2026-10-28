export type PlanningPreferences = {
  availableMinutes: number
  context: string | null
}

export const defaultPlanningPreferences: PlanningPreferences = {
  availableMinutes: 0,
  context: null,
}

export const focusTimeOptions = [
  { value: 0, label: "Any time" },
  { value: 30, label: "30m" },
  { value: 60, label: "1h" },
  { value: 120, label: "2h" },
  { value: 240, label: "Half day" },
  { value: 480, label: "Full day" },
] as const

export const effortOptions = [
  { value: null, label: "Unknown" },
  { value: 15, label: "15m" },
  { value: 30, label: "30m" },
  { value: 60, label: "1h" },
  { value: 120, label: "2h" },
  { value: 240, label: "4h" },
  { value: 480, label: "1 day" },
] as const

export function planningQuery(preferences: PlanningPreferences) {
  const params = new URLSearchParams()

  if (preferences.availableMinutes > 0) {
    params.set("availableMinutes", String(preferences.availableMinutes))
  }

  if (preferences.context) {
    params.set("context", preferences.context)
  }

  const query = params.toString()
  return query ? `?${query}` : ""
}

export function formatMinutes(minutes: number) {
  if (minutes < 60) {
    return `${minutes}m`
  }

  const hours = Math.floor(minutes / 60)
  const rest = minutes % 60

  return rest === 0 ? `${hours}h` : `${hours}h ${rest}m`
}
