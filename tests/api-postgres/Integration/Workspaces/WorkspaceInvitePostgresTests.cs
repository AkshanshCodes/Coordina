using System.Net;
using Coordina.Api.Modules.Workspaces.Application;
using Coordina.Api.Postgres.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Coordina.Api.Postgres.Tests.Integration.Workspaces;

public sealed class WorkspaceInvitePostgresTests(PostgresContainerFixture postgres)
  : PostgresApiTest(postgres)
{
  [Fact]
  public async Task ConcurrentJoin_ConsumesInviteExactlyOnce()
  {
    var owner = await Api.RegisterAsync();
    var first = await Api.RegisterAsync();
    var second = await Api.RegisterAsync();
    var workspace = await Api.SendForAsync<WorkspaceResponse>(
      HttpMethod.Post,
      "/workspaces",
      owner.AccessToken,
      new { name = "Race Team" });
    var invite = await Api.SendForAsync<InviteResponse>(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/invites",
      owner.AccessToken);

    var responses = await Task.WhenAll(
      Api.SendAsync(HttpMethod.Post, "/workspaces/join", first.AccessToken,
        new { inviteCode = invite.Code }),
      Api.SendAsync(HttpMethod.Post, "/workspaces/join", second.AccessToken,
        new { inviteCode = invite.Code }));

    Assert.Single(responses, response => response.IsSuccessStatusCode);
    Assert.Single(responses, response =>
      response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict);
    Assert.Equal(2L, await Database.ScalarAsync<long>(
      $"SELECT COUNT(*) FROM workspace_members WHERE workspace_id = '{workspace.Id}'"));
    Assert.Equal(1L, await Database.ScalarAsync<long>(
      $"SELECT COUNT(*) FROM workspace_invites WHERE workspace_id = '{workspace.Id}' AND consumed_at IS NOT NULL"));
  }

  [Fact]
  public async Task MembershipInsertFailure_DoesNotConsumeInvite()
  {
    var owner = await Api.RegisterAsync();
    var member = await Api.RegisterAsync();
    var workspace = await Api.SendForAsync<WorkspaceResponse>(
      HttpMethod.Post,
      "/workspaces",
      owner.AccessToken,
      new { name = "Rollback Team" });
    await Api.SendForAsync<InviteResponse>(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/invites",
      owner.AccessToken);
    var inviteId = await Database.ScalarAsync<Guid>(
      $"SELECT id FROM workspace_invites WHERE workspace_id = '{workspace.Id}'");

    await using var scope = Factory.Services.CreateAsyncScope();
    var store = scope.ServiceProvider.GetRequiredService<IWorkspaceStore>();
    await store.AddMemberAsync(
      workspace.Id,
      member.Id,
      DateTimeOffset.UtcNow,
      CancellationToken.None);

    var outcome = await store.ConsumeInviteAsync(
      inviteId,
      member.Id,
      DateTimeOffset.UtcNow,
      CancellationToken.None);

    Assert.Equal(ConsumeInviteStatus.AlreadyMember, outcome.Status);
    Assert.Equal(0L, await Database.ScalarAsync<long>(
      $"SELECT COUNT(*) FROM workspace_invites WHERE id = '{inviteId}' AND consumed_at IS NOT NULL"));
  }

  private sealed record WorkspaceResponse(Guid Id);

  private sealed record InviteResponse(string Code);
}
