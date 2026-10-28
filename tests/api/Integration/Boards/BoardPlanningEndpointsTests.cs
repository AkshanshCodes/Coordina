using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coordina.Api.Tests.Support;

namespace Coordina.Api.Tests;

public sealed class BoardPlanningEndpointsTests(ApiTestApplicationFactory factory)
  : IClassFixture<ApiTestApplicationFactory>
{
  private static readonly string[] FlowLists = ["Todo", "Doing", "Done"];
  private static readonly string[] BackendLabel = ["backend"];
  private readonly HttpClient _client = factory.CreateClient();

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
    var dependency = await SendAsync(
      flow.AccessToken,
      HttpMethod.Post,
      $"{flow.BoardPath}/cards/{blocked}/dependencies",
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

    var response = await SendAsync(
      flow.AccessToken,
      HttpMethod.Get,
      $"{flow.BoardPath}/graph{query}");

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

  private async Task<Flow> CreateFlowAsync()
  {
    var auth = await RegisterAsync();
    var workspace = await SendForAsync<IdResponse>(
      auth.AccessToken, HttpMethod.Post, "/workspaces", new { name = "Planning Team" });
    var project = await SendForAsync<IdResponse>(
      auth.AccessToken, HttpMethod.Post, $"/workspaces/{workspace.Id}/projects",
      new { name = "Planning" });
    var board = await SendForAsync<BoardResponse>(
      auth.AccessToken, HttpMethod.Post,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards",
      new { name = "Planning", template = "CUSTOM", customListTitles = FlowLists });

    return new Flow(auth.AccessToken, auth.User.Id, workspace.Id, project.Id, board);
  }

  private async Task<string> JoinAsTeammateAsync(Flow flow)
  {
    var teammate = await RegisterAsync();
    var invite = await SendForAsync<InviteResponse>(
      flow.AccessToken, HttpMethod.Post, $"/workspaces/{flow.WorkspaceId}/invites");
    await SendForAsync<IdResponse>(
      teammate.AccessToken, HttpMethod.Post, "/workspaces/join",
      new { inviteCode = invite.Code });

    return teammate.User.Id;
  }

  private async Task<AuthResponse> RegisterAsync()
  {
    var response = await _client.PostAsJsonAsync("/auth/register", new
    {
      name = "Planner",
      email = $"planner-{Guid.NewGuid():N}@coordina.test",
      password = "Password123!"
    });
    response.EnsureSuccessStatusCode();
    var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
    Assert.NotNull(auth);

    return auth;
  }

  private async Task<string> CreateCardAsync(
    Flow flow,
    string listId,
    string title,
    object details)
  {
    var body = System.Text.Json.JsonSerializer.SerializeToNode(details)!.AsObject();
    body["title"] = title;
    var board = await SendForAsync<BoardResponse>(
      flow.AccessToken,
      HttpMethod.Post,
      $"{flow.BoardPath}/lists/{listId}/cards",
      body);

    return board.Lists.SelectMany(list => list.Cards).Single(card => card.Title == title).Id;
  }

  private Task<GraphResponse> GetGraphAsync(Flow flow, string query) =>
    SendForAsync<GraphResponse>(
      flow.AccessToken,
      HttpMethod.Get,
      $"{flow.BoardPath}/graph{query}");

  private async Task<T> SendForAsync<T>(
    string accessToken,
    HttpMethod method,
    string path,
    object? body = null)
  {
    using var response = await SendAsync(accessToken, method, path, body);
    response.EnsureSuccessStatusCode();
    var payload = await response.Content.ReadFromJsonAsync<T>();
    Assert.NotNull(payload);

    return payload;
  }

  private async Task<HttpResponseMessage> SendAsync(
    string accessToken,
    HttpMethod method,
    string path,
    object? body = null)
  {
    using var request = new HttpRequestMessage(method, path);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    if (body is not null)
    {
      request.Content = JsonContent.Create(body);
    }

    return await _client.SendAsync(request);
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

  private sealed record AuthResponse(string AccessToken, UserResponse User);

  private sealed record UserResponse(string Id);

  private sealed record IdResponse(string Id);

  private sealed record InviteResponse(string Code);

  private sealed record BoardResponse(string Id, IReadOnlyCollection<ListResponse> Lists);

  private sealed record ListResponse(
    string Id,
    string Title,
    IReadOnlyCollection<CardResponse> Cards);

  private sealed record CardResponse(string Id, string Title);

  private sealed record GraphResponse(
    int AvailableMinutes,
    string? Context,
    IReadOnlyCollection<PlanItemResponse> PlanItems);

  private sealed record PlanItemResponse(
    GraphCardResponse Card,
    bool IsActionable,
    int? EstimatedMinutes,
    bool FitsAvailableTime,
    bool IsAssignedToMe,
    bool IsContextMatch,
    bool IsInProgress,
    bool IsShortestAlternative,
    IReadOnlyCollection<string> Reasons);

  private sealed record GraphCardResponse(string Id, string Title);
}
