using Coordina.Api.Modules.Boards.Application.Graph;
using Coordina.Api.Modules.Boards.Domain;

namespace Coordina.Api.Modules.Boards.Application;

internal static class BoardDependencyGraph
{
  private const int DefaultSuggestionLimit = 8;
  private const int NextCardLimit = 5;

  public static GraphEdgeEvaluation EvaluateDependency(
    IGraphEngine<Guid> graphEngine,
    ProjectBoard board,
    Guid cardId,
    Guid dependsOnCardId)
  {
    ArgumentNullException.ThrowIfNull(graphEngine);
    ArgumentNullException.ThrowIfNull(board);

    var graph = ToGraph(board);
    return graphEngine.EvaluateEdge(
      graph.Nodes,
      graph.Edges,
      cardId,
      dependsOnCardId);
  }

  public static BoardGraphAnalysis AnalyzeBoard(
    IGraphEngine<Guid> graphEngine,
    ProjectBoard board,
    BoardPlanningPreferences preferences)
  {
    ArgumentNullException.ThrowIfNull(graphEngine);
    ArgumentNullException.ThrowIfNull(board);
    ArgumentNullException.ThrowIfNull(preferences);

    var graph = ToGraph(board);
    var cardsById = graph.Cards.ToDictionary(card => card.Id);
    var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
    var inProgressListIds = board.Lists
      .Where(list => list.TaskStatus == BoardTaskStatus.InProgress)
      .Select(list => list.Id)
      .ToHashSet();
    var analysis = graphEngine.AnalyzeBoard(
      graph.Nodes,
      graph.Edges,
      ToWorkItems(graph.Cards, today, inProgressListIds, preferences),
      new GraphPlanningOptions(
        preferences.AvailableMinutes,
        preferences.PreferAssigned,
        preferences.IncludeUnassigned));
    var nodeAnalyses = analysis.NodeAnalyses
      .ToDictionary(nodeAnalysis => nodeAnalysis.Node);
    var criticalPathIdSet = analysis.CriticalPath.ToHashSet();
    var unblockedCards = graph.Cards
      .Where(card => IsUnblocked(card, cardsById))
      .ToArray();
    var planItems = MarkShortestAlternative(
      analysis.Recommendations
        .Select(recommendation => cardsById.TryGetValue(
          recommendation.Node,
          out var card)
            ? ToBoardWorkRecommendation(
              card,
              recommendation,
              FindNodeAnalysis(nodeAnalyses, card.Id),
              today,
              preferences)
            : null)
        .OfType<BoardWorkRecommendation>()
        .ToArray(),
      preferences);

    return new BoardGraphAnalysis(
      board.Id,
      analysis.IsAcyclic,
      ToCards(analysis.ReadyNodes, cardsById),
      unblockedCards,
      ToCards(analysis.DependencyOrder, cardsById),
      ToCards(analysis.CriticalPath, cardsById),
      planItems
        .Where(item => item.IsActionable)
        .Select(item => item.Card)
        .Take(NextCardLimit)
        .ToArray(),
      planItems,
      nodeAnalyses,
      criticalPathIdSet,
      preferences);
  }

  public static BoardCardDependencyAnalysis? AnalyzeCard(
    IGraphEngine<Guid> graphEngine,
    ProjectBoard board,
    Guid cardId,
    int maxSuggestions = DefaultSuggestionLimit)
  {
    ArgumentNullException.ThrowIfNull(graphEngine);
    ArgumentNullException.ThrowIfNull(board);

    var graph = ToGraph(board);
    var cardsById = graph.Cards.ToDictionary(card => card.Id);
    var nodeAnalyses = graphEngine
      .AnalyzeNodes(graph.Nodes, graph.Edges)
      .ToDictionary(analysis => analysis.Node);
    var criticalPathIds = graphEngine
      .FindCriticalPath(graph.Nodes, graph.Edges)
      .ToHashSet();

    if (!cardsById.TryGetValue(cardId, out var card))
    {
      return null;
    }

    var blockingDependencies = card.Dependencies
      .Select(dependency => cardsById.TryGetValue(
        dependency.CardId,
        out var dependencyCard)
          ? dependencyCard
          : null)
      .OfType<ProjectBoardCard>()
      .Where(dependency => !dependency.IsCompleted)
      .ToArray();
    var readyCardIds = graphEngine.FindReadyNodes(graph.Nodes, graph.Edges)
      .ToHashSet();
    var suggestions = graphEngine.SuggestTargets(
      graph.Nodes,
      graph.Edges,
      cardId,
      maxSuggestions);
    var impacted = graphEngine.FindImpactedDependents(
      graph.Nodes,
      graph.Edges,
      cardId);
    var directlyUnlocked = graph.Cards
      .Where(dependent => !dependent.IsCompleted)
      .Where(dependent => dependent.Dependencies.Any(dependency =>
        dependency.CardId == cardId))
      .Where(dependent => dependent.Dependencies.All(dependency =>
        dependency.CardId == cardId
          || (cardsById.TryGetValue(dependency.CardId, out var dependencyCard)
            && dependencyCard.IsCompleted)))
      .ToArray();

    return new BoardCardDependencyAnalysis(
      cardId,
      readyCardIds.Contains(cardId),
      blockingDependencies.Length == 0,
      blockingDependencies,
      ToCards(suggestions, cardsById),
      ToCards(impacted, cardsById),
      directlyUnlocked,
      FindNodeAnalysis(nodeAnalyses, cardId),
      criticalPathIds.Contains(cardId),
      nodeAnalyses,
      criticalPathIds);
  }

  private static BoardGraphSnapshot ToGraph(ProjectBoard board)
  {
    var cards = board.Lists
      .SelectMany(list => list.Cards)
      .ToArray();
    var cardIds = cards
      .Select(card => card.Id)
      .ToArray();
    var edges = cards
      .SelectMany(card => card.Dependencies.Select(dependency =>
        new GraphEdge<Guid>(card.Id, dependency.CardId)))
      .ToArray();

    return new BoardGraphSnapshot(cards, cardIds, edges);
  }

  private static ProjectBoardCard[] ToCards(
    IReadOnlyCollection<Guid> cardIds,
    Dictionary<Guid, ProjectBoardCard> cardsById) =>
    cardIds
      .Select(cardId => cardsById.TryGetValue(cardId, out var card)
        ? card
        : null)
      .OfType<ProjectBoardCard>()
      .ToArray();

  private static GraphWorkItem<Guid>[] ToWorkItems(
    IReadOnlyCollection<ProjectBoardCard> cards,
    DateOnly today,
    HashSet<Guid> inProgressListIds,
    BoardPlanningPreferences preferences) =>
    cards
      .Select(card => new GraphWorkItem<Guid>(
        card.Id,
        card.IsCompleted,
        PriorityScore(card.Priority),
        card.DueDate is null
          ? GraphWorkItem<Guid>.NoDueDate
          : card.DueDate.Value.DayNumber - today.DayNumber,
        card.Subtasks.Count,
        card.Subtasks.Count(subtask => subtask.IsCompleted),
        Math.Max(0, card.Position),
        card.EstimatedMinutes ?? 0,
        AssignmentOf(card, preferences.UserId),
        inProgressListIds.Contains(card.ListId),
        preferences.MatchesContext(card.Labels)))
      .ToArray();

  private static GraphAssignment AssignmentOf(ProjectBoardCard card, Guid userId)
  {
    if (card.Assignees.Any(assignee => assignee.UserId == userId))
    {
      return GraphAssignment.Requester;
    }

    return card.Assignees.Count > 0
      ? GraphAssignment.Others
      : GraphAssignment.Unassigned;
  }

  private static BoardWorkRecommendation[] MarkShortestAlternative(
    BoardWorkRecommendation[] planItems,
    BoardPlanningPreferences preferences)
  {
    if (preferences.AvailableMinutes == 0
      || planItems.Any(item => item.IsActionable && item.FitsAvailableTime))
    {
      return planItems;
    }

    var alternative = planItems
      .Where(item => item.IsActionable && item.Card.EstimatedMinutes is not null)
      .MinBy(item => item.Card.EstimatedMinutes);

    return alternative is null
      ? planItems
      : planItems
        .Select(item => item == alternative
          ? item with { IsShortestAlternative = true }
          : item)
        .ToArray();
  }

  private static BoardWorkRecommendation ToBoardWorkRecommendation(
    ProjectBoardCard card,
    GraphWorkRecommendation<Guid> recommendation,
    GraphNodeAnalysis<Guid>? nodeAnalysis,
    DateOnly today,
    BoardPlanningPreferences preferences) =>
    new(
      card,
      recommendation.Score,
      recommendation.BlockerCount == 0,
      recommendation.BlockerCount,
      recommendation.FitsAvailableTime,
      recommendation.IsAssignmentMatch,
      recommendation.IsContextMatch,
      recommendation.IsInProgress,
      false,
      BuildReasons(card, recommendation, nodeAnalysis, today, preferences));

  private static string[] BuildReasons(
    ProjectBoardCard card,
    GraphWorkRecommendation<Guid> recommendation,
    GraphNodeAnalysis<Guid>? nodeAnalysis,
    DateOnly today,
    BoardPlanningPreferences preferences)
  {
    var reasons = new List<string>(8);

    reasons.Add(recommendation.BlockerCount == 0
      ? "Ready now"
      : $"{recommendation.BlockerCount} blocker{Plural(recommendation.BlockerCount)}");

    if (preferences.AvailableMinutes > 0)
    {
      reasons.Add(card.EstimatedMinutes is null
        ? "Effort unknown"
        : recommendation.FitsAvailableTime
          ? $"Fits {preferences.AvailableMinutes}m focus"
          : $"Longer than {preferences.AvailableMinutes}m");
    }

    if (recommendation.IsAssignmentMatch)
    {
      reasons.Add("Assigned to you");
    }

    if (recommendation.IsInProgress)
    {
      reasons.Add("Already in progress");
    }

    if (recommendation.IsContextMatch)
    {
      reasons.Add($"Matches #{preferences.Context}");
    }

    if (recommendation.IsCriticalPath)
    {
      reasons.Add("Critical path");
    }

    if (nodeAnalysis?.TransitiveDependentCount > 0)
    {
      reasons.Add($"Unlocks {nodeAnalysis.TransitiveDependentCount}");
    }

    if (card.DueDate is not null)
    {
      var dueDays = card.DueDate.Value.DayNumber - today.DayNumber;

      if (dueDays < 0)
      {
        reasons.Add("Overdue");
      }
      else if (dueDays == 0)
      {
        reasons.Add("Due today");
      }
      else if (dueDays <= 7)
      {
        reasons.Add($"Due in {dueDays}d");
      }
    }

    if (card.Priority == BoardCardPriority.High)
    {
      reasons.Add("High priority");
    }

    if (card.Subtasks.Count > 0)
    {
      var completedSubtasks = card.Subtasks.Count(subtask => subtask.IsCompleted);
      reasons.Add($"{completedSubtasks}/{card.Subtasks.Count} subtasks");
    }

    return reasons
      .Take(6)
      .ToArray();
  }

  private static GraphNodeAnalysis<Guid>? FindNodeAnalysis(
    Dictionary<Guid, GraphNodeAnalysis<Guid>> nodeAnalyses,
    Guid cardId) =>
    nodeAnalyses.TryGetValue(cardId, out var analysis) ? analysis : null;

  private static bool IsUnblocked(
    ProjectBoardCard card,
    Dictionary<Guid, ProjectBoardCard> cardsById) =>
    card.Dependencies.All(dependency =>
      cardsById.TryGetValue(dependency.CardId, out var dependencyCard)
        && dependencyCard.IsCompleted);

  private static int PriorityScore(BoardCardPriority? priority) =>
    priority switch
    {
      BoardCardPriority.High => 3,
      BoardCardPriority.Medium => 2,
      BoardCardPriority.Low => 1,
      _ => 0
    };

  private static string Plural(int value) => value == 1 ? string.Empty : "s";

  private sealed record BoardGraphSnapshot(
    IReadOnlyCollection<ProjectBoardCard> Cards,
    IReadOnlyCollection<Guid> Nodes,
    IReadOnlyCollection<GraphEdge<Guid>> Edges);
}

internal sealed record BoardGraphAnalysis(
  Guid BoardId,
  bool IsAcyclic,
  IReadOnlyCollection<ProjectBoardCard> ReadyCards,
  IReadOnlyCollection<ProjectBoardCard> UnblockedCards,
  IReadOnlyCollection<ProjectBoardCard> DependencyOrder,
  IReadOnlyCollection<ProjectBoardCard> CriticalPath,
  IReadOnlyCollection<ProjectBoardCard> NextCards,
  IReadOnlyCollection<BoardWorkRecommendation> PlanItems,
  Dictionary<Guid, GraphNodeAnalysis<Guid>> NodeAnalyses,
  HashSet<Guid> CriticalPathIds,
  BoardPlanningPreferences Preferences);

internal sealed record BoardWorkRecommendation(
  ProjectBoardCard Card,
  int Score,
  bool IsActionable,
  int BlockerCount,
  bool FitsAvailableTime,
  bool IsAssignedToMe,
  bool IsContextMatch,
  bool IsInProgress,
  bool IsShortestAlternative,
  IReadOnlyCollection<string> Reasons);

internal sealed record BoardCardDependencyAnalysis(
  Guid CardId,
  bool IsStructurallyReady,
  bool IsUnblocked,
  IReadOnlyCollection<ProjectBoardCard> BlockingDependencies,
  IReadOnlyCollection<ProjectBoardCard> SuggestedDependencies,
  IReadOnlyCollection<ProjectBoardCard> ImpactedDependents,
  IReadOnlyCollection<ProjectBoardCard> DirectlyUnlockedDependents,
  GraphNodeAnalysis<Guid>? NodeAnalysis,
  bool IsOnCriticalPath,
  Dictionary<Guid, GraphNodeAnalysis<Guid>> NodeAnalyses,
  HashSet<Guid> CriticalPathIds);
