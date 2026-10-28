using Coordina.Api.Core.Http;
using Coordina.Api.Core.Security;
using Coordina.Api.Modules.Projects.Application;
using Coordina.Api.Modules.Projects.Contracts;

namespace Coordina.Api.Modules.Projects;

public static class ProjectsEndpoints
{
  public static IEndpointRouteBuilder MapProjectsEndpoints(
    this IEndpointRouteBuilder app)
  {
    var group = app.MapApiGroup(
      "/workspaces/{workspaceId:guid}/projects",
      "Projects");

    group.MapPost("/", Create)
      .WithSummary("Create project")
      .WithDescription("Creates a project inside a workspace where the user is a member.");

    group.MapGet("/", List)
      .WithSummary("List projects")
      .WithDescription("Lists projects only within the requested workspace membership scope.");

    group.MapGet("/{projectId:guid}", Get)
      .WithSummary("Get project")
      .WithDescription("Returns a project only when it belongs to the requested workspace and user membership scope.");

    group.MapPatch("/{projectId:guid}", Update)
      .WithSummary("Update project")
      .WithDescription("Updates project metadata, owner, or lifecycle status when the user is allowed to manage the project.");

    group.MapDelete("/{projectId:guid}", Delete)
      .WithSummary("Delete project")
      .WithDescription("Archives a project in workspace scope. Workspace owners or project owners can archive.");

    group.MapDelete("/{projectId:guid}/permanent", PermanentlyDelete)
      .WithSummary("Permanently delete project")
      .WithDescription("Permanently deletes a project in workspace scope. Only workspace OWNER members can delete permanently.");

    return app;
  }

  private static async Task<IResult> Create(
    Guid workspaceId,
    CreateProjectRequest request,
    CurrentUser user,
    IProjectService projectService,
    CancellationToken cancellationToken)
  {
    var result = await projectService.CreateAsync(
      workspaceId,
      request,
      user.Id,
      cancellationToken);

    return result.ToCreatedResult(
      project => $"/workspaces/{workspaceId}/projects/{project.Id}");
  }

  private static async Task<IResult> List(
    Guid workspaceId,
    bool? includeArchived,
    bool? includeCompleted,
    CurrentUser user,
    IProjectService projectService,
    CancellationToken cancellationToken)
  {
    var result = await projectService.ListAsync(
      workspaceId,
      includeArchived == true,
      includeCompleted == true,
      user.Id,
      cancellationToken);

    return result.ToOkResult();
  }

  private static async Task<IResult> Get(
    Guid workspaceId,
    Guid projectId,
    CurrentUser user,
    IProjectService projectService,
    CancellationToken cancellationToken)
  {
    var result = await projectService.GetAsync(
      workspaceId,
      projectId,
      user.Id,
      cancellationToken);

    return result.ToOkResult();
  }

  private static async Task<IResult> Delete(
    Guid workspaceId,
    Guid projectId,
    CurrentUser user,
    IProjectService projectService,
    CancellationToken cancellationToken)
  {
    var result = await projectService.DeleteAsync(
      workspaceId,
      projectId,
      user.Id,
      cancellationToken);

    return result.ToNoContentResult();
  }

  private static async Task<IResult> Update(
    Guid workspaceId,
    Guid projectId,
    UpdateProjectRequest request,
    CurrentUser user,
    IProjectService projectService,
    CancellationToken cancellationToken)
  {
    var result = await projectService.UpdateAsync(
      workspaceId,
      projectId,
      request,
      user.Id,
      cancellationToken);

    return result.ToOkResult();
  }

  private static async Task<IResult> PermanentlyDelete(
    Guid workspaceId,
    Guid projectId,
    CurrentUser user,
    IProjectService projectService,
    CancellationToken cancellationToken)
  {
    var result = await projectService.PermanentlyDeleteAsync(
      workspaceId,
      projectId,
      user.Id,
      cancellationToken);

    return result.ToNoContentResult();
  }
}
