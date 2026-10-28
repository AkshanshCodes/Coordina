#ifndef COORDINA_GRAPH_ENGINE_WORK_SCORE_H
#define COORDINA_GRAPH_ENGINE_WORK_SCORE_H

#include "algorithms/node_analysis.h"
#include "planning/work_planner.h"

#include <cstdint>

namespace coordina::graph
{
struct EffortFit
{
  std::int32_t score;
  bool fits;
};

std::uint32_t urgency_score(std::int32_t due_days) noexcept;

std::uint32_t priority_score(std::uint32_t priority) noexcept;

std::uint32_t progress_score(
  std::uint32_t total,
  std::uint32_t completed) noexcept;

std::uint32_t unlock_score(const NodeAnalysis& analysis) noexcept;

std::uint32_t momentum_score(bool is_in_progress) noexcept;

std::uint32_t context_score(bool is_context_match) noexcept;

EffortFit effort_fit(
  std::uint32_t estimated_minutes,
  std::uint32_t available_minutes) noexcept;

std::int32_t assignment_score(
  Assignment assignment,
  const PlanningOptions& options) noexcept;

std::uint32_t clamp_score(std::int64_t score) noexcept;
}

#endif
