import { describe, expect, it } from "vitest"

import { makePlanItem } from "@/features/board/model/board-test-fixtures"
import {
  getPlanTags,
  pickPlanFocus,
} from "@/features/board/model/planning-insights"

describe("pickPlanFocus", () => {
  it("skips blocked items and queues the next two ready ones", () => {
    const focus = pickPlanFocus(
      [
        makePlanItem({ id: "blocked", isActionable: false }),
        makePlanItem({ id: "a" }),
        makePlanItem({ id: "b" }),
        makePlanItem({ id: "c" }),
        makePlanItem({ id: "d" }),
      ],
      0
    )

    expect(focus.primary?.card.id).toBe("a")
    expect(focus.queued.map((item) => item.card.id)).toEqual(["b", "c"])
    expect(focus.isNoFitAlternative).toBe(false)
  })

  it("only proposes items that fit the focus block", () => {
    const focus = pickPlanFocus(
      [
        makePlanItem({ id: "long", fitsAvailableTime: false }),
        makePlanItem({ id: "quick", fitsAvailableTime: true }),
      ],
      30
    )

    expect(focus.primary?.card.id).toBe("quick")
    expect(focus.queued).toEqual([])
  })

  it("falls back to the shortest ready alternative", () => {
    const focus = pickPlanFocus(
      [
        makePlanItem({ id: "long", fitsAvailableTime: false }),
        makePlanItem({
          id: "shorter",
          fitsAvailableTime: false,
          isShortestAlternative: true,
        }),
      ],
      30
    )

    expect(focus.primary?.card.id).toBe("shorter")
    expect(focus.isNoFitAlternative).toBe(true)
  })

  it("returns nothing when no ready item fits and no estimate exists", () => {
    const focus = pickPlanFocus(
      [makePlanItem({ id: "unknown", fitsAvailableTime: false })],
      30
    )

    expect(focus.primary).toBeNull()
  })
})

describe("getPlanTags", () => {
  it("builds tags from structured fields", () => {
    const labels = getPlanTags(
      makePlanItem({
        estimatedMinutes: 30,
        fitsAvailableTime: true,
        isAssignedToMe: true,
        isContextMatch: true,
        isInProgress: true,
      }),
      { availableMinutes: 60, context: "backend" }
    ).map((tag) => tag.label)

    expect(labels).toEqual([
      "Ready to start",
      "Fits 1h",
      "Assigned to you",
      "In progress",
      "#backend",
    ])
  })

  it("flags unknown effort only when a focus block is set", () => {
    const item = makePlanItem({ fitsAvailableTime: false })

    expect(
      getPlanTags(item, { availableMinutes: 30, context: null }).map(
        (tag) => tag.label
      )
    ).toContain("Effort unknown")
    expect(
      getPlanTags(item, { availableMinutes: 0, context: null }).map(
        (tag) => tag.label
      )
    ).not.toContain("Effort unknown")
  })
})
