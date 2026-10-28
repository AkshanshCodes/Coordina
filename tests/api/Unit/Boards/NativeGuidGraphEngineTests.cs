using Coordina.Api.Modules.Boards.Application.Graph;

namespace Coordina.Api.Tests.Unit.Boards;

public sealed class NativeGuidGraphEngineTests
{
  private readonly NativeGuidGraphEngine _graphEngine = new();

  [Fact]
  public void EvaluateEdge_WhenTargetCanReachSource_ReturnsCycle()
  {
    var backlog = Guid.NewGuid();
    var ready = Guid.NewGuid();
    var done = Guid.NewGuid();

    var result = _graphEngine.EvaluateEdge(
      [backlog, ready, done],
      [
        new GraphEdge<Guid>(backlog, ready),
        new GraphEdge<Guid>(ready, done)
      ],
      done,
      backlog);

    Assert.False(result.CanAdd);
    Assert.Equal(GraphEdgeStatus.Cycle, result.Status);
  }

  [Fact]
  public void EvaluateEdge_WhenEdgeAlreadyExists_ReturnsDuplicate()
  {
    var source = Guid.NewGuid();
    var target = Guid.NewGuid();

    var result = _graphEngine.EvaluateEdge(
      [source, target],
      [new GraphEdge<Guid>(source, target)],
      source,
      target);

    Assert.False(result.CanAdd);
    Assert.Equal(GraphEdgeStatus.Duplicate, result.Status);
  }

  [Fact]
  public void EvaluateEdge_WhenNoCycleWouldBeCreated_ReturnsAllowed()
  {
    var source = Guid.NewGuid();
    var existingTarget = Guid.NewGuid();
    var newTarget = Guid.NewGuid();

    var result = _graphEngine.EvaluateEdge(
      [source, existingTarget, newTarget],
      [new GraphEdge<Guid>(source, existingTarget)],
      source,
      newTarget);

    Assert.True(result.CanAdd);
    Assert.Equal(GraphEdgeStatus.Allowed, result.Status);
  }

  [Fact]
  public void SuggestTargets_ExcludesSelfDuplicatesAndCycleTargets()
  {
    var first = Guid.NewGuid();
    var second = Guid.NewGuid();
    var third = Guid.NewGuid();
    var fourth = Guid.NewGuid();

    var suggestions = _graphEngine.SuggestTargets(
      [first, second, third, fourth],
      [
        new GraphEdge<Guid>(first, second),
        new GraphEdge<Guid>(third, first)
      ],
      first,
      10);

    Assert.Equal([fourth], suggestions);
  }

  [Fact]
  public void SortTopologically_ReturnsDependencyFirstOrder()
  {
    var dependency = Guid.NewGuid();
    var foundation = Guid.NewGuid();
    var api = Guid.NewGuid();
    var ui = Guid.NewGuid();

    var ordered = _graphEngine.SortTopologically(
      [dependency, foundation, api, ui],
      [
        new GraphEdge<Guid>(foundation, dependency),
        new GraphEdge<Guid>(api, foundation),
        new GraphEdge<Guid>(ui, foundation)
      ]);

    Assert.Equal([dependency, foundation, api, ui], ordered);
  }

  [Fact]
  public void FindReadyNodes_ReturnsCardsWithoutDependencies()
  {
    var dependency = Guid.NewGuid();
    var api = Guid.NewGuid();
    var ui = Guid.NewGuid();

    var ready = _graphEngine.FindReadyNodes(
      [dependency, api, ui],
      [
        new GraphEdge<Guid>(api, dependency),
        new GraphEdge<Guid>(ui, dependency)
      ]);

    Assert.Equal([dependency], ready);
  }

  [Fact]
  public void FindImpactedDependents_ReturnsTransitiveDependents()
  {
    var dependency = Guid.NewGuid();
    var foundation = Guid.NewGuid();
    var api = Guid.NewGuid();
    var ui = Guid.NewGuid();

    var impacted = _graphEngine.FindImpactedDependents(
      [dependency, foundation, api, ui],
      [
        new GraphEdge<Guid>(foundation, dependency),
        new GraphEdge<Guid>(api, foundation),
        new GraphEdge<Guid>(ui, foundation)
      ],
      dependency);

    Assert.Equal([foundation, api, ui], impacted);
  }

  [Fact]
  public void AnalyzeNodes_ReturnsDependencyPressureMetrics()
  {
    var dependency = Guid.NewGuid();
    var foundation = Guid.NewGuid();
    var api = Guid.NewGuid();
    var ui = Guid.NewGuid();

    var analysis = _graphEngine.AnalyzeNodes(
      [dependency, foundation, api, ui],
      [
        new GraphEdge<Guid>(foundation, dependency),
        new GraphEdge<Guid>(api, foundation),
        new GraphEdge<Guid>(ui, foundation)
      ]).ToDictionary(node => node.Node);

    Assert.Equal(0, analysis[dependency].DependencyCount);
    Assert.Equal(1, analysis[dependency].DependentCount);
    Assert.Equal(0, analysis[dependency].DependencyDepth);
    Assert.Equal(2, analysis[dependency].DependentDepth);
    Assert.Equal(3, analysis[dependency].TransitiveDependentCount);
    Assert.Equal(1, analysis[api].DependencyCount);
    Assert.Equal(0, analysis[api].DependentCount);
    Assert.Equal(2, analysis[api].DependencyDepth);
    Assert.Equal(0, analysis[api].DependentDepth);
    Assert.Equal(0, analysis[api].TransitiveDependentCount);
  }

  [Fact]
  public void FindCriticalPath_ReturnsLongestDependencyChain()
  {
    var dependency = Guid.NewGuid();
    var foundation = Guid.NewGuid();
    var api = Guid.NewGuid();
    var ui = Guid.NewGuid();

    var criticalPath = _graphEngine.FindCriticalPath(
      [dependency, foundation, api, ui],
      [
        new GraphEdge<Guid>(foundation, dependency),
        new GraphEdge<Guid>(api, foundation),
        new GraphEdge<Guid>(ui, foundation)
      ]);

    Assert.Equal([dependency, foundation, api], criticalPath);
  }

  [Fact]
  public void PlanWork_RanksActionableCardsByProductImpact()
  {
    var dependency = Guid.NewGuid();
    var api = Guid.NewGuid();
    var ui = Guid.NewGuid();
    var urgentStandalone = Guid.NewGuid();

    var recommendations = _graphEngine.PlanWork(
      [dependency, api, ui, urgentStandalone],
      [
        new GraphEdge<Guid>(api, dependency),
        new GraphEdge<Guid>(ui, api)
      ],
      [
        new GraphWorkItem<Guid>(dependency, false, 1, 7, 0, 0, 0),
        new GraphWorkItem<Guid>(api, false, 3, 0, 3, 1, 1),
        new GraphWorkItem<Guid>(ui, false, 3, -1, 2, 0, 2),
        new GraphWorkItem<Guid>(urgentStandalone, false, 3, 0, 0, 0, 3)
      ]).ToArray();

    Assert.Equal(dependency, recommendations[0].Node);
    Assert.Equal(0, recommendations[0].BlockerCount);
    Assert.True(recommendations[0].IsCriticalPath);
    Assert.True(recommendations[0].UnlockScore > recommendations[1].UnlockScore);
    Assert.Equal(urgentStandalone, recommendations[1].Node);
    Assert.Equal(0, recommendations[1].BlockerCount);
    Assert.True(recommendations[2].BlockerCount > 0);
  }

  [Fact]
  public void PlanWork_PrefersRequesterWorkThatFitsTheFocusBlock()
  {
    var theirs = Guid.NewGuid();
    var longMine = Guid.NewGuid();
    var quickMine = Guid.NewGuid();

    var recommendations = _graphEngine.PlanWork(
      [theirs, longMine, quickMine],
      [],
      [
        new GraphWorkItem<Guid>(theirs, false, 0, GraphWorkItem<Guid>.NoDueDate, 0, 0, 0,
          EstimatedMinutes: 30, Assignment: GraphAssignment.Others),
        new GraphWorkItem<Guid>(longMine, false, 0, GraphWorkItem<Guid>.NoDueDate, 0, 0, 1,
          EstimatedMinutes: 120, Assignment: GraphAssignment.Requester),
        new GraphWorkItem<Guid>(quickMine, false, 0, GraphWorkItem<Guid>.NoDueDate, 0, 0, 2,
          EstimatedMinutes: 30, Assignment: GraphAssignment.Requester,
          IsInProgress: true, IsContextMatch: true)
      ],
      new GraphPlanningOptions(AvailableMinutes: 60)).ToArray();

    Assert.Equal([quickMine, longMine, theirs], recommendations.Select(item => item.Node));
    var best = recommendations[0];
    Assert.True(best.FitsAvailableTime);
    Assert.True(best.IsAssignmentMatch);
    Assert.True(best.IsContextMatch);
    Assert.True(best.IsInProgress);
    Assert.True(best.EffortScore > 0);
    Assert.True(best.AssignmentScore > 0);
    Assert.True(best.MomentumScore > 0);
    Assert.True(best.ContextScore > 0);
    Assert.False(recommendations[1].FitsAvailableTime);
    Assert.True(recommendations[1].EffortScore < 0);
    Assert.True(recommendations[2].AssignmentScore < 0);
  }

  [Fact]
  public void PlanWork_WithoutOptions_IsUnrestricted()
  {
    var first = Guid.NewGuid();
    var second = Guid.NewGuid();

    var recommendations = _graphEngine.PlanWork(
      [first, second],
      [],
      [
        new GraphWorkItem<Guid>(first, false, 0, GraphWorkItem<Guid>.NoDueDate, 0, 0, 0,
          EstimatedMinutes: 480),
        new GraphWorkItem<Guid>(second, false, 0, GraphWorkItem<Guid>.NoDueDate, 0, 0, 1)
      ]).ToArray();

    Assert.Equal([first, second], recommendations.Select(item => item.Node));
    Assert.All(recommendations, item => Assert.True(item.FitsAvailableTime));
    Assert.All(recommendations, item => Assert.False(item.IsCriticalPath));
  }

  [Fact]
  public void AnalyzeBoard_MatchesIndividualCalls()
  {
    var foundation = Guid.NewGuid();
    var api = Guid.NewGuid();
    var ui = Guid.NewGuid();
    var docs = Guid.NewGuid();
    Guid[] nodes = [foundation, api, ui, docs];
    GraphEdge<Guid>[] edges =
    [
      new(api, foundation),
      new(ui, api)
    ];
    GraphWorkItem<Guid>[] items =
    [
      new(foundation, false, 1, 3, 0, 0, 0, EstimatedMinutes: 30),
      new(api, false, 3, 1, 2, 1, 1, EstimatedMinutes: 120),
      new(ui, false, 2, GraphWorkItem<Guid>.NoDueDate, 0, 0, 2),
      new(docs, false, 0, GraphWorkItem<Guid>.NoDueDate, 0, 0, 3,
        Assignment: GraphAssignment.Requester)
    ];
    var options = new GraphPlanningOptions(AvailableMinutes: 60);

    var analysis = _graphEngine.AnalyzeBoard(nodes, edges, items, options);

    Assert.Equal(_graphEngine.IsAcyclic(nodes, edges), analysis.IsAcyclic);
    Assert.Equal(_graphEngine.FindReadyNodes(nodes, edges), analysis.ReadyNodes);
    Assert.Equal(_graphEngine.SortTopologically(nodes, edges), analysis.DependencyOrder);
    Assert.Equal(_graphEngine.FindCriticalPath(nodes, edges), analysis.CriticalPath);
    Assert.Equal(_graphEngine.AnalyzeNodes(nodes, edges), analysis.NodeAnalyses);
    Assert.Equal(_graphEngine.PlanWork(nodes, edges, items, options), analysis.Recommendations);
  }

  [Fact]
  public void AnalyzeBoard_ReportsCyclesWithoutPlan()
  {
    var first = Guid.NewGuid();
    var second = Guid.NewGuid();

    var analysis = _graphEngine.AnalyzeBoard(
      [first, second],
      [new GraphEdge<Guid>(first, second), new GraphEdge<Guid>(second, first)],
      [
        new GraphWorkItem<Guid>(first, false, 0, GraphWorkItem<Guid>.NoDueDate, 0, 0, 0),
        new GraphWorkItem<Guid>(second, false, 0, GraphWorkItem<Guid>.NoDueDate, 0, 0, 1)
      ]);

    Assert.False(analysis.IsAcyclic);
    Assert.Empty(analysis.DependencyOrder);
    Assert.Empty(analysis.Recommendations);
  }
}
