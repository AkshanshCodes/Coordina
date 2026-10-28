namespace Coordina.Api.Modules.Boards.Contracts;

public sealed record BoardGraphResponse(
  Guid BoardId,
  bool IsAcyclic,
  int AvailableMinutes,
  string? Context,
  IReadOnlyCollection<BoardGraphCardResponse> ReadyCards,
  IReadOnlyCollection<BoardGraphCardResponse> UnblockedCards,
  IReadOnlyCollection<BoardGraphCardResponse> DependencyOrder,
  IReadOnlyCollection<BoardGraphCardResponse> CriticalPath,
  IReadOnlyCollection<BoardGraphCardResponse> NextCards,
  IReadOnlyCollection<BoardGraphPlanItemResponse> PlanItems);

public sealed record BoardCardDependencyAnalysisResponse(
  Guid CardId,
  bool IsStructurallyReady,
  bool IsUnblocked,
  bool IsOnCriticalPath,
  int DependencyDepth,
  int DependentDepth,
  int TransitiveDependentCount,
  IReadOnlyCollection<BoardGraphCardResponse> BlockingDependencies,
  IReadOnlyCollection<BoardGraphCardResponse> SuggestedDependencies,
  IReadOnlyCollection<BoardGraphCardResponse> ImpactedDependents,
  IReadOnlyCollection<BoardGraphCardResponse> DirectlyUnlockedDependents);

public sealed record BoardGraphPlanItemResponse(
  BoardGraphCardResponse Card,
  int Score,
  bool IsActionable,
  int BlockerCount,
  int? EstimatedMinutes,
  bool FitsAvailableTime,
  bool IsAssignedToMe,
  bool IsContextMatch,
  bool IsInProgress,
  bool IsShortestAlternative,
  IReadOnlyCollection<string> Reasons);

public sealed record BoardGraphCardResponse(
  Guid Id,
  Guid ListId,
  string Title,
  bool IsCompleted,
  int DependencyCount,
  int DependentCount,
  int DependencyDepth,
  int DependentDepth,
  int TransitiveDependentCount,
  bool IsCriticalPath);
