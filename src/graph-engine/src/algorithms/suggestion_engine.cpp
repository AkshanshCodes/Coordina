#include "suggestion_engine.h"

#include "dependency_validator.h"

namespace coordina::graph
{
std::vector<NodeId> suggest_targets(
  const GraphSnapshot& graph,
  NodeId source,
  std::size_t max_suggestions)
{
  if (max_suggestions == 0U || !graph.contains(source))
  {
    return {};
  }

  std::vector<NodeId> suggestions;
  suggestions.reserve(max_suggestions);

  for (const auto candidate : graph.nodes())
  {
    if (evaluate_edge(graph, source, candidate) != COORDINA_GRAPH_EDGE_ALLOWED)
    {
      continue;
    }

    suggestions.push_back(candidate);

    if (suggestions.size() == max_suggestions)
    {
      break;
    }
  }

  return suggestions;
}
}
