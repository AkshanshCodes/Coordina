export function normalizeLabels(labels: string[]) {
  return labels
    .map(normalizeLabel)
    .filter((label): label is string => Boolean(label))
    .filter(
      (label, index, allLabels) =>
        allLabels.findIndex(
          (candidate) => candidate.toLowerCase() === label.toLowerCase()
        ) === index
    )
}

export function normalizeLabel(label: string) {
  const normalized = label.trim().replace(/^#/, "").replace(/\s+/g, "-")
  return normalized ? normalized.slice(0, 32) : null
}

export function sameLabels(left: string[], right: string[]) {
  return sameStrings(
    left.map((label) => label.toLowerCase()).sort(),
    right.map((label) => label.toLowerCase()).sort()
  )
}

export function sameStrings(left: string[], right: string[]) {
  if (left.length !== right.length) {
    return false
  }

  return left.every((value, index) => value === right[index])
}
