#ifndef COORDINA_GRAPH_ENGINE_PLANNING_ENGINE_H
#define COORDINA_GRAPH_ENGINE_PLANNING_ENGINE_H

#include "graph_snapshot.h"

#include <vector>

namespace coordina::graph
{
std::vector<NodeId> dependency_first_topological_order(
  const GraphSnapshot& graph);

std::vector<NodeId> ready_nodes(
  const GraphSnapshot& graph);

bool is_acyclic(const GraphSnapshot& graph);
}

#endif
