#include "planning/work_planner.h"

#include "algorithms/critical_path.h"
#include "algorithms/node_analysis.h"
#include "planning/work_score.h"

#include <algorithm>
#include <cstdint>
#include <limits>
#include <unordered_map>
#include <unordered_set>

namespace coordina::graph
{
namespace
{
constexpr std::int64_t blocker_penalty = 70;

bool node_less(NodeId left, NodeId right) noexcept
{
  return left.high != right.high
    ? left.high < right.high
    : left.low < right.low;
}

std::uint32_t blocker_count(
  const GraphSnapshot& graph,
  const std::unordered_map<NodeId, WorkItem, NodeHash>& items_by_node,
  NodeId node)
{
  std::uint32_t blockers = 0U;

  for (const auto dependency : graph.outgoing(node))
  {
    const auto found = items_by_node.find(dependency);

    if (found == items_by_node.end() || !found->second.is_completed)
    {
      ++blockers;
    }
  }

  return blockers;
}

std::uint32_t position_for(
  const std::unordered_map<NodeId, WorkItem, NodeHash>& items_by_node,
  NodeId node)
{
  const auto found = items_by_node.find(node);
  return found == items_by_node.end()
    ? std::numeric_limits<std::uint32_t>::max()
    : found->second.position;
}
}

std::vector<WorkRecommendation> plan_work(
  const GraphSnapshot& graph,
  const std::vector<WorkItem>& work_items,
  const PlanningOptions& options)
{
  const auto analyses = analyze_nodes(graph);

  if (analyses.size() != graph.nodes().size())
  {
    return {};
  }

  std::unordered_map<NodeId, NodeAnalysis, NodeHash> analyses_by_node;
  analyses_by_node.reserve(analyses.size());

  for (const auto& analysis : analyses)
  {
    analyses_by_node.emplace(analysis.node, analysis);
  }

  const auto critical = critical_path(graph);
  std::unordered_set<NodeId, NodeHash> critical_nodes;
  critical_nodes.reserve(critical.size());

  for (const auto node : critical)
  {
    critical_nodes.insert(node);
  }

  std::unordered_map<NodeId, WorkItem, NodeHash> items_by_node;
  items_by_node.reserve(work_items.size());

  for (const auto& item : work_items)
  {
    items_by_node.emplace(item.node, item);
  }

  std::vector<WorkRecommendation> recommendations;
  recommendations.reserve(work_items.size());

  for (const auto& item : work_items)
  {
    if (item.is_completed || !graph.contains(item.node))
    {
      continue;
    }

    const auto found_analysis = analyses_by_node.find(item.node);

    if (found_analysis == analyses_by_node.end())
    {
      continue;
    }

    const auto blockers = blocker_count(graph, items_by_node, item.node);
    const auto is_critical = critical_nodes.find(item.node) != critical_nodes.end();
    const auto unlock = unlock_score(found_analysis->second);
    const auto urgency = urgency_score(item.due_days);
    const auto priority = priority_score(item.priority);
    const auto progress = progress_score(
      item.subtask_total,
      item.subtask_completed);
    const auto critical_bonus = is_critical ? 48U : 0U;
    const auto effort = effort_fit(
      item.estimated_minutes,
      options.available_minutes);
    const auto assignment = assignment_score(item.assignment, options);
    const auto momentum = momentum_score(item.is_in_progress);
    const auto context = context_score(item.is_context_match);
    const auto score = static_cast<std::int64_t>(10U)
      + unlock
      + urgency
      + priority
      + progress
      + critical_bonus
      + momentum
      + context
      + effort.score
      + assignment
      - static_cast<std::int64_t>(blockers) * blocker_penalty;

    recommendations.push_back(WorkRecommendation{
      item.node,
      clamp_score(score),
      blockers,
      unlock,
      urgency,
      priority,
      progress,
      is_critical,
      effort.score,
      assignment,
      momentum,
      context,
      effort.fits,
      item.assignment == Assignment::requester,
      item.is_context_match,
      item.is_in_progress,
      score
    });
  }

  std::sort(
    recommendations.begin(),
    recommendations.end(),
    [&items_by_node](const auto& left, const auto& right)
    {
      const auto left_actionable = left.blocker_count == 0U;
      const auto right_actionable = right.blocker_count == 0U;

      if (left_actionable != right_actionable)
      {
        return left_actionable;
      }

      if (left.rank_score != right.rank_score)
      {
        return left.rank_score > right.rank_score;
      }

      if (left.is_critical_path != right.is_critical_path)
      {
        return left.is_critical_path;
      }

      const auto left_position = position_for(items_by_node, left.node);
      const auto right_position = position_for(items_by_node, right.node);

      if (left_position != right_position)
      {
        return left_position < right_position;
      }

      return node_less(left.node, right.node);
    });

  return recommendations;
}
}
