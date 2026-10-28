#ifndef COORDINA_GRAPH_ENGINE_WORK_PLANNER_H
#define COORDINA_GRAPH_ENGINE_WORK_PLANNER_H

#include "core/graph_snapshot.h"

#include <cstddef>
#include <cstdint>
#include <vector>

namespace coordina::graph
{
enum class Assignment : std::uint32_t
{
  unassigned = 0U,
  requester = 1U,
  others = 2U
};

struct WorkItem
{
  NodeId node;
  bool is_completed;
  std::uint32_t priority;
  std::int32_t due_days;
  std::uint32_t subtask_total;
  std::uint32_t subtask_completed;
  std::uint32_t position;
  std::uint32_t estimated_minutes = 0U;
  Assignment assignment = Assignment::unassigned;
  bool is_in_progress = false;
  bool is_context_match = false;
};

struct PlanningOptions
{
  std::uint32_t available_minutes = 0U;
  bool prefer_assigned = true;
  bool include_unassigned = true;
};

struct WorkRecommendation
{
  NodeId node;
  std::uint32_t score;
  std::uint32_t blocker_count;
  std::uint32_t unlock_score;
  std::uint32_t urgency_score;
  std::uint32_t priority_score;
  std::uint32_t progress_score;
  bool is_critical_path;
  std::int32_t effort_score = 0;
  std::int32_t assignment_score = 0;
  std::uint32_t momentum_score = 0U;
  std::uint32_t context_score = 0U;
  bool fits_available_time = true;
  bool is_assignment_match = false;
  bool is_context_match = false;
  bool is_in_progress = false;
  std::int64_t rank_score = 0;
};

std::vector<WorkRecommendation> plan_work(
  const GraphSnapshot& graph,
  const std::vector<WorkItem>& work_items,
  const PlanningOptions& options = {});
}

#endif
