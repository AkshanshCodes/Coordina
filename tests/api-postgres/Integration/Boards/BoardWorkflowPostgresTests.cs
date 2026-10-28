using Coordina.Api.Postgres.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Coordina.Api.Postgres.Tests.Integration.Boards;

public sealed class BoardWorkflowPostgresTests(PostgresContainerFixture postgres)
  : PostgresApiTest(postgres)
{
  private static readonly Guid WorkspaceId = Guid.Parse("10000000-0000-0000-0000-000000000001");
  private static readonly Guid ProjectId = Guid.Parse("20000000-0000-0000-0000-000000000001");
  private static readonly Guid SingleListBoardId = Guid.Parse("30000000-0000-0000-0000-000000000001");
  private static readonly Guid ScrumBoardId = Guid.Parse("30000000-0000-0000-0000-000000000002");
  private static readonly Guid SingleListId = Guid.Parse("40000000-0000-0000-0000-000000000001");
  private static readonly Guid BacklogListId = Guid.Parse("40000000-0000-0000-0000-000000000002");
  private static readonly Guid SprintListId = Guid.Parse("40000000-0000-0000-0000-000000000003");
  private static readonly Guid ReviewListId = Guid.Parse("40000000-0000-0000-0000-000000000004");
  private static readonly Guid DoneListId = Guid.Parse("40000000-0000-0000-0000-000000000005");
  private static readonly Guid CompletedReviewCardId = Guid.Parse("50000000-0000-0000-0000-000000000001");
  private static readonly Guid OpenDoneCardId = Guid.Parse("50000000-0000-0000-0000-000000000002");
  private static readonly string[] FlowListTitles = ["Open", "Doing", "Closed"];
  private static readonly string[] SingleListTitles = ["Only"];

  [Fact]
  public async Task Backfill_AssignsStatusesAndAlignsCompletion()
  {
    await using var database = await Postgres.CreateEmptyDatabaseAsync();
    await using (var dbContext = database.CreateDbContext())
    {
      await dbContext.GetService<IMigrator>().MigrateAsync("RemoveLegacyTasks");
    }

    await database.ExecuteAsync($"""
      INSERT INTO workspaces (id, name, created_at)
      VALUES ('{WorkspaceId}', 'Legacy', now());
      INSERT INTO projects (id, workspace_id, name, project_owner_id, status, created_at, updated_at)
      VALUES ('{ProjectId}', '{WorkspaceId}', 'Legacy', gen_random_uuid(), 'ACTIVE', now(), now());
      INSERT INTO boards (id, workspace_id, project_id, name, template, created_at, updated_at)
      VALUES
        ('{SingleListBoardId}', '{WorkspaceId}', '{ProjectId}', 'Single', 'CUSTOM', now(), now()),
        ('{ScrumBoardId}', '{WorkspaceId}', '{ProjectId}', 'Scrum', 'AGILE_SCRUM', now(), now());
      INSERT INTO board_lists (id, board_id, workspace_id, project_id, title, position, created_at, updated_at)
      VALUES
        ('{SingleListId}', '{SingleListBoardId}', '{WorkspaceId}', '{ProjectId}', 'Only', 0, now(), now()),
        ('{BacklogListId}', '{ScrumBoardId}', '{WorkspaceId}', '{ProjectId}', 'Backlog', 0, now(), now()),
        ('{SprintListId}', '{ScrumBoardId}', '{WorkspaceId}', '{ProjectId}', 'Sprint', 5, now(), now()),
        ('{ReviewListId}', '{ScrumBoardId}', '{WorkspaceId}', '{ProjectId}', 'Review', 7, now(), now()),
        ('{DoneListId}', '{ScrumBoardId}', '{WorkspaceId}', '{ProjectId}', 'Done', 9, now(), now());
      INSERT INTO board_cards (id, board_id, list_id, workspace_id, project_id, title, labels, is_completed, completed_at, position, created_at, updated_at)
      VALUES
        ('{CompletedReviewCardId}', '{ScrumBoardId}', '{ReviewListId}', '{WorkspaceId}', '{ProjectId}', 'Checked in review', ARRAY[]::text[], TRUE, now(), 0, now(), now()),
        ('{OpenDoneCardId}', '{ScrumBoardId}', '{DoneListId}', '{WorkspaceId}', '{ProjectId}', 'Open in done', ARRAY[]::text[], FALSE, NULL, 0, now(), now());
      """);

    await using (var dbContext = database.CreateDbContext())
    {
      await dbContext.Database.MigrateAsync();
    }

    Assert.Equal("TODO", await ListStatusAsync(database, SingleListId));
    Assert.Equal("TODO", await ListStatusAsync(database, BacklogListId));
    Assert.Equal("TODO", await ListStatusAsync(database, SprintListId));
    Assert.Equal("IN_PROGRESS", await ListStatusAsync(database, ReviewListId));
    Assert.Equal("DONE", await ListStatusAsync(database, DoneListId));
    Assert.False(await database.ScalarAsync<bool>(
      $"SELECT is_completed FROM board_cards WHERE id = '{CompletedReviewCardId}'"));
    Assert.True(await database.ScalarAsync<bool>(
      $"SELECT is_completed AND completed_at IS NOT NULL FROM board_cards WHERE id = '{OpenDoneCardId}'"));
  }

  [Fact]
  public async Task ListStatus_RoundTripsAndSurvivesRename()
  {
    var user = await Api.RegisterAsync();
    var workspace = await Api.SendForAsync<IdResponse>(
      HttpMethod.Post,
      "/workspaces",
      user.AccessToken,
      new { name = "Status Team" });
    var project = await Api.SendForAsync<IdResponse>(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/projects",
      user.AccessToken,
      new { name = "Status" });
    var board = await Api.SendForAsync<IdResponse>(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards",
      user.AccessToken,
      new { name = "Flow", template = "CUSTOM", customListTitles = FlowListTitles });
    var doingListId = await Database.ScalarAsync<Guid>(
      $"SELECT id FROM board_lists WHERE board_id = '{board.Id}' AND title = 'Doing'");

    await Api.SendForAsync<IdResponse>(
      HttpMethod.Patch,
      $"/workspaces/{workspace.Id}/projects/{project.Id}/boards/{board.Id}/lists/{doingListId}",
      user.AccessToken,
      new { title = "Done" });

    Assert.Equal("IN_PROGRESS", await ListStatusAsync(Database, doingListId));
  }

  [Fact]
  public async Task Completion_FollowsListStatusThroughRealStore()
  {
    var flow = await CreateFlowAsync("BASIC", null);
    var todo = flow.Board.Lists.Single(list => list.TaskStatus == "TODO");
    var doing = flow.Board.Lists.Single(list => list.TaskStatus == "IN_PROGRESS");
    var done = flow.Board.Lists.Single(list => list.TaskStatus == "DONE");
    var created = await Api.SendForAsync<BoardResponse>(
      HttpMethod.Post,
      $"{flow.BoardPath}/lists/{todo.Id}/cards",
      flow.AccessToken,
      new { title = "Real store card" });
    var cardId = created.Lists.SelectMany(list => list.Cards)
      .Single(card => card.Title == "Real store card").Id;

    await Api.SendForAsync<BoardResponse>(HttpMethod.Patch,
      $"{flow.BoardPath}/cards/{cardId}/move", flow.AccessToken, new { listId = done.Id });
    Assert.Equal((done.Id, true), await CardStateAsync(cardId));

    await Api.SendForAsync<BoardResponse>(HttpMethod.Patch,
      $"{flow.BoardPath}/cards/{cardId}/move", flow.AccessToken, new { listId = doing.Id });
    Assert.Equal((doing.Id, false), await CardStateAsync(cardId));

    await Api.SendForAsync<BoardResponse>(HttpMethod.Patch,
      $"{flow.BoardPath}/cards/{cardId}", flow.AccessToken, new { isCompleted = true });
    Assert.Equal((done.Id, true), await CardStateAsync(cardId));

    await Api.SendForAsync<BoardResponse>(HttpMethod.Patch,
      $"{flow.BoardPath}/cards/{cardId}", flow.AccessToken, new { isCompleted = false });
    Assert.Equal((todo.Id, false), await CardStateAsync(cardId));
  }

  [Fact]
  public async Task Complete_WithoutDoneList_LeavesRowUnchanged()
  {
    var flow = await CreateFlowAsync("CUSTOM", SingleListTitles);
    var created = await Api.SendForAsync<BoardResponse>(
      HttpMethod.Post,
      $"{flow.BoardPath}/lists/{flow.Board.Lists.Single().Id}/cards",
      flow.AccessToken,
      new { title = "Stuck" });
    var cardId = created.Lists.Single().Cards.Single().Id;

    using var response = await Api.SendAsync(
      HttpMethod.Patch,
      $"{flow.BoardPath}/cards/{cardId}",
      flow.AccessToken,
      new { title = "Changed", isCompleted = true });

    Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
    Assert.Equal("Stuck", await Database.ScalarAsync<string>(
      $"SELECT title FROM board_cards WHERE id = '{cardId}'"));
    Assert.False(await Database.ScalarAsync<bool>(
      $"SELECT is_completed FROM board_cards WHERE id = '{cardId}'"));
  }

  [Fact]
  public async Task ForeignScope_CannotReadOrMutateCards()
  {
    var owner = await CreateFlowAsync("BASIC", null);
    var todo = owner.Board.Lists.Single(list => list.TaskStatus == "TODO");
    var created = await Api.SendForAsync<BoardResponse>(
      HttpMethod.Post,
      $"{owner.BoardPath}/lists/{todo.Id}/cards",
      owner.AccessToken,
      new { title = "Private" });
    var cardId = created.Lists.SelectMany(list => list.Cards)
      .Single(card => card.Title == "Private").Id;
    var outsider = await CreateFlowAsync("BASIC", null);
    var foreignPath =
      $"/workspaces/{outsider.WorkspaceId}/projects/{outsider.ProjectId}/boards/{owner.Board.Id}";

    using var read = await Api.SendAsync(
      HttpMethod.Get, $"{foreignPath}/graph", outsider.AccessToken);
    using var write = await Api.SendAsync(
      HttpMethod.Patch, $"{foreignPath}/cards/{cardId}", outsider.AccessToken,
      new { title = "Hijacked" });
    using var move = await Api.SendAsync(
      HttpMethod.Patch, $"{foreignPath}/cards/{cardId}/move", outsider.AccessToken,
      new { listId = outsider.Board.Lists.First().Id });

    Assert.Equal(System.Net.HttpStatusCode.NotFound, read.StatusCode);
    Assert.Equal(System.Net.HttpStatusCode.NotFound, write.StatusCode);
    Assert.Equal(System.Net.HttpStatusCode.NotFound, move.StatusCode);
    Assert.Equal("Private", await Database.ScalarAsync<string>(
      $"SELECT title FROM board_cards WHERE id = '{cardId}'"));
    Assert.Equal(todo.Id, await Database.ScalarAsync<Guid>(
      $"SELECT list_id FROM board_cards WHERE id = '{cardId}'"));
  }

  [Fact]
  public async Task CardEffort_RoundTripsAndIsConstrained()
  {
    var flow = await CreateFlowAsync("BASIC", null);
    var todo = flow.Board.Lists.Single(list => list.TaskStatus == "TODO");
    var created = await Api.SendForAsync<BoardResponse>(
      HttpMethod.Post,
      $"{flow.BoardPath}/lists/{todo.Id}/cards",
      flow.AccessToken,
      new { title = "Sized", estimatedMinutes = 45 });
    var cardId = created.Lists.SelectMany(list => list.Cards)
      .Single(card => card.Title == "Sized").Id;

    Assert.Equal(45, await Database.ScalarAsync<int>(
      $"SELECT estimated_minutes FROM board_cards WHERE id = '{cardId}'"));

    await Api.SendForAsync<BoardResponse>(
      HttpMethod.Patch,
      $"{flow.BoardPath}/cards/{cardId}",
      flow.AccessToken,
      new { clearEstimatedMinutes = true });

    Assert.Null(await Database.ScalarAsync<int?>(
      $"SELECT estimated_minutes FROM board_cards WHERE id = '{cardId}'"));

    var violation = await Assert.ThrowsAsync<Npgsql.PostgresException>(() =>
      Database.ExecuteAsync(
        $"UPDATE board_cards SET estimated_minutes = 2881 WHERE id = '{cardId}'"));
    Assert.Equal("ck_board_cards_estimated_minutes", violation.ConstraintName);
  }

  private async Task<Flow> CreateFlowAsync(string template, string[]? customListTitles)
  {
    var user = await Api.RegisterAsync();
    var workspace = await Api.SendForAsync<IdResponse>(
      HttpMethod.Post, "/workspaces", user.AccessToken, new { name = "Flow Team" });
    var project = await Api.SendForAsync<IdResponse>(
      HttpMethod.Post, $"/workspaces/{workspace.Id}/projects", user.AccessToken,
      new { name = "Flow" });
    var board = await Api.SendForAsync<BoardResponse>(
      HttpMethod.Post, $"/workspaces/{workspace.Id}/projects/{project.Id}/boards",
      user.AccessToken, new { name = "Flow", template, customListTitles });

    return new Flow(user.AccessToken, workspace.Id, project.Id, board);
  }

  private async Task<(Guid ListId, bool IsCompleted)> CardStateAsync(Guid cardId)
  {
    var listId = await Database.ScalarAsync<Guid>(
      $"SELECT list_id FROM board_cards WHERE id = '{cardId}'");
    var isCompleted = await Database.ScalarAsync<bool>(
      $"SELECT is_completed FROM board_cards WHERE id = '{cardId}'");

    return (listId, isCompleted);
  }

  private static Task<string?> ListStatusAsync(PostgresDatabase database, Guid listId) =>
    database.ScalarAsync<string>(
      $"SELECT task_status FROM board_lists WHERE id = '{listId}'");

  private sealed record IdResponse(Guid Id);

  private sealed record Flow(
    string AccessToken,
    Guid WorkspaceId,
    Guid ProjectId,
    BoardResponse Board)
  {
    public string BoardPath =>
      $"/workspaces/{WorkspaceId}/projects/{ProjectId}/boards/{Board.Id}";
  }

  private sealed record BoardResponse(Guid Id, IReadOnlyCollection<ListResponse> Lists);

  private sealed record ListResponse(
    Guid Id,
    string TaskStatus,
    IReadOnlyCollection<CardResponse> Cards);

  private sealed record CardResponse(Guid Id, string Title);
}
