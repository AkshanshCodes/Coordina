#include "planning/planning_analysis.h"

#include "algorithms/critical_path.h"
#include "algorithms/topological_order.h"

namespace coordina::graph
{
PlanningAnalysis analyze_planning(const GraphSnapshot& graph)
{
  auto order = dependency_first_topological_order(graph);
  const auto is_acyclic = order.size() == graph.nodes().size();
  auto analyses = analyze_nodes(graph, order);
  auto path = critical_path(graph, order);

  return PlanningAnalysis{
    is_acyclic,
    std::move(order),
    ready_nodes(graph),
    std::move(analyses),
    std::move(path)
  };
}

BoardAnalysis analyze_board(
  const GraphSnapshot& graph,
  const std::vector<WorkItem>& work_items,
  const PlanningOptions& options)
{
  auto planning = analyze_planning(graph);
  auto recommendations = plan_work(graph, work_items, options, planning);

  return BoardAnalysis{std::move(planning), std::move(recommendations)};
}
}
