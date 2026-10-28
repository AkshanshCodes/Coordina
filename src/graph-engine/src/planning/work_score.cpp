#include "planning/work_score.h"

#include <algorithm>
#include <limits>

namespace coordina::graph
{
namespace
{
constexpr auto no_due_days = std::numeric_limits<std::int32_t>::max();
constexpr std::uint32_t momentum_bonus = 30U;
constexpr std::int32_t requester_bonus = 40;
constexpr std::int32_t not_preferred_penalty = -80;
constexpr std::uint32_t context_bonus = 45U;
constexpr std::int32_t fits_time_bonus = 40;
constexpr std::int32_t too_long_penalty = -60;
}

std::uint32_t urgency_score(std::int32_t due_days) noexcept
{
  if (due_days == no_due_days)
  {
    return 0U;
  }

  if (due_days < 0)
  {
    const auto overdue_days = -static_cast<std::int64_t>(due_days);
    const auto overdue_bonus = static_cast<std::uint32_t>(
      std::min<std::int64_t>(30, overdue_days * 5));
    return 90U + overdue_bonus;
  }

  if (due_days == 0)
  {
    return 75U;
  }

  if (due_days <= 2)
  {
    return 56U;
  }

  if (due_days <= 7)
  {
    return 32U;
  }

  if (due_days <= 14)
  {
    return 12U;
  }

  return 0U;
}

std::uint32_t priority_score(std::uint32_t priority) noexcept
{
  return std::min(priority, 3U) * 22U;
}

std::uint32_t progress_score(
  std::uint32_t total,
  std::uint32_t completed) noexcept
{
  if (total == 0U)
  {
    return 4U;
  }

  const auto capped_completed = std::min(completed, total);
  auto score = static_cast<std::uint32_t>((capped_completed * 18U) / total);

  if (capped_completed > 0U && total - capped_completed <= 1U)
  {
    score += 8U;
  }

  return std::min(score, 26U);
}

std::uint32_t unlock_score(const NodeAnalysis& analysis) noexcept
{
  const auto transitive = analysis.transitive_dependent_count * 34U;
  const auto depth = analysis.dependent_depth * 18U;
  const auto direct = analysis.dependent_count * 12U;
  const auto score = transitive + depth + direct;
  return score > 220U ? 220U : static_cast<std::uint32_t>(score);
}

EffortFit effort_fit(
  std::uint32_t estimated_minutes,
  std::uint32_t available_minutes) noexcept
{
  if (available_minutes == 0U)
  {
    return EffortFit{0, true};
  }

  if (estimated_minutes == 0U)
  {
    return EffortFit{0, false};
  }

  return estimated_minutes <= available_minutes
    ? EffortFit{fits_time_bonus, true}
    : EffortFit{too_long_penalty, false};
}

std::int32_t assignment_score(
  Assignment assignment,
  const PlanningOptions& options) noexcept
{
  if (assignment == Assignment::requester)
  {
    return options.prefer_assigned ? requester_bonus : 0;
  }

  if (assignment == Assignment::others)
  {
    return options.prefer_assigned ? not_preferred_penalty : 0;
  }

  return options.include_unassigned ? 0 : not_preferred_penalty;
}

std::uint32_t clamp_score(std::int64_t score) noexcept
{
  if (score < 1)
  {
    return 1U;
  }

  constexpr auto max_score =
    static_cast<std::int64_t>(std::numeric_limits<std::uint32_t>::max());
  return static_cast<std::uint32_t>(score > max_score ? max_score : score);
}

std::uint32_t momentum_score(bool is_in_progress) noexcept
{
  return is_in_progress ? momentum_bonus : 0U;
}

std::uint32_t context_score(bool is_context_match) noexcept
{
  return is_context_match ? context_bonus : 0U;
}
}
