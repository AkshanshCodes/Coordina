#ifndef COORDINA_GRAPH_ENGINE_GRAPH_TRAVERSAL_H
#define COORDINA_GRAPH_ENGINE_GRAPH_TRAVERSAL_H

#include "graph_snapshot.h"

#include <vector>

namespace coordina::graph
{
bool has_path(
  const GraphSnapshot& graph,
  NodeId source,
  NodeId target);

std::vector<NodeId> reachable_dependents(
  const GraphSnapshot& graph,
  NodeId dependency);
}

#endif
