using Coordina.Api.Core.Http;
using Coordina.Api.Core.Security;
using Coordina.Api.Modules.Workspaces.Application;
using Coordina.Api.Modules.Workspaces.Contracts;

namespace Coordina.Api.Modules.Workspaces;

public static class WorkspacesEndpoints
{
  public static IEndpointRouteBuilder MapWorkspacesEndpoints(
    this IEndpointRouteBuilder app)
  {
    var group = app.MapApiGroup("/workspaces", "Workspaces");

    group.MapPost("/", Create)
      .WithSummary("Create workspace")
      .WithDescription("Creates a workspace and adds the authenticated user as OWNER.");

    group.MapGet("/", List)
      .WithSummary("List workspaces")
      .WithDescription("Lists only workspaces where the authenticated user is a member.");

    group.MapGet("/{workspaceId:guid}", Get)
      .WithSummary("Get workspace")
      .WithDescription("Returns a workspace only when the user belongs to it.");

    group.MapPost("/join", Join)
      .WithSummary("Join workspace")
      .WithDescription("Adds the authenticated user as MEMBER using a one-time invitation code.");

    group.MapPost("/{workspaceId:guid}/invites", CreateInvite)
      .WithSummary("Create invitation code")
      .WithDescription("Creates a one-time invitation code. Only OWNER members can invite.");

    group.MapGet("/{workspaceId:guid}/members", ListMembers)
      .WithSummary("List workspace members")
      .WithDescription("Lists members only when the authenticated user belongs to the workspace.");

    group.MapDelete("/{workspaceId:guid}/members/{memberUserId:guid}", RemoveMember)
      .WithSummary("Remove workspace member")
      .WithDescription("Removes a MEMBER from the workspace. Only OWNER members can remove members.");

    group.MapDelete("/{workspaceId:guid}", Delete)
      .WithSummary("Delete workspace")
      .WithDescription("Deletes a workspace. Only OWNER members can delete.");

    return app;
  }

  private static async Task<IResult> Create(
    CreateWorkspaceRequest request,
    CurrentUser user,
    IWorkspaceService workspaceService,
    CancellationToken cancellationToken)
  {
    var result = await workspaceService.CreateAsync(
      request,
      user.Id,
      cancellationToken);

    return result.ToCreatedResult(workspace => $"/workspaces/{workspace.Id}");
  }

  private static async Task<IResult> List(
    CurrentUser user,
    IWorkspaceService workspaceService,
    CancellationToken cancellationToken)
  {
    var workspaces = await workspaceService.ListAsync(user.Id, cancellationToken);

    return TypedResults.Ok(workspaces);
  }

  private static async Task<IResult> Get(
    Guid workspaceId,
    CurrentUser user,
    IWorkspaceService workspaceService,
    CancellationToken cancellationToken)
  {
    var result = await workspaceService.GetAsync(
      workspaceId,
      user.Id,
      cancellationToken);

    return result.ToOkResult();
  }

  private static async Task<IResult> Join(
    JoinWorkspaceRequest request,
    CurrentUser user,
    IWorkspaceService workspaceService,
    CancellationToken cancellationToken)
  {
    var result = await workspaceService.JoinAsync(
      request,
      user.Id,
      cancellationToken);

    return result.ToOkResult();
  }

  private static async Task<IResult> CreateInvite(
    Guid workspaceId,
    CurrentUser user,
    IWorkspaceService workspaceService,
    CancellationToken cancellationToken)
  {
    var result = await workspaceService.CreateInviteAsync(
      workspaceId,
      user.Id,
      cancellationToken);

    return result.ToCreatedResult(_ => $"/workspaces/{workspaceId}/invites");
  }

  private static async Task<IResult> ListMembers(
    Guid workspaceId,
    CurrentUser user,
    IWorkspaceService workspaceService,
    CancellationToken cancellationToken)
  {
    var result = await workspaceService.ListMembersAsync(
      workspaceId,
      user.Id,
      cancellationToken);

    return result.ToOkResult();
  }

  private static async Task<IResult> RemoveMember(
    Guid workspaceId,
    Guid memberUserId,
    CurrentUser user,
    IWorkspaceService workspaceService,
    CancellationToken cancellationToken)
  {
    var result = await workspaceService.RemoveMemberAsync(
      workspaceId,
      memberUserId,
      user.Id,
      cancellationToken);

    return result.ToNoContentResult();
  }

  private static async Task<IResult> Delete(
    Guid workspaceId,
    CurrentUser user,
    IWorkspaceService workspaceService,
    CancellationToken cancellationToken)
  {
    var result = await workspaceService.DeleteAsync(
      workspaceId,
      user.Id,
      cancellationToken);

    return result.ToNoContentResult();
  }
}
