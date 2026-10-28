#ifndef COORDINA_GRAPH_ENGINE_GRAPH_TYPES_H
#define COORDINA_GRAPH_ENGINE_GRAPH_TYPES_H

#include "coordina_graph_engine.h"

#include <cstddef>
#include <cstdint>

namespace coordina::graph
{
struct NodeId
{
  std::uint64_t high;
  std::uint64_t low;
};

struct Edge
{
  NodeId source;
  NodeId target;
};

struct NodeHash
{
  std::size_t operator()(const NodeId& value) const noexcept;
};

struct EdgeHash
{
  std::size_t operator()(const Edge& value) const noexcept;
};

bool operator==(const NodeId& left, const NodeId& right) noexcept;
bool operator!=(const NodeId& left, const NodeId& right) noexcept;
bool operator==(const Edge& left, const Edge& right) noexcept;

NodeId to_node_id(coordina_graph_node_id value) noexcept;
coordina_graph_node_id to_abi_node_id(NodeId value) noexcept;
}

#endif
