#ifndef COORDINA_GRAPH_ENGINE_DEPENDENCY_VALIDATOR_H
#define COORDINA_GRAPH_ENGINE_DEPENDENCY_VALIDATOR_H

#include "coordina_graph_engine.h"
#include "graph_snapshot.h"

namespace coordina::graph
{
coordina_graph_edge_status evaluate_edge(
  const GraphSnapshot& graph,
  NodeId source,
  NodeId target);
}

#endif
