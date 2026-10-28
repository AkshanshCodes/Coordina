using Coordina.Api.Core.Results;
using Coordina.Api.Modules.Projects.Domain;
using Coordina.Api.Modules.Workspaces.Application;

namespace Coordina.Api.Modules.Projects.Application;

public sealed class ProjectAccessGuard(
  IProjectStore projects,
  IWorkspaceStore workspaces) : IProjectAccessGuard
{
  public async Task<Result<ProjectAccess>> AuthorizeReadAsync(
    Guid workspaceId,
    Guid projectId,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var role = await workspaces.FindUserRoleAsync(
      workspaceId,
      userId,
      cancellationToken);

    if (role is null)
    {
      return Error.NotFound();
    }

    var project = await projects.FindInWorkspaceAsync(
      workspaceId,
      projectId,
      cancellationToken);

    return project is null
      ? Error.NotFound()
      : Result.Success(new ProjectAccess(role.Value, project));
  }

  public async Task<Result<ProjectAccess>> AuthorizeWriteAsync(
    Guid workspaceId,
    Guid projectId,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var access = await AuthorizeReadAsync(
      workspaceId,
      projectId,
      userId,
      cancellationToken);

    if (access.IsSuccess && access.Value.Project.Status == ProjectStatus.Completed)
    {
      return Error.Conflict(ProjectAccess.ReadOnlyMessage);
    }

    return access;
  }
}
