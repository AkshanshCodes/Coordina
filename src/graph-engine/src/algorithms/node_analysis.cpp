#include "algorithms/node_analysis.h"

#include "algorithms/graph_traversal.h"
#include "algorithms/topological_order.h"

#include <algorithm>
#include <bit>
#include <cstdint>
#include <unordered_map>

namespace coordina::graph
{
namespace
{
constexpr std::size_t max_bitset_nodes = 16384U;
constexpr std::size_t bits_per_word = 64U;

// Counts every node that transitively depends on each node. Dependents always
// come after their dependencies in `ordered`, so walking it backwards lets each
// node merge the already complete sets of its direct dependents.
std::unordered_map<NodeId, std::size_t, NodeHash> transitive_dependent_counts(
  const GraphSnapshot& graph,
  const std::vector<NodeId>& ordered)
{
  std::unordered_map<NodeId, std::size_t, NodeHash> counts;
  counts.reserve(ordered.size());

  if (ordered.size() > max_bitset_nodes)
  {
    for (const auto node : ordered)
    {
      counts.emplace(node, reachable_dependents(graph, node).size());
    }

    return counts;
  }

  std::unordered_map<NodeId, std::size_t, NodeHash> index_of;
  index_of.reserve(ordered.size());

  for (std::size_t index = 0U; index < ordered.size(); ++index)
  {
    index_of.emplace(ordered[index], index);
  }

  const auto words = (ordered.size() + bits_per_word - 1U) / bits_per_word;
  std::vector<std::uint64_t> bits(ordered.size() * words, 0U);

  for (auto index = ordered.size(); index-- > 0U;)
  {
    auto* row = bits.data() + index * words;

    for (const auto dependent : graph.incoming(ordered[index]))
    {
      const auto dependent_index = index_of.at(dependent);
      const auto* dependent_row = bits.data() + dependent_index * words;
      row[dependent_index / bits_per_word] |=
        std::uint64_t{1U} << (dependent_index % bits_per_word);

      for (std::size_t word = 0U; word < words; ++word)
      {
        row[word] |= dependent_row[word];
      }
    }

    std::size_t count = 0U;

    for (std::size_t word = 0U; word < words; ++word)
    {
      count += static_cast<std::size_t>(std::popcount(row[word]));
    }

    counts.emplace(ordered[index], count);
  }

  return counts;
}
}
std::vector<NodeAnalysis> analyze_nodes(
  const GraphSnapshot& graph)
{
  return analyze_nodes(graph, dependency_first_topological_order(graph));
}

std::vector<NodeAnalysis> analyze_nodes(
  const GraphSnapshot& graph,
  const std::vector<NodeId>& ordered)
{
  if (ordered.size() != graph.nodes().size())
  {
    return {};
  }

  std::unordered_map<NodeId, std::size_t, NodeHash> dependency_depths;
  std::unordered_map<NodeId, std::size_t, NodeHash> dependent_depths;
  dependency_depths.reserve(graph.nodes().size());
  dependent_depths.reserve(graph.nodes().size());

  for (const auto node : graph.nodes())
  {
    dependency_depths.emplace(node, 0U);
    dependent_depths.emplace(node, 0U);
  }

  for (const auto dependency : ordered)
  {
    const auto depth = dependency_depths[dependency];

    for (const auto dependent : graph.incoming(dependency))
    {
      auto& candidate_depth = dependency_depths[dependent];
      candidate_depth = std::max(candidate_depth, depth + 1U);
    }
  }

  for (auto node = ordered.rbegin(); node != ordered.rend(); ++node)
  {
    const auto depth = dependent_depths[*node];

    for (const auto dependency : graph.outgoing(*node))
    {
      auto& candidate_depth = dependent_depths[dependency];
      candidate_depth = std::max(candidate_depth, depth + 1U);
    }
  }

  const auto transitive_counts = transitive_dependent_counts(graph, ordered);
  std::vector<NodeAnalysis> analysis;
  analysis.reserve(graph.nodes().size());

  for (const auto node : graph.nodes())
  {
    analysis.push_back(NodeAnalysis{
      node,
      graph.dependency_count(node),
      graph.dependent_count(node),
      dependency_depths[node],
      dependent_depths[node],
      transitive_counts.at(node)
    });
  }

  return analysis;
}
}
