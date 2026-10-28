using Coordina.Api.Core.Results;
using Coordina.Api.Modules.Projects.Contracts;
using Coordina.Api.Modules.Projects.Domain;
using Coordina.Api.Modules.Workspaces.Application;
using Coordina.Api.Modules.Workspaces.Domain;
using Coordina.Api.Shared.Enums;
using Coordina.Api.Shared.Text;
using Coordina.Api.Shared.Validation;

namespace Coordina.Api.Modules.Projects.Application;

public sealed class ProjectService(
  IProjectStore projects,
  IWorkspaceStore workspaces,
  IProjectAccessGuard projectAccess,
  TimeProvider clock) : IProjectService
{
  private const string OwnerOutsideWorkspaceMessage =
    "Project owner must belong to the workspace.";

  public async Task<Result<ProjectResponse>> CreateAsync(
    Guid workspaceId,
    CreateProjectRequest request,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var role = await FindRoleAsync(workspaceId, userId, cancellationToken);

    if (role is null)
    {
      return Error.NotFound();
    }

    var ownerId = request.ProjectOwnerId ?? userId;
    var ownerRole = await FindRoleAsync(workspaceId, ownerId, cancellationToken);

    if (ownerRole is null)
    {
      return Error.Validation(
        nameof(request.ProjectOwnerId),
        OwnerOutsideWorkspaceMessage);
    }

    var errors = ValidateFields(
      request.Name,
      request.Description,
      request.Key,
      request.Icon,
      request.Color);

    if (errors.HasErrors)
    {
      return errors.ToError();
    }

    var project = await projects.CreateAsync(
      workspaceId,
      request.Name.Trim(),
      TextNormalizer.TrimToNull(request.Description),
      NormalizeKey(request.Key),
      TextNormalizer.TrimToNull(request.Icon),
      TextNormalizer.TrimToNull(request.Color),
      ownerId,
      clock.GetUtcNow(),
      cancellationToken);

    return Result.Success(ToResponse(project));
  }

  public async Task<Result<IReadOnlyCollection<ProjectResponse>>> ListAsync(
    Guid workspaceId,
    bool includeArchived,
    bool includeCompleted,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var role = await FindRoleAsync(workspaceId, userId, cancellationToken);

    if (role is null)
    {
      return Error.NotFound();
    }

    var scopedProjects = await projects.ListForWorkspaceAsync(
      workspaceId,
      includeArchived,
      includeCompleted,
      cancellationToken);

    return Result.Success<IReadOnlyCollection<ProjectResponse>>(
      scopedProjects.Select(ToResponse).ToArray());
  }

  public async Task<Result<ProjectResponse>> GetAsync(
    Guid workspaceId,
    Guid projectId,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var access = await projectAccess.AuthorizeReadAsync(
      workspaceId,
      projectId,
      userId,
      cancellationToken);

    return access.IsSuccess
      ? Result.Success(ToResponse(access.Value.Project))
      : access.Error;
  }

  public async Task<Result<ProjectResponse>> UpdateAsync(
    Guid workspaceId,
    Guid projectId,
    UpdateProjectRequest request,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var access = await projectAccess.AuthorizeWriteAsync(
      workspaceId,
      projectId,
      userId,
      cancellationToken);

    if (!access.IsSuccess)
    {
      return access.Error;
    }

    if (!access.Value.CanManage(userId))
    {
      return Error.Forbidden(
        "Only workspace owners or project owners can update projects.");
    }

    var project = access.Value.Project;

    var statusErrors = new ValidationErrors()
      .Code<ProjectStatus>(nameof(request.Status), request.Status, "Project status", out var status);

    if (statusErrors.HasErrors)
    {
      return statusErrors.ToError();
    }

    var ownerId = request.ProjectOwnerId ?? project.ProjectOwnerId;
    var ownerRole = await FindRoleAsync(workspaceId, ownerId, cancellationToken);

    if (ownerRole is null)
    {
      return Error.Validation(
        nameof(request.ProjectOwnerId),
        OwnerOutsideWorkspaceMessage);
    }

    var update = new ProjectUpdate(
      TextNormalizer.TrimToNull(request.Name) ?? project.Name,
      request.Description is null
        ? project.Description
        : TextNormalizer.TrimToNull(request.Description),
      request.Key is null ? project.Key : NormalizeKey(request.Key),
      request.Icon is null ? project.Icon : TextNormalizer.TrimToNull(request.Icon),
      request.Color is null ? project.Color : TextNormalizer.TrimToNull(request.Color),
      ownerId,
      status ?? project.Status);

    var errors = ValidateFields(
      update.Name,
      update.Description,
      update.Key,
      update.Icon,
      update.Color);

    if (errors.HasErrors)
    {
      return errors.ToError();
    }

    var updatedProject = await projects.UpdateInWorkspaceAsync(
      workspaceId,
      projectId,
      update,
      clock.GetUtcNow(),
      cancellationToken);

    return updatedProject is null
      ? Error.NotFound()
      : Result.Success(ToResponse(updatedProject));
  }

  public async Task<Result> DeleteAsync(
    Guid workspaceId,
    Guid projectId,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var access = await projectAccess.AuthorizeWriteAsync(
      workspaceId,
      projectId,
      userId,
      cancellationToken);

    if (!access.IsSuccess)
    {
      return access.Error;
    }

    if (!access.Value.CanManage(userId))
    {
      return Error.Forbidden(
        "Only workspace owners or project owners can archive projects.");
    }

    var project = access.Value.Project;

    var archived = await projects.ArchiveInWorkspaceAsync(
      workspaceId,
      projectId,
      clock.GetUtcNow(),
      cancellationToken);

    return archived ? Result.Success() : Error.NotFound();
  }

  public async Task<Result> PermanentlyDeleteAsync(
    Guid workspaceId,
    Guid projectId,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var role = await FindRoleAsync(workspaceId, userId, cancellationToken);

    if (role is null)
    {
      return Error.NotFound();
    }

    if (role != WorkspaceRole.Owner)
    {
      return Error.Forbidden(
        "Only workspace owners can permanently delete projects.");
    }

    var deleted = await projects.PermanentlyDeleteInWorkspaceAsync(
      workspaceId,
      projectId,
      cancellationToken);

    return deleted ? Result.Success() : Error.NotFound();
  }

  private Task<WorkspaceRole?> FindRoleAsync(
    Guid workspaceId,
    Guid userId,
    CancellationToken cancellationToken) =>
    workspaces.FindUserRoleAsync(workspaceId, userId, cancellationToken);

  private static ValidationErrors ValidateFields(
    string? name,
    string? description,
    string? key,
    string? icon,
    string? color) =>
    new ValidationErrors()
      .Required("Name", name, "Project name", minLength: 2, maxLength: 100)
      .MaxLength("Description", description, "Project description", 500)
      .MaxLength("Key", key, "Project key", 12)
      .MaxLength("Icon", icon, "Project icon", 16)
      .MaxLength("Color", color, "Project color", 32);

  private static string? NormalizeKey(string? value) =>
    TextNormalizer.TrimToNull(value)?.ToUpperInvariant();

  private static ProjectResponse ToResponse(Project project) =>
    new(
      project.Id,
      project.Name,
      project.Description,
      project.Key,
      project.Icon,
      project.Color,
      project.WorkspaceId,
      project.ProjectOwnerId,
      project.ProjectOwnerName,
      EnumCode.Format(project.Status),
      project.CreatedAt,
      project.UpdatedAt,
      project.ArchivedAt);
}
