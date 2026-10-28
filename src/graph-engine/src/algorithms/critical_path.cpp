#include "algorithms/critical_path.h"

#include "algorithms/topological_order.h"

#include <algorithm>
#include <unordered_map>

namespace coordina::graph
{
std::vector<NodeId> critical_path(
  const GraphSnapshot& graph)
{
  const auto ordered = dependency_first_topological_order(graph);

  if (ordered.empty())
  {
    return {};
  }

  if (ordered.size() != graph.nodes().size())
  {
    return {};
  }

  std::unordered_map<NodeId, std::size_t, NodeHash> depths;
  std::unordered_map<NodeId, NodeId, NodeHash> previous;
  depths.reserve(graph.nodes().size());
  previous.reserve(graph.nodes().size());

  for (const auto node : graph.nodes())
  {
    depths.emplace(node, 0U);
  }

  for (const auto dependency : ordered)
  {
    const auto depth = depths[dependency];

    for (const auto dependent : graph.incoming(dependency))
    {
      const auto candidate_depth = depth + 1U;

      if (candidate_depth > depths[dependent])
      {
        depths[dependent] = candidate_depth;
        previous[dependent] = dependency;
      }
    }
  }

  auto end = ordered.front();
  auto longest_depth = depths[end];

  for (const auto node : ordered)
  {
    if (depths[node] > longest_depth)
    {
      end = node;
      longest_depth = depths[node];
    }
  }

  if (longest_depth == 0U)
  {
    return {};
  }

  std::vector<NodeId> path;
  path.push_back(end);

  auto current = end;
  while (true)
  {
    const auto found = previous.find(current);

    if (found == previous.end())
    {
      break;
    }

    current = found->second;
    path.push_back(current);
  }

  std::reverse(path.begin(), path.end());
  return path;
}
}
