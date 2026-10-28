using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coordina.Api.Tests.Support;

namespace Coordina.Api.Tests;

public sealed class BoardWorkflowEndpointsTests(ApiTestApplicationFactory factory)
  : IClassFixture<ApiTestApplicationFactory>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Theory]
  [InlineData("BASIC", new[] { "TODO", "IN_PROGRESS", "DONE" })]
  [InlineData("AGILE_SCRUM", new[] { "TODO", "TODO", "IN_PROGRESS", "IN_PROGRESS", "DONE" })]
  [InlineData("BUG_TRACKING", new[] { "TODO", "IN_PROGRESS", "IN_PROGRESS", "DONE" })]
  [InlineData("PRODUCT_ROADMAP", new[] { "TODO", "TODO", "IN_PROGRESS", "DONE" })]
  public async Task TemplateBoards_ExposeDeclaredStatuses(
    string template,
    string[] expectedStatuses)
  {
    var scope = await CreateScopeAsync();

    var board = await CreateBoardAsync(scope, template, null);

    Assert.Equal(expectedStatuses, StatusesOf(board));
    Assert.All(
      board.Lists.SelectMany(list => list.Cards.Select(card => (list, card))),
      item => Assert.Equal(item.list.TaskStatus == "DONE", item.card.IsCompleted));
  }

  [Theory]
  [InlineData(new[] { "Only" }, new[] { "TODO" })]
  [InlineData(new[] { "Open", "Closed" }, new[] { "TODO", "DONE" })]
  [InlineData(new[] { "Inbox", "Build", "Verify", "Live" }, new[] { "TODO", "IN_PROGRESS", "IN_PROGRESS", "DONE" })]
  public async Task CustomBoard_AssignsStatusesByPosition(
    string[] titles,
    string[] expectedStatuses)
  {
    var scope = await CreateScopeAsync();

    var board = await CreateBoardAsync(scope, "CUSTOM", titles);

    Assert.Equal(expectedStatuses, StatusesOf(board));
  }

  [Fact]
  public async Task CreateList_WithoutStatus_DefaultsToTodo()
  {
    var scope = await CreateScopeAsync();
    var board = await CreateBoardAsync(scope, "CUSTOM", ["Open", "Closed"]);

    var updated = await SendForBoardAsync(
      scope,
      HttpMethod.Post,
      $"{BoardPath(scope, board)}/lists",
      new { title = "Later" });

    Assert.Equal("TODO", updated.Lists.Single(list => list.Title == "Later").TaskStatus);
  }

  [Fact]
  public async Task CreateList_WithStatus_UsesIt()
  {
    var scope = await CreateScopeAsync();
    var board = await CreateBoardAsync(scope, "CUSTOM", ["Open", "Closed"]);

    var updated = await SendForBoardAsync(
      scope,
      HttpMethod.Post,
      $"{BoardPath(scope, board)}/lists",
      new { title = "Doing", taskStatus = "in_progress" });

    Assert.Equal(
      "IN_PROGRESS",
      updated.Lists.Single(list => list.Title == "Doing").TaskStatus);
  }

  [Fact]
  public async Task CreateList_WithUnknownStatus_ReturnsValidationProblem()
  {
    var scope = await CreateScopeAsync();
    var board = await CreateBoardAsync(scope, "CUSTOM", ["Open", "Closed"]);

    var response = await SendAsync(
      scope.AccessToken,
      HttpMethod.Post,
      $"{BoardPath(scope, board)}/lists",
      new { title = "Doing", taskStatus = "BLOCKED" });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task RenameList_DoesNotChangeTaskStatus()
  {
    var scope = await CreateScopeAsync();
    var board = await CreateBoardAsync(scope, "BASIC", null);
    var doneList = board.Lists.Single(list => list.TaskStatus == "DONE");

    var renamed = await SendForBoardAsync(
      scope,
      HttpMethod.Patch,
      $"{BoardPath(scope, board)}/lists/{doneList.Id}",
      new { title = "Backlog" });

    var list = renamed.Lists.Single(item => item.Id == doneList.Id);
    Assert.Equal("Backlog", list.Title);
    Assert.Equal("DONE", list.TaskStatus);
  }

  [Fact]
  public async Task CreateCard_InDoneList_IsCompleted()
  {
    var scope = await CreateScopeAsync();
    var board = await CreateBoardAsync(scope, "BASIC", null);
    var doneList = ListWithStatus(board, "DONE");

    var updated = await CreateCardAsync(scope, board, doneList.Id, "Already shipped");

    var card = FindCard(updated, "Already shipped");
    Assert.True(card.IsCompleted);
    Assert.NotNull(card.CompletedAt);
  }

  [Fact]
  public async Task MoveCard_IntoDoneList_CompletesCard()
  {
    var scope = await CreateScopeAsync();
    var board = await CreateBoardAsync(scope, "BASIC", null);
    var created = await CreateCardAsync(scope, board, ListWithStatus(board, "TODO").Id, "Ship");
    var doneList = ListWithStatus(board, "DONE");

    var moved = await MoveCardAsync(scope, board, FindCard(created, "Ship").Id, doneList.Id);

    var card = FindCard(moved, "Ship");
    Assert.Equal(doneList.Id, card.ListId);
    Assert.True(card.IsCompleted);
    Assert.NotNull(card.CompletedAt);
  }

  [Fact]
  public async Task MoveCard_OutOfDoneList_ReopensCard()
  {
    var scope = await CreateScopeAsync();
    var board = await CreateBoardAsync(scope, "BASIC", null);
    var created = await CreateCardAsync(scope, board, ListWithStatus(board, "DONE").Id, "Regressed");
    var activeList = ListWithStatus(board, "IN_PROGRESS");

    var moved = await MoveCardAsync(scope, board, FindCard(created, "Regressed").Id, activeList.Id);

    var card = FindCard(moved, "Regressed");
    Assert.Equal(activeList.Id, card.ListId);
    Assert.False(card.IsCompleted);
    Assert.Null(card.CompletedAt);
  }

  [Fact]
  public async Task CompleteCard_MovesToFirstDoneList()
  {
    var scope = await CreateScopeAsync();
    var board = await CreateBoardAsync(scope, "BUG_TRACKING", null);
    var created = await CreateCardAsync(scope, board, ListWithStatus(board, "TODO").Id, "Fix crash");
    var card = FindCard(created, "Fix crash");

    var completed = await UpdateCardAsync(scope, board, card.Id, new { isCompleted = true });

    var updated = FindCard(completed, "Fix crash");
    Assert.Equal(ListWithStatus(board, "DONE").Id, updated.ListId);
    Assert.True(updated.IsCompleted);
  }

  [Fact]
  public async Task ReopenCard_MovesToFirstTodoList()
  {
    var scope = await CreateScopeAsync();
    var board = await CreateBoardAsync(scope, "AGILE_SCRUM", null);
    var created = await CreateCardAsync(scope, board, ListWithStatus(board, "DONE").Id, "Reopen me");
    var card = FindCard(created, "Reopen me");

    var reopened = await UpdateCardAsync(scope, board, card.Id, new { isCompleted = false });

    var updated = FindCard(reopened, "Reopen me");
    var backlog = board.Lists.Single(list => list.Title == "Backlog");
    Assert.Equal(backlog.Id, updated.ListId);
    Assert.False(updated.IsCompleted);
  }

  [Fact]
  public async Task CompleteCard_WithoutDoneList_ReturnsConflictAndPreservesCard()
  {
    var scope = await CreateScopeAsync();
    var board = await CreateBoardAsync(scope, "CUSTOM", ["Only"]);
    var created = await CreateCardAsync(scope, board, board.Lists.Single().Id, "Stuck");
    var card = FindCard(created, "Stuck");

    var response = await SendAsync(
      scope.AccessToken,
      HttpMethod.Patch,
      $"{BoardPath(scope, board)}/cards/{card.Id}",
      new { isCompleted = true, title = "Renamed while stuck" });

    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    var reloaded = await SendForAsync<BoardResponse>(
      scope.AccessToken,
      HttpMethod.Get,
      $"/workspaces/{scope.WorkspaceId}/projects/{scope.ProjectId}/board");
    var unchanged = FindCard(reloaded, "Stuck");
    Assert.False(unchanged.IsCompleted);
    Assert.Equal(card.ListId, unchanged.ListId);
  }

  [Fact]
  public async Task UpdateCard_WithoutCompletionChange_StaysInList()
  {
    var scope = await CreateScopeAsync();
    var board = await CreateBoardAsync(scope, "BASIC", null);
    var activeList = ListWithStatus(board, "IN_PROGRESS");
    var created = await CreateCardAsync(scope, board, activeList.Id, "Polish");
    var card = FindCard(created, "Polish");

    var updated = await UpdateCardAsync(scope, board, card.Id, new { title = "Polish UI" });

    var renamed = FindCard(updated, "Polish UI");
    Assert.Equal(activeList.Id, renamed.ListId);
    Assert.False(renamed.IsCompleted);
  }

  [Fact]
  public async Task CardEffort_CanBeSetKeptAndCleared()
  {
    var scope = await CreateScopeAsync();
    var board = await CreateBoardAsync(scope, "BASIC", null);
    var created = await SendForBoardAsync(
      scope,
      HttpMethod.Post,
      $"{BoardPath(scope, board)}/lists/{ListWithStatus(board, "TODO").Id}/cards",
      new { title = "Estimate me", estimatedMinutes = 30 });
    var card = FindCard(created, "Estimate me");
    Assert.Equal(30, card.EstimatedMinutes);

    var updated = await UpdateCardAsync(scope, board, card.Id, new { estimatedMinutes = 120 });
    Assert.Equal(120, FindCard(updated, "Estimate me").EstimatedMinutes);

    var kept = await UpdateCardAsync(scope, board, card.Id, new { title = "Estimated" });
    Assert.Equal(120, FindCard(kept, "Estimated").EstimatedMinutes);

    var cleared = await UpdateCardAsync(
      scope,
      board,
      card.Id,
      new { clearEstimatedMinutes = true });
    Assert.Null(FindCard(cleared, "Estimated").EstimatedMinutes);
  }

  [Theory]
  [InlineData(4)]
  [InlineData(2881)]
  public async Task CardEffort_OutOfRange_ReturnsValidationProblem(int minutes)
  {
    var scope = await CreateScopeAsync();
    var board = await CreateBoardAsync(scope, "BASIC", null);

    var response = await SendAsync(
      scope.AccessToken,
      HttpMethod.Post,
      $"{BoardPath(scope, board)}/lists/{ListWithStatus(board, "TODO").Id}/cards",
      new { title = "Too odd", estimatedMinutes = minutes });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  private static string[] StatusesOf(BoardResponse board) =>
    board.Lists
      .OrderBy(list => list.Position)
      .Select(list => list.TaskStatus)
      .ToArray();

  private static string BoardPath(TestScope scope, BoardResponse board) =>
    $"/workspaces/{scope.WorkspaceId}/projects/{scope.ProjectId}/boards/{board.Id}";

  private static BoardListResponse ListWithStatus(BoardResponse board, string status) =>
    board.Lists
      .OrderBy(list => list.Position)
      .First(list => list.TaskStatus == status);

  private static BoardCardResponse FindCard(BoardResponse board, string title) =>
    board.Lists
      .SelectMany(list => list.Cards)
      .Single(card => card.Title == title);

  private Task<BoardResponse> CreateCardAsync(
    TestScope scope,
    BoardResponse board,
    string listId,
    string title)
  {
    return SendForBoardAsync(
      scope,
      HttpMethod.Post,
      $"{BoardPath(scope, board)}/lists/{listId}/cards",
      new { title });
  }

  private Task<BoardResponse> MoveCardAsync(
    TestScope scope,
    BoardResponse board,
    string cardId,
    string listId)
  {
    return SendForBoardAsync(
      scope,
      HttpMethod.Patch,
      $"{BoardPath(scope, board)}/cards/{cardId}/move",
      new { listId });
  }

  private Task<BoardResponse> UpdateCardAsync(
    TestScope scope,
    BoardResponse board,
    string cardId,
    object body)
  {
    return SendForBoardAsync(
      scope,
      HttpMethod.Patch,
      $"{BoardPath(scope, board)}/cards/{cardId}",
      body);
  }

  private async Task<TestScope> CreateScopeAsync()
  {
    var registration = await _client.PostAsJsonAsync("/auth/register", new
    {
      name = "Workflow Tester",
      email = $"workflow-{Guid.NewGuid():N}@coordina.test",
      password = "Password123!"
    });
    registration.EnsureSuccessStatusCode();
    var auth = await registration.Content.ReadFromJsonAsync<AuthResponse>();
    Assert.NotNull(auth);

    var workspace = await SendForAsync<IdResponse>(
      auth.AccessToken,
      HttpMethod.Post,
      "/workspaces",
      new { name = "Workflow Team" });
    var project = await SendForAsync<IdResponse>(
      auth.AccessToken,
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/projects",
      new { name = "Workflow" });

    return new TestScope(auth.AccessToken, workspace.Id, project.Id);
  }

  private Task<BoardResponse> CreateBoardAsync(
    TestScope scope,
    string template,
    string[]? customListTitles)
  {
    return SendForAsync<BoardResponse>(
      scope.AccessToken,
      HttpMethod.Post,
      $"/workspaces/{scope.WorkspaceId}/projects/{scope.ProjectId}/boards",
      new { name = "Workflow board", template, customListTitles });
  }

  private Task<BoardResponse> SendForBoardAsync(
    TestScope scope,
    HttpMethod method,
    string path,
    object body)
  {
    return SendForAsync<BoardResponse>(scope.AccessToken, method, path, body);
  }

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
    request.Headers.Authorization = new AuthenticationHeaderValue(
      "Bearer",
      accessToken);

    if (body is not null)
    {
      request.Content = JsonContent.Create(body);
    }

    return await _client.SendAsync(request);
  }

  private sealed record TestScope(
    string AccessToken,
    string WorkspaceId,
    string ProjectId);

  private sealed record AuthResponse(string AccessToken);

  private sealed record IdResponse(string Id);

  private sealed record BoardResponse(
    string Id,
    IReadOnlyCollection<BoardListResponse> Lists);

  private sealed record BoardListResponse(
    string Id,
    string Title,
    int Position,
    string TaskStatus,
    IReadOnlyCollection<BoardCardResponse> Cards);

  private sealed record BoardCardResponse(
    string Id,
    string ListId,
    string Title,
    bool IsCompleted,
    DateTimeOffset? CompletedAt,
    int? EstimatedMinutes);
}
