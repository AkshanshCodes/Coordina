using Coordina.Api.Core.Results;
using Coordina.Api.Modules.Projects.Contracts;

namespace Coordina.Api.Modules.Projects.Application;

public interface IProjectService
{
  Task<Result<ProjectResponse>> CreateAsync(
    Guid workspaceId,
    CreateProjectRequest request,
    Guid userId,
    CancellationToken cancellationToken);

  Task<Result<IReadOnlyCollection<ProjectResponse>>> ListAsync(
    Guid workspaceId,
    bool includeArchived,
    bool includeCompleted,
    Guid userId,
    CancellationToken cancellationToken);

  Task<Result<ProjectResponse>> GetAsync(
    Guid workspaceId,
    Guid projectId,
    Guid userId,
    CancellationToken cancellationToken);

  Task<Result> DeleteAsync(
    Guid workspaceId,
    Guid projectId,
    Guid userId,
    CancellationToken cancellationToken);

  Task<Result<ProjectResponse>> UpdateAsync(
    Guid workspaceId,
    Guid projectId,
    UpdateProjectRequest request,
    Guid userId,
    CancellationToken cancellationToken);

  Task<Result> PermanentlyDeleteAsync(
    Guid workspaceId,
    Guid projectId,
    Guid userId,
    CancellationToken cancellationToken);
}
