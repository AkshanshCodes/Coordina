using Coordina.Api.Core.Results;
using Coordina.Api.Modules.Projects.Domain;
using Coordina.Api.Modules.Workspaces.Domain;

namespace Coordina.Api.Modules.Projects.Application;

public sealed record ProjectAccess(WorkspaceRole Role, Project Project)
{
  public const string ReadOnlyMessage = "Completed projects are read-only.";

  public bool CanManage(Guid userId) =>
    Role == WorkspaceRole.Owner || Project.ProjectOwnerId == userId;
}

/// <summary>
/// Access rules other modules reuse for content that lives inside a project.
/// </summary>
public interface IProjectAccessGuard
{
  /// <summary>
  /// Grants access when the user belongs to the workspace and the project
  /// exists in it; fails with not found otherwise, so projects stay hidden.
  /// </summary>
  Task<Result<ProjectAccess>> AuthorizeReadAsync(
    Guid workspaceId,
    Guid projectId,
    Guid userId,
    CancellationToken cancellationToken);

  /// <summary>
  /// Same as <see cref="AuthorizeReadAsync"/>, and additionally fails with a
  /// conflict when the project is completed and therefore read-only.
  /// </summary>
  Task<Result<ProjectAccess>> AuthorizeWriteAsync(
    Guid workspaceId,
    Guid projectId,
    Guid userId,
    CancellationToken cancellationToken);
}
