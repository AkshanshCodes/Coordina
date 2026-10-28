#ifndef COORDINA_GRAPH_ENGINE_NATIVE_BRIDGE_H
#define COORDINA_GRAPH_ENGINE_NATIVE_BRIDGE_H

#include "coordina_graph_engine.h"
#include "graph_snapshot.h"
#include "node_analysis.h"
#include "work_planner.h"

#include <cstddef>
#include <vector>

namespace coordina::graph
{
bool has_invalid_pointer(
  const coordina_graph_node_id* nodes,
  std::size_t node_count,
  const coordina_graph_edge* edges,
  std::size_t edge_count) noexcept;

GraphSnapshot build_snapshot(
  const coordina_graph_node_id* nodes,
  std::size_t node_count,
  const coordina_graph_edge* edges,
  std::size_t edge_count);

std::size_t write_nodes(
  const std::vector<NodeId>& nodes,
  coordina_graph_node_id* output,
  std::size_t output_capacity);

std::size_t write_node_analyses(
  const std::vector<NodeAnalysis>& analyses,
  coordina_graph_node_analysis* output,
  std::size_t output_capacity);

PlanningOptions build_planning_options(
  const coordina_graph_planning_options* options) noexcept;

std::vector<WorkItem> build_work_items(
  const coordina_graph_work_item* work_items,
  std::size_t work_item_count);

std::size_t write_work_recommendations(
  const std::vector<WorkRecommendation>& recommendations,
  coordina_graph_work_recommendation* output,
  std::size_t output_capacity);
}

#endif
