#ifndef COORDINA_GRAPH_ENGINE_SUGGESTION_ENGINE_H
#define COORDINA_GRAPH_ENGINE_SUGGESTION_ENGINE_H

#include "core/graph_snapshot.h"

#include <cstddef>
#include <vector>

namespace coordina::graph
{
std::vector<NodeId> suggest_targets(
  const GraphSnapshot& graph,
  NodeId source,
  std::size_t max_suggestions);
}

#endif
