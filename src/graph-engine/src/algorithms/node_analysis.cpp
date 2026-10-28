#include "node_analysis.h"

#include "graph_traversal.h"
#include "planning_engine.h"

#include <algorithm>
#include <unordered_map>

namespace coordina::graph
{
std::vector<NodeAnalysis> analyze_nodes(
  const GraphSnapshot& graph)
{
  const auto ordered = dependency_first_topological_order(graph);

  if (ordered.size() != graph.nodes().size())
  {
    return {};
  }

  std::unordered_map<NodeId, std::size_t, NodeHash> dependency_depths;
  std::unordered_map<NodeId, std::size_t, NodeHash> dependent_depths;
  dependency_depths.reserve(graph.nodes().size());
  dependent_depths.reserve(graph.nodes().size());

  for (const auto node : graph.nodes())
  {
    dependency_depths.emplace(node, 0U);
    dependent_depths.emplace(node, 0U);
  }

  for (const auto dependency : ordered)
  {
    const auto depth = dependency_depths[dependency];

    for (const auto dependent : graph.incoming(dependency))
    {
      auto& candidate_depth = dependency_depths[dependent];
      candidate_depth = std::max(candidate_depth, depth + 1U);
    }
  }

  for (auto node = ordered.rbegin(); node != ordered.rend(); ++node)
  {
    const auto depth = dependent_depths[*node];

    for (const auto dependency : graph.outgoing(*node))
    {
      auto& candidate_depth = dependent_depths[dependency];
      candidate_depth = std::max(candidate_depth, depth + 1U);
    }
  }

  std::vector<NodeAnalysis> analysis;
  analysis.reserve(graph.nodes().size());

  for (const auto node : graph.nodes())
  {
    analysis.push_back(NodeAnalysis{
      node,
      graph.dependency_count(node),
      graph.dependent_count(node),
      dependency_depths[node],
      dependent_depths[node],
      reachable_dependents(graph, node).size()
    });
  }

  return analysis;
}
}
