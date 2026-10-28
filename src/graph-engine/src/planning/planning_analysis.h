#ifndef COORDINA_GRAPH_ENGINE_PLANNING_ANALYSIS_H
#define COORDINA_GRAPH_ENGINE_PLANNING_ANALYSIS_H

#include "algorithms/node_analysis.h"
#include "core/graph_snapshot.h"
#include "planning/work_planner.h"

#include <vector>

namespace coordina::graph
{
struct PlanningAnalysis
{
  bool is_acyclic;
  std::vector<NodeId> dependency_order;
  std::vector<NodeId> ready_nodes;
  std::vector<NodeAnalysis> node_analyses;
  std::vector<NodeId> critical_path;
};

struct BoardAnalysis
{
  PlanningAnalysis planning;
  std::vector<WorkRecommendation> recommendations;
};

PlanningAnalysis analyze_planning(const GraphSnapshot& graph);

BoardAnalysis analyze_board(
  const GraphSnapshot& graph,
  const std::vector<WorkItem>& work_items,
  const PlanningOptions& options);
}

#endif
