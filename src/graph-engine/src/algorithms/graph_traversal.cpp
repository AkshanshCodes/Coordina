#include "graph_traversal.h"

#include <stack>
#include <unordered_set>

namespace coordina::graph
{
bool has_path(
  const GraphSnapshot& graph,
  NodeId source,
  NodeId target)
{
  if (!graph.contains(source) || !graph.contains(target))
  {
    return false;
  }

  std::unordered_set<NodeId, NodeHash> visited;
  visited.reserve(graph.nodes().size());
  std::stack<NodeId> pending;
  pending.push(source);

  while (!pending.empty())
  {
    const auto current = pending.top();
    pending.pop();

    if (visited.find(current) != visited.end())
    {
      continue;
    }

    visited.insert(current);

    if (current == target)
    {
      return true;
    }

    for (const auto next : graph.outgoing(current))
    {
      pending.push(next);
    }
  }

  return false;
}

std::vector<NodeId> reachable_dependents(
  const GraphSnapshot& graph,
  NodeId dependency)
{
  if (!graph.contains(dependency))
  {
    return {};
  }

  std::vector<NodeId> result;
  std::unordered_set<NodeId, NodeHash> visited;
  visited.reserve(graph.nodes().size());
  std::vector<NodeId> pending = graph.incoming(dependency);
  std::size_t read_index = 0U;

  while (read_index < pending.size())
  {
    const auto current = pending[read_index];
    ++read_index;

    if (!visited.insert(current).second)
    {
      continue;
    }

    result.push_back(current);

    for (const auto dependent : graph.incoming(current))
    {
      pending.push_back(dependent);
    }
  }

  return result;
}
}
