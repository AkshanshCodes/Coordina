namespace Coordina.Api.Modules.Boards.Application.Graph;

public sealed record GraphBoardAnalysis<TNode>(
  bool IsAcyclic,
  IReadOnlyCollection<TNode> ReadyNodes,
  IReadOnlyCollection<TNode> DependencyOrder,
  IReadOnlyCollection<TNode> CriticalPath,
  IReadOnlyCollection<GraphNodeAnalysis<TNode>> NodeAnalyses,
  IReadOnlyCollection<GraphWorkRecommendation<TNode>> Recommendations)
  where TNode : notnull;
