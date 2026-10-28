#ifndef COORDINA_GRAPH_ENGINE_GRAPH_SNAPSHOT_H
#define COORDINA_GRAPH_ENGINE_GRAPH_SNAPSHOT_H

#include "graph_types.h"

#include <unordered_map>
#include <unordered_set>
#include <vector>

namespace coordina::graph
{
class GraphSnapshot
{
public:
  GraphSnapshot(
    std::vector<NodeId> nodes,
    std::vector<Edge> edges);

  const std::vector<NodeId>& nodes() const noexcept;
  const std::vector<Edge>& edges() const noexcept;
  const std::vector<NodeId>& outgoing(NodeId node) const;
  const std::vector<NodeId>& incoming(NodeId node) const;

  bool contains(NodeId node) const;
  bool has_edge(NodeId source, NodeId target) const;
  std::size_t dependency_count(NodeId node) const;
  std::size_t dependent_count(NodeId node) const;

private:
  void add_node(NodeId node);
  void add_edge(Edge edge);

  std::vector<NodeId> nodes_;
  std::vector<Edge> edges_;
  std::unordered_map<NodeId, std::vector<NodeId>, NodeHash> outgoing_;
  std::unordered_map<NodeId, std::vector<NodeId>, NodeHash> incoming_;
  std::unordered_set<Edge, EdgeHash> edge_set_;
};
}

#endif
