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

  private static string[] StatusesOf(BoardResponse board) =>
    board.Lists
      .OrderBy(list => list.Position)
      .Select(list => list.TaskStatus)
      .ToArray();

  private static string BoardPath(TestScope scope, BoardResponse board) =>
    $"/workspaces/{scope.WorkspaceId}/projects/{scope.ProjectId}/boards/{board.Id}";

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
    DateTimeOffset? CompletedAt);
}
