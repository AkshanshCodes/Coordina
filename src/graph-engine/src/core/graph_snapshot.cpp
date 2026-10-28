#include "graph_snapshot.h"

#include <stdexcept>

namespace coordina::graph
{
namespace
{
const std::vector<NodeId> empty_nodes;
}

GraphSnapshot::GraphSnapshot(
  std::vector<NodeId> nodes,
  std::vector<Edge> edges)
{
  nodes_.reserve(nodes.size());
  outgoing_.reserve(nodes.size());
  incoming_.reserve(nodes.size());
  edges_.reserve(edges.size());
  edge_set_.reserve(edges.size());

  for (const auto node : nodes)
  {
    add_node(node);
  }

  for (const auto edge : edges)
  {
    add_edge(edge);
  }
}

const std::vector<NodeId>& GraphSnapshot::nodes() const noexcept
{
  return nodes_;
}

const std::vector<Edge>& GraphSnapshot::edges() const noexcept
{
  return edges_;
}

const std::vector<NodeId>& GraphSnapshot::outgoing(NodeId node) const
{
  const auto found = outgoing_.find(node);
  return found == outgoing_.end() ? empty_nodes : found->second;
}

const std::vector<NodeId>& GraphSnapshot::incoming(NodeId node) const
{
  const auto found = incoming_.find(node);
  return found == incoming_.end() ? empty_nodes : found->second;
}

bool GraphSnapshot::contains(NodeId node) const
{
  return outgoing_.find(node) != outgoing_.end();
}

bool GraphSnapshot::has_edge(NodeId source, NodeId target) const
{
  return edge_set_.find(Edge{source, target}) != edge_set_.end();
}

std::size_t GraphSnapshot::dependency_count(NodeId node) const
{
  return outgoing(node).size();
}

std::size_t GraphSnapshot::dependent_count(NodeId node) const
{
  return incoming(node).size();
}

void GraphSnapshot::add_node(NodeId node)
{
  if (contains(node))
  {
    return;
  }

  nodes_.push_back(node);
  outgoing_.emplace(node, std::vector<NodeId>{});
  incoming_.emplace(node, std::vector<NodeId>{});
}

void GraphSnapshot::add_edge(Edge edge)
{
  if (!contains(edge.source) || !contains(edge.target))
  {
    return;
  }

  if (edge_set_.find(edge) != edge_set_.end())
  {
    return;
  }

  outgoing_[edge.source].push_back(edge.target);
  incoming_[edge.target].push_back(edge.source);
  edges_.push_back(edge);
  edge_set_.insert(edge);
}
}
