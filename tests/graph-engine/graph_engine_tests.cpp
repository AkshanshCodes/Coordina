#include "coordina_graph_engine.h"

#include <cassert>
#include <cstddef>
#include <cstdint>
#include <limits>
#include <vector>

namespace
{
coordina_graph_node_id node(unsigned long long value)
{
  return coordina_graph_node_id{0U, value};
}

coordina_graph_work_item work_item(
  unsigned long long id,
  std::uint32_t position)
{
  coordina_graph_work_item item{};
  item.node = node(id);
  item.due_days = std::numeric_limits<std::int32_t>::max();
  item.position = position;
  return item;
}

coordina_graph_planning_options options(
  std::uint32_t available_minutes,
  std::uint32_t prefer_assigned = 1U,
  std::uint32_t include_unassigned = 1U)
{
  return coordina_graph_planning_options{
    available_minutes,
    prefer_assigned,
    include_unassigned
  };
}

std::vector<coordina_graph_work_recommendation> plan(
  const std::vector<coordina_graph_work_item>& items,
  const coordina_graph_planning_options* planning_options,
  const std::vector<coordina_graph_edge>& edges = {})
{
  std::vector<coordina_graph_node_id> nodes;

  for (const auto& item : items)
  {
    nodes.push_back(item.node);
  }

  for (const auto& edge : edges)
  {
    nodes.push_back(edge.target);
  }

  std::vector<coordina_graph_work_recommendation> output(nodes.size());
  const auto count = coordina_graph_plan_work(
    nodes.data(),
    nodes.size(),
    edges.data(),
    edges.size(),
    items.data(),
    items.size(),
    planning_options,
    output.data(),
    output.size());
  output.resize(count);
  return output;
}

void detects_cycles()
{
  const coordina_graph_node_id nodes[] = {node(1U), node(2U), node(3U)};
  const coordina_graph_edge edges[] = {
    coordina_graph_edge{node(1U), node(2U)},
    coordina_graph_edge{node(2U), node(3U)}
  };

  const auto status = coordina_graph_evaluate_edge(
    nodes,
    3U,
    edges,
    2U,
    node(3U),
    node(1U));

  assert(status == COORDINA_GRAPH_EDGE_CYCLE);
}

void suggests_only_safe_targets()
{
  const coordina_graph_node_id nodes[] = {
    node(1U),
    node(2U),
    node(3U),
    node(4U)
  };
  const coordina_graph_edge edges[] = {
    coordina_graph_edge{node(1U), node(2U)},
    coordina_graph_edge{node(3U), node(1U)}
  };
  coordina_graph_node_id output[4U] = {};

  const auto count = coordina_graph_suggest_targets(
    nodes,
    4U,
    edges,
    2U,
    node(1U),
    output,
    4U);

  assert(count == 1U);
  assert(output[0U].low == 4U);
}

void sorts_dependency_first()
{
  const coordina_graph_node_id nodes[] = {
    node(1U),
    node(2U),
    node(3U),
    node(4U)
  };
  const coordina_graph_edge edges[] = {
    coordina_graph_edge{node(2U), node(1U)},
    coordina_graph_edge{node(3U), node(2U)},
    coordina_graph_edge{node(4U), node(2U)}
  };
  coordina_graph_node_id output[4U] = {};

  const auto count = coordina_graph_topological_sort(
    nodes,
    4U,
    edges,
    3U,
    output,
    4U);

  assert(count == 4U);
  assert(output[0U].low == 1U);
  assert(output[1U].low == 2U);
  assert(output[2U].low == 3U);
  assert(output[3U].low == 4U);
}

void finds_ready_nodes()
{
  const coordina_graph_node_id nodes[] = {
    node(1U),
    node(2U),
    node(3U)
  };
  const coordina_graph_edge edges[] = {
    coordina_graph_edge{node(2U), node(1U)},
    coordina_graph_edge{node(3U), node(1U)}
  };
  coordina_graph_node_id output[3U] = {};

  const auto count = coordina_graph_ready_nodes(
    nodes,
    3U,
    edges,
    2U,
    output,
    3U);

  assert(count == 1U);
  assert(output[0U].low == 1U);
}

void finds_impacted_dependents()
{
  const coordina_graph_node_id nodes[] = {
    node(1U),
    node(2U),
    node(3U),
    node(4U)
  };
  const coordina_graph_edge edges[] = {
    coordina_graph_edge{node(2U), node(1U)},
    coordina_graph_edge{node(3U), node(2U)},
    coordina_graph_edge{node(4U), node(2U)}
  };
  coordina_graph_node_id output[4U] = {};

  const auto count = coordina_graph_impacted_dependents(
    nodes,
    4U,
    edges,
    3U,
    node(1U),
    output,
    4U);

  assert(count == 3U);
  assert(output[0U].low == 2U);
  assert(output[1U].low == 3U);
  assert(output[2U].low == 4U);
}

void analyzes_node_pressure()
{
  const coordina_graph_node_id nodes[] = {
    node(1U),
    node(2U),
    node(3U),
    node(4U)
  };
  const coordina_graph_edge edges[] = {
    coordina_graph_edge{node(2U), node(1U)},
    coordina_graph_edge{node(3U), node(2U)},
    coordina_graph_edge{node(4U), node(2U)}
  };
  coordina_graph_node_analysis output[4U] = {};

  const auto count = coordina_graph_analyze_nodes(
    nodes,
    4U,
    edges,
    3U,
    output,
    4U);

  assert(count == 4U);
  assert(output[0U].node.low == 1U);
  assert(output[0U].dependency_count == 0U);
  assert(output[0U].dependent_count == 1U);
  assert(output[0U].dependency_depth == 0U);
  assert(output[0U].dependent_depth == 2U);
  assert(output[0U].transitive_dependent_count == 3U);
  assert(output[2U].node.low == 3U);
  assert(output[2U].dependency_count == 1U);
  assert(output[2U].dependent_count == 0U);
  assert(output[2U].dependency_depth == 2U);
  assert(output[2U].dependent_depth == 0U);
  assert(output[2U].transitive_dependent_count == 0U);
}

void finds_critical_path()
{
  const coordina_graph_node_id nodes[] = {
    node(1U),
    node(2U),
    node(3U),
    node(4U)
  };
  const coordina_graph_edge edges[] = {
    coordina_graph_edge{node(2U), node(1U)},
    coordina_graph_edge{node(3U), node(2U)},
    coordina_graph_edge{node(4U), node(2U)}
  };
  coordina_graph_node_id output[4U] = {};

  const auto count = coordina_graph_critical_path(
    nodes,
    4U,
    edges,
    3U,
    output,
    4U);

  assert(count == 3U);
  assert(output[0U].low == 1U);
  assert(output[1U].low == 2U);
  assert(output[2U].low == 3U);
}

void has_no_critical_path_without_dependencies()
{
  const coordina_graph_node_id nodes[] = {node(1U), node(2U)};
  coordina_graph_node_id output[2U] = {};

  const auto count = coordina_graph_critical_path(
    nodes,
    2U,
    nullptr,
    0U,
    output,
    2U);

  assert(count == 0U);

  const auto plan_output = plan({work_item(1U, 0U), work_item(2U, 1U)}, nullptr);
  assert(plan_output[0U].is_critical_path == 0U);
  assert(plan_output[1U].is_critical_path == 0U);
}

void ranks_actionable_work_by_impact()
{
  const coordina_graph_node_id nodes[] = {
    node(1U),
    node(2U),
    node(3U),
    node(4U)
  };
  const coordina_graph_edge edges[] = {
    coordina_graph_edge{node(2U), node(1U)},
    coordina_graph_edge{node(3U), node(2U)}
  };
  const coordina_graph_work_item work_items[] = {
    coordina_graph_work_item{node(1U), 0U, 1U, 7, 0U, 0U, 0U, 0U, 0U, 0U, 0U},
    coordina_graph_work_item{node(2U), 0U, 3U, 0, 3U, 1U, 1U, 0U, 0U, 0U, 0U},
    coordina_graph_work_item{node(3U), 0U, 3U, -1, 2U, 0U, 2U, 0U, 0U, 0U, 0U},
    coordina_graph_work_item{node(4U), 0U, 3U, 0, 0U, 0U, 3U, 0U, 0U, 0U, 0U}
  };
  coordina_graph_work_recommendation output[4U] = {};

  const auto count = coordina_graph_plan_work(
    nodes,
    4U,
    edges,
    2U,
    work_items,
    4U,
    nullptr,
    output,
    4U);

  assert(count == 4U);
  assert(output[0U].node.low == 1U);
  assert(output[0U].blocker_count == 0U);
  assert(output[0U].is_critical_path == 1U);
  assert(output[0U].unlock_score > output[1U].unlock_score);
  assert(output[1U].node.low == 4U);
  assert(output[1U].blocker_count == 0U);
  assert(output[2U].blocker_count > 0U);
}

void prefers_work_that_fits_the_focus_block()
{
  auto quick = work_item(1U, 2U);
  quick.estimated_minutes = 30U;
  auto unknown = work_item(2U, 0U);
  auto long_task = work_item(3U, 1U);
  long_task.estimated_minutes = 120U;
  const auto focus = options(60U);

  const auto output = plan({quick, unknown, long_task}, &focus);

  assert(output.size() == 3U);
  assert(output[0U].node.low == 1U);
  assert(output[0U].fits_available_time == 1U);
  assert(output[0U].effort_score > 0);
  assert(output[1U].node.low == 2U);
  assert(output[1U].fits_available_time == 0U);
  assert(output[1U].effort_score == 0);
  assert(output[2U].node.low == 3U);
  assert(output[2U].fits_available_time == 0U);
  assert(output[2U].effort_score < 0);
}

void ignores_effort_without_a_time_limit()
{
  auto quick = work_item(1U, 1U);
  quick.estimated_minutes = 30U;
  auto long_task = work_item(2U, 0U);
  long_task.estimated_minutes = 480U;
  const auto unrestricted = options(0U);

  const auto output = plan({quick, long_task}, &unrestricted);

  assert(output[0U].node.low == 2U);
  assert(output[0U].fits_available_time == 1U);
  assert(output[1U].fits_available_time == 1U);
  assert(output[0U].effort_score == 0);
}

void ranks_requester_work_before_others()
{
  auto mine = work_item(1U, 2U);
  mine.assignment = 1U;
  auto unassigned = work_item(2U, 1U);
  auto theirs = work_item(3U, 0U);
  theirs.assignment = 2U;
  const auto preferred = options(0U, 1U, 1U);
  const auto owned_only = options(0U, 1U, 0U);
  const auto neutral = options(0U, 0U, 1U);

  const auto ranked = plan({mine, unassigned, theirs}, &preferred);
  assert(ranked[0U].node.low == 1U);
  assert(ranked[0U].is_assignment_match == 1U);
  assert(ranked[0U].assignment_score > 0);
  assert(ranked[1U].node.low == 2U);
  assert(ranked[2U].node.low == 3U);
  assert(ranked[2U].assignment_score < 0);

  const auto strict = plan({mine, unassigned, theirs}, &owned_only);
  assert(strict[0U].node.low == 1U);
  assert(strict[1U].node.low == 3U);
  assert(strict[2U].node.low == 2U);

  const auto by_position = plan({mine, unassigned, theirs}, &neutral);
  assert(by_position[0U].node.low == 3U);
  assert(by_position[2U].node.low == 1U);
}

void finishes_started_work_first()
{
  auto todo = work_item(1U, 0U);
  auto started = work_item(2U, 1U);
  started.is_in_progress = 1U;

  const auto output = plan({todo, started}, nullptr);

  assert(output[0U].node.low == 2U);
  assert(output[0U].is_in_progress == 1U);
  assert(output[0U].momentum_score > 0U);
  assert(output[1U].momentum_score == 0U);
}

void prefers_matching_context()
{
  auto other = work_item(1U, 0U);
  auto matching = work_item(2U, 1U);
  matching.is_context_match = 1U;

  const auto output = plan({other, matching}, nullptr);

  assert(output[0U].node.low == 2U);
  assert(output[0U].is_context_match == 1U);
  assert(output[0U].context_score > 0U);
}

void breaks_full_ties_by_node_id()
{
  const auto output = plan({work_item(9U, 0U), work_item(3U, 0U)}, nullptr);

  assert(output[0U].node.low == 3U);
  assert(output[1U].node.low == 9U);
}

void keeps_preference_order_below_the_score_floor()
{
  auto theirs = work_item(1U, 0U);
  theirs.estimated_minutes = 30U;
  theirs.assignment = 2U;
  auto long_mine = work_item(2U, 1U);
  long_mine.estimated_minutes = 120U;
  long_mine.assignment = 1U;
  const auto focus = options(60U);

  const auto output = plan({theirs, long_mine}, &focus);

  assert(output[0U].score == 1U);
  assert(output[1U].score == 1U);
  assert(output[0U].node.low == 2U);
}

void clamps_heavily_blocked_work()
{
  std::vector<coordina_graph_edge> edges;

  for (unsigned long long id = 100U; id < 5100U; ++id)
  {
    edges.push_back(coordina_graph_edge{node(1U), node(id)});
  }

  const auto output = plan({work_item(1U, 0U), work_item(2U, 1U)}, nullptr, edges);

  assert(output.size() == 2U);
  assert(output[0U].node.low == 2U);
  assert(output[1U].blocker_count == 5000U);
  assert(output[1U].score == 1U);
}
}

int main()
{
  detects_cycles();
  suggests_only_safe_targets();
  sorts_dependency_first();
  finds_ready_nodes();
  finds_impacted_dependents();
  analyzes_node_pressure();
  finds_critical_path();
  has_no_critical_path_without_dependencies();
  ranks_actionable_work_by_impact();
  prefers_work_that_fits_the_focus_block();
  ignores_effort_without_a_time_limit();
  ranks_requester_work_before_others();
  finishes_started_work_first();
  prefers_matching_context();
  breaks_full_ties_by_node_id();
  keeps_preference_order_below_the_score_floor();
  clamps_heavily_blocked_work();

  return 0;
}
