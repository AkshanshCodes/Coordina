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

  private static Task<string?> ListStatusAsync(PostgresDatabase database, Guid listId) =>
    database.ScalarAsync<string>(
      $"SELECT task_status FROM board_lists WHERE id = '{listId}'");

  private sealed record IdResponse(Guid Id);
}
