#include "coordina_graph_engine.h"

#include "algorithms/critical_path.h"
#include "algorithms/dependency_validator.h"
#include "algorithms/graph_traversal.h"
#include "interop/native_bridge.h"
#include "planning/planning_analysis.h"
#include "algorithms/node_analysis.h"
#include "algorithms/topological_order.h"
#include "algorithms/suggestion_engine.h"
#include "planning/work_planner.h"

extern "C" const char* coordina_graph_version(void)
{
  return "coordina-graph-engine/0.5.0";
}

extern "C" coordina_graph_edge_status coordina_graph_evaluate_edge(
  const coordina_graph_node_id* nodes,
  std::size_t node_count,
  const coordina_graph_edge* edges,
  std::size_t edge_count,
  coordina_graph_node_id source,
  coordina_graph_node_id target)
{
  if (coordina::graph::has_invalid_pointer(nodes, node_count, edges, edge_count))
  {
    return COORDINA_GRAPH_EDGE_INVALID_ARGUMENT;
  }

  const auto graph = coordina::graph::build_snapshot(
    nodes,
    node_count,
    edges,
    edge_count);

  return coordina::graph::evaluate_edge(
    graph,
    coordina::graph::to_node_id(source),
    coordina::graph::to_node_id(target));
}

extern "C" int coordina_graph_has_path(
  const coordina_graph_node_id* nodes,
  std::size_t node_count,
  const coordina_graph_edge* edges,
  std::size_t edge_count,
  coordina_graph_node_id source,
  coordina_graph_node_id target)
{
  if (coordina::graph::has_invalid_pointer(nodes, node_count, edges, edge_count))
  {
    return -1;
  }

  const auto graph = coordina::graph::build_snapshot(
    nodes,
    node_count,
    edges,
    edge_count);

  return coordina::graph::has_path(
    graph,
    coordina::graph::to_node_id(source),
    coordina::graph::to_node_id(target))
      ? 1
      : 0;
}

extern "C" std::size_t coordina_graph_suggest_targets(
  const coordina_graph_node_id* nodes,
  std::size_t node_count,
  const coordina_graph_edge* edges,
  std::size_t edge_count,
  coordina_graph_node_id source,
  coordina_graph_node_id* output,
  std::size_t output_capacity)
{
  if (coordina::graph::has_invalid_pointer(nodes, node_count, edges, edge_count))
  {
    return 0U;
  }

  const auto graph = coordina::graph::build_snapshot(
    nodes,
    node_count,
    edges,
    edge_count);
  const auto suggestions = coordina::graph::suggest_targets(
    graph,
    coordina::graph::to_node_id(source),
    output_capacity);

  return coordina::graph::write_nodes(suggestions, output, output_capacity);
}

extern "C" int coordina_graph_is_acyclic(
  const coordina_graph_node_id* nodes,
  std::size_t node_count,
  const coordina_graph_edge* edges,
  std::size_t edge_count)
{
  if (coordina::graph::has_invalid_pointer(nodes, node_count, edges, edge_count))
  {
    return -1;
  }

  const auto graph = coordina::graph::build_snapshot(
    nodes,
    node_count,
    edges,
    edge_count);

  return coordina::graph::is_acyclic(graph) ? 1 : 0;
}

extern "C" std::size_t coordina_graph_topological_sort(
  const coordina_graph_node_id* nodes,
  std::size_t node_count,
  const coordina_graph_edge* edges,
  std::size_t edge_count,
  coordina_graph_node_id* output,
  std::size_t output_capacity)
{
  if (coordina::graph::has_invalid_pointer(nodes, node_count, edges, edge_count))
  {
    return 0U;
  }

  const auto graph = coordina::graph::build_snapshot(
    nodes,
    node_count,
    edges,
    edge_count);
  const auto ordered = coordina::graph::dependency_first_topological_order(graph);

  return coordina::graph::write_nodes(ordered, output, output_capacity);
}

extern "C" std::size_t coordina_graph_ready_nodes(
  const coordina_graph_node_id* nodes,
  std::size_t node_count,
  const coordina_graph_edge* edges,
  std::size_t edge_count,
  coordina_graph_node_id* output,
  std::size_t output_capacity)
{
  if (coordina::graph::has_invalid_pointer(nodes, node_count, edges, edge_count))
  {
    return 0U;
  }

  const auto graph = coordina::graph::build_snapshot(
    nodes,
    node_count,
    edges,
    edge_count);
  const auto ready = coordina::graph::ready_nodes(graph);

  return coordina::graph::write_nodes(ready, output, output_capacity);
}

extern "C" std::size_t coordina_graph_impacted_dependents(
  const coordina_graph_node_id* nodes,
  std::size_t node_count,
  const coordina_graph_edge* edges,
  std::size_t edge_count,
  coordina_graph_node_id dependency,
  coordina_graph_node_id* output,
  std::size_t output_capacity)
{
  if (coordina::graph::has_invalid_pointer(nodes, node_count, edges, edge_count))
  {
    return 0U;
  }

  const auto graph = coordina::graph::build_snapshot(
    nodes,
    node_count,
    edges,
    edge_count);
  const auto impacted = coordina::graph::reachable_dependents(
    graph,
    coordina::graph::to_node_id(dependency));

  return coordina::graph::write_nodes(impacted, output, output_capacity);
}

extern "C" std::size_t coordina_graph_analyze_nodes(
  const coordina_graph_node_id* nodes,
  std::size_t node_count,
  const coordina_graph_edge* edges,
  std::size_t edge_count,
  coordina_graph_node_analysis* output,
  std::size_t output_capacity)
{
  if (coordina::graph::has_invalid_pointer(nodes, node_count, edges, edge_count))
  {
    return 0U;
  }

  const auto graph = coordina::graph::build_snapshot(
    nodes,
    node_count,
    edges,
    edge_count);
  const auto analysis = coordina::graph::analyze_nodes(graph);

  return coordina::graph::write_node_analyses(
    analysis,
    output,
    output_capacity);
}

extern "C" std::size_t coordina_graph_critical_path(
  const coordina_graph_node_id* nodes,
  std::size_t node_count,
  const coordina_graph_edge* edges,
  std::size_t edge_count,
  coordina_graph_node_id* output,
  std::size_t output_capacity)
{
  if (coordina::graph::has_invalid_pointer(nodes, node_count, edges, edge_count))
  {
    return 0U;
  }

  const auto graph = coordina::graph::build_snapshot(
    nodes,
    node_count,
    edges,
    edge_count);
  const auto path = coordina::graph::critical_path(graph);

  return coordina::graph::write_nodes(path, output, output_capacity);
}

extern "C" std::size_t coordina_graph_plan_work(
  const coordina_graph_node_id* nodes,
  std::size_t node_count,
  const coordina_graph_edge* edges,
  std::size_t edge_count,
  const coordina_graph_work_item* work_items,
  std::size_t work_item_count,
  const coordina_graph_planning_options* options,
  coordina_graph_work_recommendation* output,
  std::size_t output_capacity)
{
  if (coordina::graph::has_invalid_pointer(nodes, node_count, edges, edge_count)
    || (work_item_count > 0U && work_items == nullptr))
  {
    return 0U;
  }

  const auto graph = coordina::graph::build_snapshot(
    nodes,
    node_count,
    edges,
    edge_count);
  const auto native_work_items = coordina::graph::build_work_items(
    work_items,
    work_item_count);
  const auto recommendations = coordina::graph::plan_work(
    graph,
    native_work_items,
    coordina::graph::build_planning_options(options));

  return coordina::graph::write_work_recommendations(
    recommendations,
    output,
    output_capacity);
}

namespace
{
bool fits(std::size_t required, const void* items, std::size_t capacity) noexcept
{
  return required == 0U || (items != nullptr && capacity >= required);
}
}

extern "C" coordina_graph_status coordina_graph_analyze_board(
  const coordina_graph_node_id* nodes,
  std::size_t node_count,
  const coordina_graph_edge* edges,
  std::size_t edge_count,
  const coordina_graph_work_item* work_items,
  std::size_t work_item_count,
  const coordina_graph_planning_options* options,
  coordina_graph_board_analysis* output)
{
  if (output == nullptr
    || coordina::graph::has_invalid_pointer(nodes, node_count, edges, edge_count)
    || (work_item_count > 0U && work_items == nullptr))
  {
    return COORDINA_GRAPH_INVALID_ARGUMENT;
  }

  const auto graph = coordina::graph::build_snapshot(
    nodes,
    node_count,
    edges,
    edge_count);
  const auto analysis = coordina::graph::analyze_board(
    graph,
    coordina::graph::build_work_items(work_items, work_item_count),
    coordina::graph::build_planning_options(options));
  const auto& planning = analysis.planning;

  output->is_acyclic = planning.is_acyclic ? 1U : 0U;
  output->ready_nodes.count = planning.ready_nodes.size();
  output->dependency_order.count = planning.dependency_order.size();
  output->critical_path.count = planning.critical_path.size();
  output->node_analysis_count = planning.node_analyses.size();
  output->recommendation_count = analysis.recommendations.size();

  if (!fits(output->ready_nodes.count, output->ready_nodes.items, output->ready_nodes.capacity)
    || !fits(output->dependency_order.count, output->dependency_order.items, output->dependency_order.capacity)
    || !fits(output->critical_path.count, output->critical_path.items, output->critical_path.capacity)
    || !fits(output->node_analysis_count, output->node_analyses, output->node_analysis_capacity)
    || !fits(output->recommendation_count, output->recommendations, output->recommendation_capacity))
  {
    return COORDINA_GRAPH_INSUFFICIENT_CAPACITY;
  }

  coordina::graph::write_nodes(
    planning.ready_nodes,
    output->ready_nodes.items,
    output->ready_nodes.capacity);
  coordina::graph::write_nodes(
    planning.dependency_order,
    output->dependency_order.items,
    output->dependency_order.capacity);
  coordina::graph::write_nodes(
    planning.critical_path,
    output->critical_path.items,
    output->critical_path.capacity);
  coordina::graph::write_node_analyses(
    planning.node_analyses,
    output->node_analyses,
    output->node_analysis_capacity);
  coordina::graph::write_work_recommendations(
    analysis.recommendations,
    output->recommendations,
    output->recommendation_capacity);

  return COORDINA_GRAPH_OK;
}
