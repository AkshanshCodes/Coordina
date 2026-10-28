using System.Net;
using System.Net.Http.Json;
using Coordina.Api.Tests.Support;

namespace Coordina.Api.Tests;

public sealed class WorkspaceEndpointsTests(ApiTestApplicationFactory factory)
  : ApiTest(factory)
{
  [Fact]
  public async Task CreateWorkspace_AddsCreatorAsOwner()
  {
    var auth = await Api.RegisterAsync();

    var response = await Api.SendAsync(
      HttpMethod.Post,
      "/workspaces",
      auth.AccessToken,
      new { name = "Product Team" });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var workspace = await response.Content.ReadFromJsonAsync<WorkspaceResponse>();
    Assert.NotNull(workspace);
    Assert.Equal("Product Team", workspace.Name);
    Assert.Equal("OWNER", workspace.Role);
  }

  [Fact]
  public async Task GetWorkspace_ForNonMember_ReturnsNotFound()
  {
    var owner = await Api.RegisterAsync();
    var outsider = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Private Team");

    var response = await Api.SendAsync(
      HttpMethod.Get,
      $"/workspaces/{workspace.Id}",
      outsider.AccessToken);

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task DeleteWorkspace_ByMember_ReturnsForbidden()
  {
    var owner = await Api.RegisterAsync();
    var member = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Launch Team");
    var invite = await Api.CreateInviteAsync(owner.AccessToken, workspace.Id);
    await Api.JoinWorkspaceAsync(member.AccessToken, invite.Code);

    var response = await Api.SendAsync(
      HttpMethod.Delete,
      $"/workspaces/{workspace.Id}",
      member.AccessToken);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task DeleteWorkspace_ByOwner_RemovesWorkspace()
  {
    var owner = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Ops Team");

    var deleteResponse = await Api.SendAsync(
      HttpMethod.Delete,
      $"/workspaces/{workspace.Id}",
      owner.AccessToken);

    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

    var getResponse = await Api.SendAsync(
      HttpMethod.Get,
      $"/workspaces/{workspace.Id}",
      owner.AccessToken);

    Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
  }

  [Fact]
  public async Task Workspaces_WithoutToken_ReturnsUnauthorized()
  {
    var response = await Api.Http.GetAsync("/workspaces");

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task ListWorkspaces_ReturnsOnlyUserMemberships()
  {
    var firstUser = await Api.RegisterAsync();
    var secondUser = await Api.RegisterAsync();
    var shared = await Api.CreateWorkspaceAsync(firstUser.AccessToken, "Shared");
    var privateWorkspace = await Api.CreateWorkspaceAsync(
      firstUser.AccessToken,
      "Private");
    await Api.CreateWorkspaceAsync(secondUser.AccessToken, "Other");
    var invite = await Api.CreateInviteAsync(firstUser.AccessToken, shared.Id);
    await Api.JoinWorkspaceAsync(secondUser.AccessToken, invite.Code);

    var firstResponse = await Api.SendAsync(
      HttpMethod.Get,
      "/workspaces",
      firstUser.AccessToken);
    var secondResponse = await Api.SendAsync(
      HttpMethod.Get,
      "/workspaces",
      secondUser.AccessToken);

    Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
    Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

    var firstList = await firstResponse.Content
      .ReadFromJsonAsync<WorkspaceResponse[]>();
    var secondList = await secondResponse.Content
      .ReadFromJsonAsync<WorkspaceResponse[]>();

    Assert.NotNull(firstList);
    Assert.NotNull(secondList);
    Assert.Contains(firstList, workspace => workspace.Id == shared.Id);
    Assert.Contains(firstList, workspace => workspace.Id == privateWorkspace.Id);
    Assert.DoesNotContain(secondList, workspace => workspace.Id == privateWorkspace.Id);
    Assert.Contains(secondList, workspace => workspace.Id == shared.Id
      && workspace.Role == "MEMBER");
  }

  [Fact]
  public async Task JoinWorkspace_WithWorkspaceId_ReturnsValidationProblem()
  {
    var owner = await Api.RegisterAsync();
    var member = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Locked");

    var response = await Api.SendAsync(
      HttpMethod.Post,
      "/workspaces/join",
      member.AccessToken,
      new { inviteCode = workspace.Id });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task InvitationCode_IsOneTimeUse()
  {
    var owner = await Api.RegisterAsync();
    var firstMember = await Api.RegisterAsync();
    var secondMember = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Invite Only");
    var invite = await Api.CreateInviteAsync(owner.AccessToken, workspace.Id);

    var firstJoin = await Api.SendAsync(
      HttpMethod.Post,
      "/workspaces/join",
      firstMember.AccessToken,
      new { inviteCode = invite.Code });
    var secondJoin = await Api.SendAsync(
      HttpMethod.Post,
      "/workspaces/join",
      secondMember.AccessToken,
      new { inviteCode = invite.Code });

    Assert.Equal(HttpStatusCode.OK, firstJoin.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, secondJoin.StatusCode);
  }

  [Fact]
  public async Task CreateInvite_ByMember_ReturnsForbidden()
  {
    var owner = await Api.RegisterAsync();
    var member = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Owner Invites");
    var invite = await Api.CreateInviteAsync(owner.AccessToken, workspace.Id);
    await Api.JoinWorkspaceAsync(member.AccessToken, invite.Code);

    var response = await Api.SendAsync(
      HttpMethod.Post,
      $"/workspaces/{workspace.Id}/invites",
      member.AccessToken);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task Members_CanBeListedAndRemovedByOwner()
  {
    var owner = await Api.RegisterAsync();
    var member = await Api.RegisterAsync();
    var workspace = await Api.CreateWorkspaceAsync(owner.AccessToken, "Members");
    var invite = await Api.CreateInviteAsync(owner.AccessToken, workspace.Id);
    await Api.JoinWorkspaceAsync(member.AccessToken, invite.Code);

    var listResponse = await Api.SendAsync(
      HttpMethod.Get,
      $"/workspaces/{workspace.Id}/members",
      owner.AccessToken);

    Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

    var members = await listResponse.Content
      .ReadFromJsonAsync<WorkspaceMemberResponse[]>();

    Assert.NotNull(members);
    Assert.Contains(members, item => item.UserId == owner.User.Id
      && item.Role == "OWNER");
    Assert.Contains(members, item => item.UserId == member.User.Id
      && item.Role == "MEMBER");

    var removeResponse = await Api.SendAsync(
      HttpMethod.Delete,
      $"/workspaces/{workspace.Id}/members/{member.User.Id}",
      owner.AccessToken);

    Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

    var memberGetResponse = await Api.SendAsync(
      HttpMethod.Get,
      $"/workspaces/{workspace.Id}",
      member.AccessToken);

    Assert.Equal(HttpStatusCode.NotFound, memberGetResponse.StatusCode);
  }
}
