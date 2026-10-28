#include "graph_types.h"

#include <functional>

namespace coordina::graph
{
std::size_t NodeHash::operator()(const NodeId& value) const noexcept
{
  const auto high = std::hash<std::uint64_t>{}(value.high);
  const auto low = std::hash<std::uint64_t>{}(value.low);
  return high ^ (low + 0x9e3779b97f4a7c15ULL + (high << 6U) + (high >> 2U));
}

std::size_t EdgeHash::operator()(const Edge& value) const noexcept
{
  const auto source = NodeHash{}(value.source);
  const auto target = NodeHash{}(value.target);
  return source ^ (target + 0x9e3779b97f4a7c15ULL + (source << 6U) + (source >> 2U));
}

bool operator==(const NodeId& left, const NodeId& right) noexcept
{
  return left.high == right.high && left.low == right.low;
}

bool operator!=(const NodeId& left, const NodeId& right) noexcept
{
  return !(left == right);
}

bool operator==(const Edge& left, const Edge& right) noexcept
{
  return left.source == right.source && left.target == right.target;
}

NodeId to_node_id(coordina_graph_node_id value) noexcept
{
  return NodeId{value.high, value.low};
}

coordina_graph_node_id to_abi_node_id(NodeId value) noexcept
{
  return coordina_graph_node_id{value.high, value.low};
}
}
