#ifndef COORDINA_GRAPH_ENGINE_H
#define COORDINA_GRAPH_ENGINE_H

#include <stddef.h>
#include <stdint.h>

#if defined(_WIN32)
#define COORDINA_GRAPH_EXPORT __declspec(dllexport)
#else
#define COORDINA_GRAPH_EXPORT __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef struct coordina_graph_node_id
{
  uint64_t high;
  uint64_t low;
} coordina_graph_node_id;

typedef struct coordina_graph_edge
{
  coordina_graph_node_id source;
  coordina_graph_node_id target;
} coordina_graph_edge;

typedef struct coordina_graph_node_analysis
{
  coordina_graph_node_id node;
  uint32_t dependency_count;
  uint32_t dependent_count;
  uint32_t dependency_depth;
  uint32_t dependent_depth;
  uint32_t transitive_dependent_count;
} coordina_graph_node_analysis;

typedef struct coordina_graph_work_item
{
  coordina_graph_node_id node;
  uint32_t is_completed;
  uint32_t priority;
  int32_t due_days;
  uint32_t subtask_total;
  uint32_t subtask_completed;
  uint32_t position;
  uint32_t estimated_minutes;
  uint32_t assignment;
  uint32_t is_in_progress;
  uint32_t is_context_match;
} coordina_graph_work_item;

typedef enum coordina_graph_assignment
{
  COORDINA_GRAPH_UNASSIGNED = 0,
  COORDINA_GRAPH_ASSIGNED_TO_REQUESTER = 1,
  COORDINA_GRAPH_ASSIGNED_TO_OTHERS = 2
} coordina_graph_assignment;

typedef struct coordina_graph_planning_options
{
  uint32_t available_minutes;
  uint32_t prefer_assigned;
  uint32_t include_unassigned;
} coordina_graph_planning_options;

typedef struct coordina_graph_work_recommendation
{
  coordina_graph_node_id node;
  uint32_t score;
  uint32_t blocker_count;
  uint32_t unlock_score;
  uint32_t urgency_score;
  uint32_t priority_score;
  uint32_t progress_score;
  uint32_t is_critical_path;
  int32_t effort_score;
  int32_t assignment_score;
  uint32_t momentum_score;
  uint32_t context_score;
  uint32_t fits_available_time;
  uint32_t is_assignment_match;
  uint32_t is_context_match;
  uint32_t is_in_progress;
} coordina_graph_work_recommendation;

typedef enum coordina_graph_edge_status
{
  COORDINA_GRAPH_EDGE_ALLOWED = 0,
  COORDINA_GRAPH_EDGE_MISSING_SOURCE = 1,
  COORDINA_GRAPH_EDGE_MISSING_TARGET = 2,
  COORDINA_GRAPH_EDGE_SELF_REFERENCE = 3,
  COORDINA_GRAPH_EDGE_DUPLICATE = 4,
  COORDINA_GRAPH_EDGE_CYCLE = 5,
  COORDINA_GRAPH_EDGE_INVALID_ARGUMENT = 100
} coordina_graph_edge_status;

typedef enum coordina_graph_status
{
  COORDINA_GRAPH_OK = 0,
  COORDINA_GRAPH_INVALID_ARGUMENT = 1,
  COORDINA_GRAPH_INSUFFICIENT_CAPACITY = 2
} coordina_graph_status;

typedef struct coordina_graph_node_buffer
{
  coordina_graph_node_id* items;
  size_t capacity;
  size_t count;
} coordina_graph_node_buffer;

/*
 * Output of coordina_graph_analyze_board. The caller owns every buffer and sets
 * each capacity. On return every count holds the number of items the engine
 * produced. If any capacity is too small the call returns
 * COORDINA_GRAPH_INSUFFICIENT_CAPACITY and writes no item, so the caller can
 * retry with the reported counts.
 */
typedef struct coordina_graph_board_analysis
{
  coordina_graph_node_buffer ready_nodes;
  coordina_graph_node_buffer dependency_order;
  coordina_graph_node_buffer critical_path;
  coordina_graph_node_analysis* node_analyses;
  size_t node_analysis_capacity;
  size_t node_analysis_count;
  coordina_graph_work_recommendation* recommendations;
  size_t recommendation_capacity;
  size_t recommendation_count;
  uint32_t is_acyclic;
  uint32_t reserved;
} coordina_graph_board_analysis;

COORDINA_GRAPH_EXPORT const char* coordina_graph_version(void);

COORDINA_GRAPH_EXPORT coordina_graph_edge_status coordina_graph_evaluate_edge(
  const coordina_graph_node_id* nodes,
  size_t node_count,
  const coordina_graph_edge* edges,
  size_t edge_count,
  coordina_graph_node_id source,
  coordina_graph_node_id target);

COORDINA_GRAPH_EXPORT int coordina_graph_has_path(
  const coordina_graph_node_id* nodes,
  size_t node_count,
  const coordina_graph_edge* edges,
  size_t edge_count,
  coordina_graph_node_id source,
  coordina_graph_node_id target);

COORDINA_GRAPH_EXPORT size_t coordina_graph_suggest_targets(
  const coordina_graph_node_id* nodes,
  size_t node_count,
  const coordina_graph_edge* edges,
  size_t edge_count,
  coordina_graph_node_id source,
  coordina_graph_node_id* output,
  size_t output_capacity);

COORDINA_GRAPH_EXPORT int coordina_graph_is_acyclic(
  const coordina_graph_node_id* nodes,
  size_t node_count,
  const coordina_graph_edge* edges,
  size_t edge_count);

COORDINA_GRAPH_EXPORT size_t coordina_graph_topological_sort(
  const coordina_graph_node_id* nodes,
  size_t node_count,
  const coordina_graph_edge* edges,
  size_t edge_count,
  coordina_graph_node_id* output,
  size_t output_capacity);

COORDINA_GRAPH_EXPORT size_t coordina_graph_ready_nodes(
  const coordina_graph_node_id* nodes,
  size_t node_count,
  const coordina_graph_edge* edges,
  size_t edge_count,
  coordina_graph_node_id* output,
  size_t output_capacity);

COORDINA_GRAPH_EXPORT size_t coordina_graph_impacted_dependents(
  const coordina_graph_node_id* nodes,
  size_t node_count,
  const coordina_graph_edge* edges,
  size_t edge_count,
  coordina_graph_node_id dependency,
  coordina_graph_node_id* output,
  size_t output_capacity);

COORDINA_GRAPH_EXPORT size_t coordina_graph_analyze_nodes(
  const coordina_graph_node_id* nodes,
  size_t node_count,
  const coordina_graph_edge* edges,
  size_t edge_count,
  coordina_graph_node_analysis* output,
  size_t output_capacity);

COORDINA_GRAPH_EXPORT size_t coordina_graph_critical_path(
  const coordina_graph_node_id* nodes,
  size_t node_count,
  const coordina_graph_edge* edges,
  size_t edge_count,
  coordina_graph_node_id* output,
  size_t output_capacity);

COORDINA_GRAPH_EXPORT size_t coordina_graph_plan_work(
  const coordina_graph_node_id* nodes,
  size_t node_count,
  const coordina_graph_edge* edges,
  size_t edge_count,
  const coordina_graph_work_item* work_items,
  size_t work_item_count,
  const coordina_graph_planning_options* options,
  coordina_graph_work_recommendation* output,
  size_t output_capacity);

/*
 * Builds the graph once and returns everything the board planning view needs:
 * acyclicity, ready nodes, dependency order, critical path, node analyses, and
 * work recommendations. Results match the individual functions above.
 */
COORDINA_GRAPH_EXPORT coordina_graph_status coordina_graph_analyze_board(
  const coordina_graph_node_id* nodes,
  size_t node_count,
  const coordina_graph_edge* edges,
  size_t edge_count,
  const coordina_graph_work_item* work_items,
  size_t work_item_count,
  const coordina_graph_planning_options* options,
  coordina_graph_board_analysis* output);

#ifdef __cplusplus
}
#endif

#endif
