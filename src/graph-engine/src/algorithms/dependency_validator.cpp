#include "dependency_validator.h"

#include "graph_traversal.h"

namespace coordina::graph
{
coordina_graph_edge_status evaluate_edge(
  const GraphSnapshot& graph,
  NodeId source,
  NodeId target)
{
  if (!graph.contains(source))
  {
    return COORDINA_GRAPH_EDGE_MISSING_SOURCE;
  }

  if (!graph.contains(target))
  {
    return COORDINA_GRAPH_EDGE_MISSING_TARGET;
  }

  if (source == target)
  {
    return COORDINA_GRAPH_EDGE_SELF_REFERENCE;
  }

  if (graph.has_edge(source, target))
  {
    return COORDINA_GRAPH_EDGE_DUPLICATE;
  }

  if (has_path(graph, target, source))
  {
    return COORDINA_GRAPH_EDGE_CYCLE;
  }

  return COORDINA_GRAPH_EDGE_ALLOWED;
}
}
