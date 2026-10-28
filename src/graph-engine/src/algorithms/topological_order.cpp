#include "planning_engine.h"

#include <unordered_map>

namespace coordina::graph
{
std::vector<NodeId> dependency_first_topological_order(
  const GraphSnapshot& graph)
{
  std::unordered_map<NodeId, std::size_t, NodeHash> remaining_dependencies;
  remaining_dependencies.reserve(graph.nodes().size());

  std::vector<NodeId> available;
  available.reserve(graph.nodes().size());

  for (const auto node : graph.nodes())
  {
    const auto count = graph.dependency_count(node);
    remaining_dependencies.emplace(node, count);

    if (count == 0U)
    {
      available.push_back(node);
    }
  }

  std::vector<NodeId> ordered;
  ordered.reserve(graph.nodes().size());
  std::size_t read_index = 0U;

  while (read_index < available.size())
  {
    const auto current = available[read_index];
    ++read_index;
    ordered.push_back(current);

    for (const auto dependent : graph.incoming(current))
    {
      auto found = remaining_dependencies.find(dependent);
      if (found == remaining_dependencies.end() || found->second == 0U)
      {
        continue;
      }

      --found->second;
      if (found->second == 0U)
      {
        available.push_back(dependent);
      }
    }
  }

  return ordered.size() == graph.nodes().size()
    ? ordered
    : std::vector<NodeId>{};
}

std::vector<NodeId> ready_nodes(
  const GraphSnapshot& graph)
{
  std::vector<NodeId> ready;
  ready.reserve(graph.nodes().size());

  for (const auto node : graph.nodes())
  {
    if (graph.dependency_count(node) == 0U)
    {
      ready.push_back(node);
    }
  }

  return ready;
}

bool is_acyclic(const GraphSnapshot& graph)
{
  return dependency_first_topological_order(graph).size() == graph.nodes().size();
}
}
