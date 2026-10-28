using Coordina.Api.Modules.Boards.Contracts;
using Coordina.Api.Modules.Boards.Application.Graph;
using Coordina.Api.Modules.Boards.Domain;

namespace Coordina.Api.Modules.Boards.Application;

internal static class BoardGraphResponseMapper
{
  public static BoardGraphResponse ToResponse(BoardGraphAnalysis analysis) =>
    new(
      analysis.BoardId,
      analysis.IsAcyclic,
      analysis.Preferences.AvailableMinutes,
      analysis.Preferences.Context,
      ToCards(analysis.ReadyCards, analysis),
      ToCards(analysis.UnblockedCards, analysis),
      ToCards(analysis.DependencyOrder, analysis),
      ToCards(analysis.CriticalPath, analysis),
      ToCards(analysis.NextCards, analysis),
      ToPlanItems(analysis));

  public static BoardCardDependencyAnalysisResponse ToResponse(
    BoardCardDependencyAnalysis analysis) =>
    new(
      analysis.CardId,
      analysis.IsStructurallyReady,
      analysis.IsUnblocked,
      analysis.IsOnCriticalPath,
      analysis.NodeAnalysis?.DependencyDepth ?? 0,
      analysis.NodeAnalysis?.DependentDepth ?? 0,
      analysis.NodeAnalysis?.TransitiveDependentCount ?? 0,
      ToCards(analysis.BlockingDependencies, analysis),
      ToCards(analysis.SuggestedDependencies, analysis),
      ToCards(analysis.ImpactedDependents, analysis),
      ToCards(analysis.DirectlyUnlockedDependents, analysis));

  private static BoardGraphCardResponse[] ToCards(
    IReadOnlyCollection<ProjectBoardCard> cards,
    BoardGraphAnalysis analysis) =>
    cards
      .Select(card => ToCard(card, analysis.NodeAnalyses, analysis.CriticalPathIds))
      .ToArray();

  private static BoardGraphCardResponse[] ToCards(
    IReadOnlyCollection<ProjectBoardCard> cards,
    BoardCardDependencyAnalysis analysis) =>
    cards
      .Select(card => ToCard(card, analysis.NodeAnalyses, analysis.CriticalPathIds))
      .ToArray();

  private static BoardGraphPlanItemResponse[] ToPlanItems(
    BoardGraphAnalysis analysis) =>
    analysis.PlanItems
      .Select(item => new BoardGraphPlanItemResponse(
        ToCard(item.Card, analysis.NodeAnalyses, analysis.CriticalPathIds),
        item.Score,
        item.IsActionable,
        item.BlockerCount,
        item.Card.EstimatedMinutes,
        item.FitsAvailableTime,
        item.IsAssignedToMe,
        item.IsContextMatch,
        item.IsInProgress,
        item.IsShortestAlternative,
        item.Reasons))
      .ToArray();

  private static BoardGraphCardResponse ToCard(
    ProjectBoardCard card,
    Dictionary<Guid, GraphNodeAnalysis<Guid>> nodeAnalyses,
    HashSet<Guid> criticalPathIds)
  {
    nodeAnalyses.TryGetValue(card.Id, out var nodeAnalysis);

    return new BoardGraphCardResponse(
      card.Id,
      card.ListId,
      card.Title,
      card.IsCompleted,
      nodeAnalysis?.DependencyCount ?? 0,
      nodeAnalysis?.DependentCount ?? 0,
      nodeAnalysis?.DependencyDepth ?? 0,
      nodeAnalysis?.DependentDepth ?? 0,
      nodeAnalysis?.TransitiveDependentCount ?? 0,
      criticalPathIds.Contains(card.Id));
  }
}
