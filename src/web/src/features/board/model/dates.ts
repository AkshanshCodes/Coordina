export function todayDateKey(date = new Date()) {
  const year = date.getFullYear()
  const month = `${date.getMonth() + 1}`.padStart(2, "0")
  const day = `${date.getDate()}`.padStart(2, "0")

  return `${year}-${month}-${day}`
}

export function dateFromKey(value: string) {
  const [year, month, day] = value.split("-").map(Number)

  return new Date(year, (month ?? 1) - 1, day ?? 1)
}

export function daysUntilDateKey(value: string, today = todayDateKey()) {
  const start = dateFromKey(today).getTime()
  const target = dateFromKey(value).getTime()

  return Math.ceil((target - start) / 86_400_000)
}
