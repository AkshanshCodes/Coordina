#ifndef COORDINA_GRAPH_ENGINE_NODE_ANALYSIS_H
#define COORDINA_GRAPH_ENGINE_NODE_ANALYSIS_H

#include "graph_snapshot.h"

#include <cstddef>
#include <vector>

namespace coordina::graph
{
struct NodeAnalysis
{
  NodeId node;
  std::size_t dependency_count;
  std::size_t dependent_count;
  std::size_t dependency_depth;
  std::size_t dependent_depth;
  std::size_t transitive_dependent_count;
};

std::vector<NodeAnalysis> analyze_nodes(
  const GraphSnapshot& graph);
}

#endif
