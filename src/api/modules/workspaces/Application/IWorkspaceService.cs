using Coordina.Api.Core.Results;
using Coordina.Api.Modules.Workspaces.Contracts;

namespace Coordina.Api.Modules.Workspaces.Application;

public interface IWorkspaceService
{
  Task<Result<WorkspaceResponse>> CreateAsync(
    CreateWorkspaceRequest request,
    Guid userId,
    CancellationToken cancellationToken);

  Task<IReadOnlyCollection<WorkspaceResponse>> ListAsync(
    Guid userId,
    CancellationToken cancellationToken);

  Task<Result<WorkspaceResponse>> GetAsync(
    Guid workspaceId,
    Guid userId,
    CancellationToken cancellationToken);

  Task<Result<WorkspaceResponse>> JoinAsync(
    JoinWorkspaceRequest request,
    Guid userId,
    CancellationToken cancellationToken);

  Task<Result<CreateWorkspaceInviteResponse>> CreateInviteAsync(
    Guid workspaceId,
    Guid userId,
    CancellationToken cancellationToken);

  Task<Result<IReadOnlyCollection<WorkspaceMemberResponse>>> ListMembersAsync(
    Guid workspaceId,
    Guid userId,
    CancellationToken cancellationToken);

  Task<Result> RemoveMemberAsync(
    Guid workspaceId,
    Guid memberUserId,
    Guid currentUserId,
    CancellationToken cancellationToken);

  Task<Result> DeleteAsync(
    Guid workspaceId,
    Guid userId,
    CancellationToken cancellationToken);
}
