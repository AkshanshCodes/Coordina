import { describe, expect, it } from "vitest"

import { makeCard } from "@/features/board/model/board-test-fixtures"
import {
  addDraftLabel,
  canCreateDraftLabel,
  draftFromCard,
  hasDraftChanges,
  submittedDraftLabels,
  suggestDraftLabels,
  toggleDraftLabel,
  validateDraft,
} from "@/features/board/model/card-draft"

const card = makeCard({
  title: "API contract",
  description: "Shape the payload",
  priority: "HIGH",
  dueDate: "2026-11-02",
  labels: ["backend", "review"],
  assignees: [
    { userId: "b", name: "B", email: null },
    { userId: "a", name: "A", email: null },
  ],
})

describe("draftFromCard", () => {
  it("copies editable fields and starts with an empty label input", () => {
    expect(draftFromCard(card)).toEqual({
      title: "API contract",
      description: "Shape the payload",
      priority: "HIGH",
      dueDate: "2026-11-02",
      labels: ["backend", "review"],
      labelInput: "",
      assigneeIds: ["b", "a"],
      isCompleted: false,
    })
  })

  it("returns an empty draft without a card", () => {
    expect(draftFromCard(null).title).toBe("")
  })
})

describe("hasDraftChanges", () => {
  it("ignores label case, label order, assignee order, and padding", () => {
    const draft = {
      ...draftFromCard(card),
      title: "  API contract ",
      labels: ["REVIEW", "Backend"],
      assigneeIds: ["a", "b"],
    }

    expect(hasDraftChanges(draft, card)).toBe(false)
  })

  it("counts a pending label input as a change", () => {
    expect(
      hasDraftChanges({ ...draftFromCard(card), labelInput: "#ops" }, card)
    ).toBe(true)
  })

  it("detects completion and due date changes", () => {
    const draft = draftFromCard(card)

    expect(hasDraftChanges({ ...draft, isCompleted: true }, card)).toBe(true)
    expect(hasDraftChanges({ ...draft, dueDate: "" }, card)).toBe(true)
  })
})

describe("validateDraft", () => {
  const draft = draftFromCard(card)

  it("requires a title", () => {
    expect(validateDraft({ ...draft, title: "  " }, "2026-10-28")).toBe(
      "Card title is required."
    )
  })

  it("rejects due dates before the injected day", () => {
    expect(
      validateDraft({ ...draft, dueDate: "2026-10-27" }, "2026-10-28")
    ).toBe("Due date cannot be in the past.")
    expect(validateDraft(draft, "2026-10-28")).toBeNull()
  })
})

describe("labels", () => {
  it("merges the pending input and caps at eight labels", () => {
    const draft = {
      ...draftFromCard(card),
      labels: ["a", "b", "c", "d", "e", "f", "g"],
      labelInput: "h i",
    }

    expect(submittedDraftLabels(draft)).toEqual([
      "a",
      "b",
      "c",
      "d",
      "e",
      "f",
      "g",
      "h-i",
    ])
    expect(addDraftLabel([...draft.labels, "h"], "extra")).toHaveLength(8)
  })

  it("toggles labels case-insensitively", () => {
    expect(toggleDraftLabel(["Backend"], "backend")).toEqual([])
    expect(toggleDraftLabel(["backend"], "ui")).toEqual(["backend", "ui"])
  })

  it("returns null when the input normalizes to nothing", () => {
    expect(addDraftLabel(["backend"], "  # ")).toBeNull()
  })

  it("suggests unselected labels that match the input", () => {
    expect(
      suggestDraftLabels(["backend", "frontend", "ops"], ["ops"], "end")
    ).toEqual(["backend", "frontend"])
  })

  it("allows creating only new labels", () => {
    expect(canCreateDraftLabel(["backend"], [], "Backend")).toBe(false)
    expect(canCreateDraftLabel(["backend"], ["ops"], "ops")).toBe(false)
    expect(canCreateDraftLabel(["backend"], [], "ops")).toBe(true)
    expect(canCreateDraftLabel([], [], " ")).toBe(false)
  })
})
