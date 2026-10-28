using System.Net;
using System.Net.Http.Json;
using Coordina.Api.Tests.Support;

namespace Coordina.Api.Tests;

public sealed class BoardPlanningEndpointsTests(ApiTestApplicationFactory factory)
  : ApiTest(factory)
{
  private static readonly string[] FlowLists = ["Todo", "Doing", "Done"];
  private static readonly string[] BackendLabel = ["backend"];

  [Fact]
  public async Task Preferences_RankMyFittingContextCardFirst()
  {
    var flow = await CreateFlowAsync();
    var todo = flow.ListId("Todo");
    await CreateCardAsync(flow, todo, "Long backend", new
    {
      estimatedMinutes = 120,
      labels = BackendLabel,
      assigneeIds = new[] { flow.UserId }
    });
    await CreateCardAsync(flow, todo, "Quick backend", new
    {
      estimatedMinutes = 30,
      labels = BackendLabel,
      assigneeIds = new[] { flow.UserId }
    });

    var graph = await GetGraphAsync(flow, "?availableMinutes=60&context=%23Backend");

    var first = graph.PlanItems.First();
    Assert.Equal("Quick backend", first.Card.Title);
    Assert.Equal(30, first.EstimatedMinutes);
    Assert.True(first.FitsAvailableTime);
    Assert.True(first.IsAssignedToMe);
    Assert.True(first.IsContextMatch);
    Assert.False(first.IsShortestAlternative);
    Assert.Contains("Fits 60m focus", first.Reasons);
    var longCard = graph.PlanItems.Single(item => item.Card.Title == "Long backend");
    Assert.False(longCard.FitsAvailableTime);
    Assert.Equal(60, graph.AvailableMinutes);
    Assert.Equal("Backend", graph.Context);
  }

  [Fact]
  public async Task WorkAssignedToSomeoneElse_RanksBelowMine()
  {
    var flow = await CreateFlowAsync();
    var teammate = await JoinAsTeammateAsync(flow);
    var todo = flow.ListId("Todo");
    await CreateCardAsync(flow, todo, "Theirs", new
    {
      priority = "HIGH",
      assigneeIds = new[] { teammate }
    });
    await CreateCardAsync(flow, todo, "Mine", new
    {
      assigneeIds = new[] { flow.UserId }
    });

    var graph = await GetGraphAsync(flow, string.Empty);

    Assert.Equal("Mine", graph.PlanItems.First().Card.Title);
    Assert.Contains(graph.PlanItems, item => item.Card.Title == "Theirs");
  }

  [Fact]
  public async Task BlockedWork_IsNeverPrimary()
  {
    var flow = await CreateFlowAsync();
    var todo = flow.ListId("Todo");
    var blocker = await CreateCardAsync(flow, todo, "Foundation", new { });
    var blocked = await CreateCardAsync(flow, todo, "Urgent feature", new
    {
      priority = "HIGH",
      dueDate = DateOnly.FromDateTime(DateTime.UtcNow),
      assigneeIds = new[] { flow.UserId }
    });
    var dependency = await Api.SendAsync(
      HttpMethod.Post,
      $"{flow.BoardPath}/cards/{blocked}/dependencies",
      flow.AccessToken,
      new { dependsOnCardId = blocker });
    dependency.EnsureSuccessStatusCode();

    var graph = await GetGraphAsync(flow, "?availableMinutes=30");

    var first = graph.PlanItems.First();
    Assert.True(first.IsActionable);
    Assert.Equal("Foundation", first.Card.Title);
    Assert.False(graph.PlanItems.Single(item => item.Card.Title == "Urgent feature").IsActionable);
  }

  [Fact]
  public async Task NothingFits_FlagsShortestReadyAlternative()
  {
    var flow = await CreateFlowAsync();
    var todo = flow.ListId("Todo");
    await CreateCardAsync(flow, todo, "Two hours", new { estimatedMinutes = 120 });
    await CreateCardAsync(flow, todo, "One hour", new { estimatedMinutes = 60 });
    await CreateCardAsync(flow, todo, "Unknown", new { });

    var graph = await GetGraphAsync(flow, "?availableMinutes=30");

    Assert.DoesNotContain(graph.PlanItems, item => item.FitsAvailableTime);
    var alternative = Assert.Single(graph.PlanItems, item => item.IsShortestAlternative);
    Assert.Equal("One hour", alternative.Card.Title);
  }

  [Fact]
  public async Task CardInProgressList_IsFlaggedInProgress()
  {
    var flow = await CreateFlowAsync();
    await CreateCardAsync(flow, flow.ListId("Doing"), "Started", new { });
    await CreateCardAsync(flow, flow.ListId("Todo"), "Not started", new { });

    var graph = await GetGraphAsync(flow, string.Empty);

    var first = graph.PlanItems.First();
    Assert.Equal("Started", first.Card.Title);
    Assert.True(first.IsInProgress);
  }

  [Theory]
  [InlineData("?availableMinutes=3")]
  [InlineData("?availableMinutes=3000")]
  [InlineData("?context=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
  public async Task InvalidPreferences_ReturnValidationProblem(string query)
  {
    var flow = await CreateFlowAsync();

    var response = await Api.SendAsync(
      HttpMethod.Get,
      $"{flow.BoardPath}/graph{query}",
      flow.AccessToken);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task RepeatedCalls_ReturnTheSameOrder()
  {
    var flow = await CreateFlowAsync();
    var todo = flow.ListId("Todo");

    for (var index = 0; index < 5; index++)
    {
      await CreateCardAsync(flow, todo, $"Card {index}", new { estimatedMinutes = 30 });
    }

    var first = await GetGraphAsync(flow, "?availableMinutes=60");
    var second = await GetGraphAsync(flow, "?availableMinutes=60");

    Assert.Equal(
      first.PlanItems.Select(item => item.Card.Id),
      second.PlanItems.Select(item => item.Card.Id));
  }

  private async Task<string> CreateCardAsync(
    Flow flow,
    string listId,
    string title,
    object details)
  {
    var body = System.Text.Json.JsonSerializer.SerializeToNode(details)!.AsObject();
    body["title"] = title;
    var board = await Api.SendForAsync<BoardResponse>(
      HttpMethod.Post,
      $"{flow.BoardPath}/lists/{listId}/cards",
      flow.AccessToken,
      body);

    return board.Lists.SelectMany(list => list.Cards).Single(card => card.Title == title).Id;
  }

  private Task<BoardGraphResponse> GetGraphAsync(Flow flow, string query) =>
    Api.SendForAsync<BoardGraphResponse>(
      HttpMethod.Get,
      $"{flow.BoardPath}/graph{query}",
      flow.AccessToken);

  private async Task<Flow> CreateFlowAsync()
  {
    var owner = await Api.RegisterAsync("Planner");
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Planning Team");
    var project = await Api.CreateProjectAsync(owner.AccessToken, workspace.Id, "Planning");
    var board = await Api.CreateBoardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      "CUSTOM",
      FlowLists,
      "Planning");

    return new Flow(owner.AccessToken, owner.User.Id, workspace.Id, project.Id, board);
  }

  private async Task<string> JoinAsTeammateAsync(Flow flow)
  {
    var teammate = await Api.AddMemberAsync(flow.AccessToken, flow.WorkspaceId);

    return teammate.User.Id;
  }

  private sealed record Flow(
    string AccessToken,
    string UserId,
    string WorkspaceId,
    string ProjectId,
    BoardResponse Board)
  {
    public string BoardPath =>
      $"/workspaces/{WorkspaceId}/projects/{ProjectId}/boards/{Board.Id}";

    public string ListId(string title) =>
      Board.Lists.Single(list => list.Title == title).Id;
  }
}
