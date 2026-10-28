using System.Net;
using System.Net.Http.Json;
using Coordina.Api.Tests.Support;

namespace Coordina.Api.Tests;

public sealed class BoardEndpointsTests(ApiTestApplicationFactory factory)
  : ApiTest(factory)
{
  private static readonly string[] BackendLabels = ["backend"];
  private static readonly string[] DetailedLabels = ["backend", "review"];

  [Fact]
  public async Task CreateBoard_WithTemplate_GeneratesDefaultListsAndStarterCards()
  {
    var owner = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Board Team");
    var project = await Api.CreateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      "Delivery");

    var response = await Api.SendAsync(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards",
      owner.AccessToken,
      new
      {
        name = "Sprint board",
        template = "AGILE_SCRUM"
      });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var board = await response.Content.ReadFromJsonAsync<BoardResponse>();

    Assert.NotNull(board);
    Assert.Equal(project.Id, board.ProjectId);
    Assert.Equal("AGILE_SCRUM", board.Template);
    Assert.Equal(
      ["Backlog", "Sprint", "In Progress", "Review", "Done"],
      board.Lists.Select(list => list.Title).ToArray());
    Assert.Contains(
      board.Lists.Single(list => list.Title == "Backlog").Cards,
      card => card.Title == "Define user story"
        && card.Priority == "MEDIUM"
        && card.Labels.Contains("story"));
  }

  [Fact]
  public async Task CreateBoard_WithCustomTemplate_UsesUserListsWithoutStarterCards()
  {
    var owner = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Custom Team");
    var project = await Api.CreateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      "Custom Board");

    var board = await Api.CreateBoardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      "CUSTOM",
      ["Ideas", "Build", "Launch"]);

    Assert.Equal("CUSTOM", board.Template);
    Assert.Equal(
      ["Ideas", "Build", "Launch"],
      board.Lists.Select(list => list.Title).ToArray());
    Assert.All(board.Lists, list => Assert.Empty(list.Cards));
  }

  [Fact]
  public async Task GetDefaultBoard_ReturnsFullBoardStructure()
  {
    var owner = await Api.RegisterAsync();
    var member = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Full Team");
    var invite = await Api.CreateInviteAsync(owner.AccessToken, workspace.Id);
    await Api.JoinWorkspaceAsync(member.AccessToken, invite.Code);
    var project = await Api.CreateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      "Board");
    var board = await Api.CreateBoardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      "CUSTOM");
    var firstList = board.Lists.OrderBy(list => list.Position).First();
    await CreateCardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      board.Id,
      firstList.Id,
      "Assigned card",
      member.User.Id);

    var loaded = await Api.GetDefaultBoardAsync(
      member.AccessToken,
      workspace.Id,
      project.Id);

    var card = Assert.Single(loaded.Lists.First().Cards);
    Assert.Equal("Assigned card", card.Title);
    Assert.Equal("HIGH", card.Priority);
    Assert.Equal("backend", Assert.Single(card.Labels));
    Assert.Equal(member.User.Id, Assert.Single(card.Assignees).UserId);
  }

  [Fact]
  public async Task MoveCard_ChangesListForDragAndDrop()
  {
    var owner = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Move Team");
    var project = await Api.CreateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      "Board");
    var board = await Api.CreateBoardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      "CUSTOM");
    var lists = board.Lists.OrderBy(list => list.Position).ToArray();
    var created = await CreateCardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      board.Id,
      lists[0].Id,
      "Drag me");
    var card = created.Lists
      .SelectMany(list => list.Cards)
      .Single();

    var moved = await MoveCardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      board.Id,
      card.Id,
      lists[2].Id);

    Assert.Empty(moved.Lists.Single(list => list.Id == lists[0].Id).Cards);
    Assert.Contains(
      moved.Lists.Single(list => list.Id == lists[2].Id).Cards,
      card => card.Title == "Drag me" && card.ListId == lists[2].Id);
  }

  [Fact]
  public async Task UpdateCard_WithPartialPayload_PreservesExistingDetails()
  {
    var owner = await Api.RegisterAsync();
    var member = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Patch Team");
    var invite = await Api.CreateInviteAsync(owner.AccessToken, workspace.Id);
    await Api.JoinWorkspaceAsync(member.AccessToken, invite.Code);
    var project = await Api.CreateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      "Board");
    var board = await Api.CreateBoardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      "CUSTOM");
    var list = board.Lists.First();
    var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
    var created = await Api.SendAsync(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/lists/{list.Id}/cards",
      owner.AccessToken,
      new
      {
        title = "Detailed card",
        description = "Keep these details.",
        priority = "MEDIUM",
        dueDate,
        labels = DetailedLabels,
        assigneeIds = new[] { member.User.Id }
      });
    created.EnsureSuccessStatusCode();
    var withCard = await created.Content.ReadFromJsonAsync<BoardResponse>();

    Assert.NotNull(withCard);
    var card = withCard.Lists.Single(item => item.Id == list.Id).Cards.Single();

    var updateResponse = await Api.SendAsync(
      HttpMethod.Patch,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/cards/{card.Id}",
      owner.AccessToken,
      new { title = "Renamed card" });

    Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

    var updatedBoard = await updateResponse.Content.ReadFromJsonAsync<BoardResponse>();

    Assert.NotNull(updatedBoard);
    var updated = updatedBoard.Lists
      .Single(item => item.Id == list.Id)
      .Cards
      .Single();
    Assert.Equal("Renamed card", updated.Title);
    Assert.Equal("Keep these details.", updated.Description);
    Assert.Equal("MEDIUM", updated.Priority);
    Assert.Equal(dueDate, updated.DueDate);
    Assert.Equal(["backend", "review"], updated.Labels.ToArray());
    Assert.Equal(member.User.Id, Assert.Single(updated.Assignees).UserId);
  }

  [Fact]
  public async Task UpdateCard_WithNewAssignee_AssignsWorkspaceMember()
  {
    var owner = await Api.RegisterAsync();
    var member = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Assignment Team");
    var invite = await Api.CreateInviteAsync(owner.AccessToken, workspace.Id);
    await Api.JoinWorkspaceAsync(member.AccessToken, invite.Code);
    var project = await Api.CreateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      "Board");
    var board = await Api.CreateBoardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      "CUSTOM");
    var list = board.Lists.First();
    var created = await CreateCardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      board.Id,
      list.Id,
      "Needs ownership");
    var card = created.Lists.Single(item => item.Id == list.Id).Cards.Single();

    Assert.Empty(card.Assignees);

    var updateResponse = await Api.SendAsync(
      HttpMethod.Patch,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/cards/{card.Id}",
      owner.AccessToken,
      new { assigneeIds = new[] { member.User.Id } });

    Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

    var updatedBoard = await updateResponse.Content.ReadFromJsonAsync<BoardResponse>();

    Assert.NotNull(updatedBoard);
    var updated = updatedBoard.Lists
      .Single(item => item.Id == list.Id)
      .Cards
      .Single();
    Assert.Equal(member.User.Id, Assert.Single(updated.Assignees).UserId);
  }

  [Fact]
  public async Task UpdateCard_WithClearDueDate_RemovesExistingDueDate()
  {
    var owner = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Clear Date Team");
    var project = await Api.CreateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      "Board");
    var board = await Api.CreateBoardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      "CUSTOM");
    var list = board.Lists.First();
    var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
    var created = await Api.SendAsync(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/lists/{list.Id}/cards",
      owner.AccessToken,
      new
      {
        title = "Dated card",
        dueDate
      });
    created.EnsureSuccessStatusCode();
    var withCard = await created.Content.ReadFromJsonAsync<BoardResponse>();

    Assert.NotNull(withCard);
    var card = withCard.Lists.Single(item => item.Id == list.Id).Cards.Single();

    var updateResponse = await Api.SendAsync(
      HttpMethod.Patch,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/cards/{card.Id}",
      owner.AccessToken,
      new
      {
        title = "Dated card",
        clearDueDate = true
      });

    Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

    var updatedBoard = await updateResponse.Content.ReadFromJsonAsync<BoardResponse>();

    Assert.NotNull(updatedBoard);
    Assert.Null(updatedBoard.Lists.Single(item => item.Id == list.Id).Cards.Single().DueDate);
  }

  [Fact]
  public async Task CardCollaborationEndpoints_PersistCompletionCommentsSubtasksAndDependencies()
  {
    var owner = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Collab Team");
    var project = await Api.CreateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      "Board");
    var board = await Api.CreateBoardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      "CUSTOM");
    var list = board.Lists.First();
    var firstBoard = await CreateCardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      board.Id,
      list.Id,
      "Blocked by copy");
    var firstCard = firstBoard.Lists.First().Cards.First();
    var secondBoard = await CreateCardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      board.Id,
      list.Id,
      "Dependency target");
    var secondCard = secondBoard.Lists.First().Cards
      .Single(card => card.Title == "Dependency target");

    var completedResponse = await Api.SendAsync(
      HttpMethod.Patch,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/cards/{firstCard.Id}",
      owner.AccessToken,
      new
      {
        title = firstCard.Title,
        isCompleted = true
      });
    completedResponse.EnsureSuccessStatusCode();

    var commentResponse = await Api.SendAsync(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/cards/{firstCard.Id}/comments",
      owner.AccessToken,
      new { body = "Ship it after QA signs off." });
    commentResponse.EnsureSuccessStatusCode();

    var subtaskResponse = await Api.SendAsync(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/cards/{firstCard.Id}/subtasks",
      owner.AccessToken,
      new { title = "QA checklist" });
    subtaskResponse.EnsureSuccessStatusCode();
    var withSubtask = await subtaskResponse.Content.ReadFromJsonAsync<BoardResponse>();
    Assert.NotNull(withSubtask);
    var subtask = withSubtask.Lists
      .SelectMany(item => item.Cards)
      .Single(card => card.Id == firstCard.Id)
      .Subtasks
      .Single();

    var updateSubtaskResponse = await Api.SendAsync(
      HttpMethod.Patch,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/cards/{firstCard.Id}/subtasks/{subtask.Id}",
      owner.AccessToken,
      new { isCompleted = true });
    updateSubtaskResponse.EnsureSuccessStatusCode();

    var dependencyResponse = await Api.SendAsync(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/cards/{firstCard.Id}/dependencies",
      owner.AccessToken,
      new { dependsOnCardId = secondCard.Id });
    dependencyResponse.EnsureSuccessStatusCode();

    var loaded = await Api.GetDefaultBoardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id);
    var card = loaded.Lists
      .SelectMany(item => item.Cards)
      .Single(item => item.Id == firstCard.Id);

    Assert.True(card.IsCompleted);
    Assert.Equal(loaded.Lists.OrderBy(item => item.Position).Last().Id, card.ListId);
    Assert.Equal(owner.User.Id, card.CompletedBy?.UserId);
    Assert.Equal("Ship it after QA signs off.", Assert.Single(card.Comments).Body);
    Assert.Equal(owner.User.Id, Assert.Single(card.Comments).Author.UserId);
    Assert.True(Assert.Single(card.Subtasks).IsCompleted);
    Assert.Equal(owner.User.Id, Assert.Single(card.Subtasks).CompletedBy?.UserId);
    Assert.Equal(secondCard.Id, Assert.Single(card.Dependencies).CardId);
    Assert.Equal("Dependency target", Assert.Single(card.Dependencies).Title);
  }

  [Fact]
  public async Task AddCardDependency_WhenItWouldCreateCycle_ReturnsConflict()
  {
    var owner = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Graph Team");
    var project = await Api.CreateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      "Graph Board");
    var board = await Api.CreateBoardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      "CUSTOM");
    var list = board.Lists.First();
    var withFirstCard = await CreateCardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      board.Id,
      list.Id,
      "Ship API");
    var firstCard = withFirstCard.Lists.First().Cards.Single();
    var withSecondCard = await CreateCardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      board.Id,
      list.Id,
      "Prepare database");
    var secondCard = withSecondCard.Lists.First().Cards
      .Single(card => card.Title == "Prepare database");

    var dependencyResponse = await Api.SendAsync(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/cards/{firstCard.Id}/dependencies",
      owner.AccessToken,
      new { dependsOnCardId = secondCard.Id });
    dependencyResponse.EnsureSuccessStatusCode();

    var cycleResponse = await Api.SendAsync(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/cards/{secondCard.Id}/dependencies",
      owner.AccessToken,
      new { dependsOnCardId = firstCard.Id });

    Assert.Equal(HttpStatusCode.Conflict, cycleResponse.StatusCode);
  }

  [Fact]
  public async Task DependencyGraphEndpoints_ReturnPlanningSignals()
  {
    var owner = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Signals Team");
    var project = await Api.CreateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      "Signals Board");
    var board = await Api.CreateBoardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      "CUSTOM");
    var list = board.Lists.First();
    var withDatabase = await CreateCardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      board.Id,
      list.Id,
      "Database migration");
    var database = withDatabase.Lists.First().Cards.Single();
    var withApi = await CreateCardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      board.Id,
      list.Id,
      "API endpoint");
    var api = withApi.Lists.First().Cards
      .Single(card => card.Title == "API endpoint");
    var withUi = await CreateCardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      board.Id,
      list.Id,
      "Frontend panel");
    var ui = withUi.Lists.First().Cards
      .Single(card => card.Title == "Frontend panel");

    var apiDependencyResponse = await Api.SendAsync(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/cards/{api.Id}/dependencies",
      owner.AccessToken,
      new { dependsOnCardId = database.Id });
    apiDependencyResponse.EnsureSuccessStatusCode();

    var uiDependencyResponse = await Api.SendAsync(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/cards/{ui.Id}/dependencies",
      owner.AccessToken,
      new { dependsOnCardId = api.Id });
    uiDependencyResponse.EnsureSuccessStatusCode();

    var graphResponse = await Api.SendAsync(
      HttpMethod.Get,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/graph",
      owner.AccessToken);
    graphResponse.EnsureSuccessStatusCode();
    var graph = await graphResponse.Content.ReadFromJsonAsync<BoardGraphResponse>();

    Assert.NotNull(graph);
    Assert.True(graph.IsAcyclic);
    Assert.Equal(database.Id, Assert.Single(graph.ReadyCards).Id);
    Assert.Equal(
      [database.Id, api.Id, ui.Id],
      graph.DependencyOrder.Select(card => card.Id).ToArray());
    Assert.Equal(
      [database.Id, api.Id, ui.Id],
      graph.CriticalPath.Select(card => card.Id).ToArray());
    var nextCard = Assert.Single(graph.NextCards);
    Assert.Equal(database.Id, nextCard.Id);
    Assert.Equal(2, nextCard.TransitiveDependentCount);
    Assert.True(nextCard.IsCriticalPath);
    var firstPlanItem = graph.PlanItems.First();
    Assert.Equal(database.Id, firstPlanItem.Card.Id);
    Assert.True(firstPlanItem.IsActionable);
    Assert.Equal(0, firstPlanItem.BlockerCount);
    Assert.Contains("Ready now", firstPlanItem.Reasons);
    Assert.Contains("Critical path", firstPlanItem.Reasons);
    Assert.Contains("Unlocks 2", firstPlanItem.Reasons);

    var analysisResponse = await Api.SendAsync(
      HttpMethod.Get,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/cards/{api.Id}/dependency-analysis",
      owner.AccessToken);
    analysisResponse.EnsureSuccessStatusCode();
    var analysis = await analysisResponse.Content
      .ReadFromJsonAsync<BoardCardDependencyAnalysisResponse>();

    Assert.NotNull(analysis);
    Assert.False(analysis.IsStructurallyReady);
    Assert.False(analysis.IsUnblocked);
    Assert.True(analysis.IsOnCriticalPath);
    Assert.Equal(1, analysis.DependencyDepth);
    Assert.Equal(1, analysis.DependentDepth);
    Assert.Equal(1, analysis.TransitiveDependentCount);
    Assert.Equal(database.Id, Assert.Single(analysis.BlockingDependencies).Id);
    Assert.DoesNotContain(
      analysis.SuggestedDependencies,
      candidate => candidate.Id == ui.Id);
    Assert.Equal(ui.Id, Assert.Single(analysis.ImpactedDependents).Id);
    Assert.Equal(ui.Id, Assert.Single(analysis.DirectlyUnlockedDependents).Id);
  }

  [Fact]
  public async Task BoardMutations_OnCompletedProject_ReturnConflict()
  {
    var owner = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Readonly Team");
    var project = await Api.CreateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      "Completed board");
    var board = await Api.CreateBoardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      "CUSTOM");
    var list = board.Lists.First();
    await Api.UpdateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      new { status = "COMPLETED" });

    var response = await Api.SendAsync(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/lists/{list.Id}/cards",
      owner.AccessToken,
      new { title = "Should not write" });

    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
  }

  [Fact]
  public async Task BoardOperations_ForNonMember_ReturnNotFound()
  {
    var owner = await Api.RegisterAsync();
    var outsider = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Private Team");
    var project = await Api.CreateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      "Private");
    var board = await Api.CreateBoardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      "BASIC");
    var list = board.Lists.First();

    var getResponse = await Api.SendAsync(
      HttpMethod.Get,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/board",
      outsider.AccessToken);
    var createResponse = await Api.SendAsync(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/lists/{list.Id}/cards",
      outsider.AccessToken,
      new { title = "Nope" });

    Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, createResponse.StatusCode);
  }

  [Fact]
  public async Task BoardOperations_WithWrongProjectScope_ReturnNotFound()
  {
    var owner = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Scope Team");
    var firstProject = await Api.CreateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      "First");
    var secondProject = await Api.CreateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      "Second");
    var board = await Api.CreateBoardAsync(
      owner.AccessToken,
      workspace.Id,
      firstProject.Id,
      "CUSTOM");
    var list = board.Lists.First();

    var response = await Api.SendAsync(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/projects/{secondProject.Id}/boards/{board.Id}/lists/{list.Id}/cards",
      owner.AccessToken,
      new { title = "Leak" });

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task AssigningNonMember_ReturnsValidationProblem()
  {
    var owner = await Api.RegisterAsync();
    var outsider = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Assign Team");
    var project = await Api.CreateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      "Board");
    var board = await Api.CreateBoardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      "CUSTOM");
    var list = board.Lists.First();

    var response = await Api.SendAsync(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/lists/{list.Id}/cards",
      owner.AccessToken,
      new
      {
        title = "Invalid assignment",
        assigneeIds = new[] { outsider.User.Id }
      });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task CreatingCard_WithPastDueDate_ReturnsValidationProblem()
  {
    var owner = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Date Team");
    var project = await Api.CreateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      "Board");
    var board = await Api.CreateBoardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      "CUSTOM");
    var list = board.Lists.First();

    var response = await Api.SendAsync(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/lists/{list.Id}/cards",
      owner.AccessToken,
      new
      {
        title = "Past due",
        dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
      });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task Member_CannotDeleteCard_WhenNotProjectOwner()
  {
    var owner = await Api.RegisterAsync();
    var member = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Delete Team");
    var invite = await Api.CreateInviteAsync(owner.AccessToken, workspace.Id);
    await Api.JoinWorkspaceAsync(member.AccessToken, invite.Code);
    var project = await Api.CreateProjectAsync(
      owner.AccessToken,
      workspace.Id,
      "Board");
    var board = await Api.CreateBoardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      "CUSTOM");
    var list = board.Lists.First();
    var withCard = await CreateCardAsync(
      owner.AccessToken,
      workspace.Id,
      project.Id,
      board.Id,
      list.Id,
      "Protected");
    var card = withCard.Lists.First().Cards.First();

    var response = await Api.SendAsync(
      HttpMethod.Delete,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/cards/{card.Id}",
      member.AccessToken);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  private async Task<BoardResponse> CreateCardAsync(
    string accessToken,
    string workspaceId,
    string projectId,
    string boardId,
    string listId,
    string title,
    string? assigneeId = null)
  {
    var response = await Api.SendAsync(
      HttpMethod.Post,
      $"/workspaces/{workspaceId}/projects/{projectId}/boards/{boardId}/lists/{listId}/cards",
      accessToken,
      new
      {
        title,
        priority = "HIGH",
        labels = BackendLabels,
        assigneeIds = assigneeId is null ? [] : new[] { assigneeId }
      });

    response.EnsureSuccessStatusCode();

    var payload = await response.Content.ReadFromJsonAsync<BoardResponse>();

    Assert.NotNull(payload);
    return payload;
  }

  private async Task<BoardResponse> MoveCardAsync(
    string accessToken,
    string workspaceId,
    string projectId,
    string boardId,
    string cardId,
    string listId)
  {
    var response = await Api.SendAsync(
      HttpMethod.Patch,
      $"/workspaces/{workspaceId}/projects/{projectId}/boards/{boardId}/cards/{cardId}/move",
      accessToken,
      new { listId });

    response.EnsureSuccessStatusCode();

    var payload = await response.Content.ReadFromJsonAsync<BoardResponse>();

    Assert.NotNull(payload);
    return payload;
  }
}
