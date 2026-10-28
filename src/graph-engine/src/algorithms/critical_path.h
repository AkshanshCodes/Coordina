#ifndef COORDINA_GRAPH_ENGINE_CRITICAL_PATH_H
#define COORDINA_GRAPH_ENGINE_CRITICAL_PATH_H

#include "core/graph_snapshot.h"

#include <vector>

namespace coordina::graph
{
std::vector<NodeId> critical_path(
  const GraphSnapshot& graph);

std::vector<NodeId> critical_path(
  const GraphSnapshot& graph,
  const std::vector<NodeId>& dependency_order);
}

#endif
