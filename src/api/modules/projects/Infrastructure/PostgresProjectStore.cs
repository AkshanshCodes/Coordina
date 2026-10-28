using Coordina.Api.Core.Persistence;
using Coordina.Api.Modules.Auth.Infrastructure;
using Coordina.Api.Modules.Projects.Application;
using Coordina.Api.Modules.Projects.Domain;
using Microsoft.EntityFrameworkCore;

namespace Coordina.Api.Modules.Projects.Infrastructure;

public sealed class PostgresProjectStore(CoordinaDbContext dbContext)
  : IProjectStore
{
  private DbSet<AuthUserEntity> AuthUsers => dbContext.Set<AuthUserEntity>();

  private DbSet<ProjectEntity> Projects => dbContext.Set<ProjectEntity>();

  public async Task<Project> CreateAsync(
    Guid workspaceId,
    string name,
    string? description,
    string? key,
    string? icon,
    string? color,
    Guid projectOwnerId,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken)
  {
    var entity = new ProjectEntity
    {
      Id = Guid.NewGuid(),
      WorkspaceId = workspaceId,
      Name = name,
      Description = description,
      Key = key,
      Icon = icon,
      Color = color,
      ProjectOwnerId = projectOwnerId,
      Status = ProjectStatus.Active,
      CreatedAt = createdAt,
      UpdatedAt = createdAt
    };

    Projects.Add(entity);
    await dbContext.SaveChangesAsync(cancellationToken);

    var project = await FindInWorkspaceAsync(
      workspaceId,
      entity.Id,
      cancellationToken);

    return project ?? throw new InvalidOperationException(
      "Project was not created.");
  }

  public async Task<IReadOnlyCollection<Project>> ListForWorkspaceAsync(
    Guid workspaceId,
    bool includeArchived,
    bool includeCompleted,
    CancellationToken cancellationToken)
  {
    var query = Projects
      .AsNoTracking()
      .Where(project => project.WorkspaceId == workspaceId);

    if (!includeArchived)
    {
      query = query.Where(project => project.Status != ProjectStatus.Archived);
    }

    if (!includeCompleted)
    {
      query = query.Where(project => project.Status != ProjectStatus.Completed);
    }

    return await SelectProjects(query.OrderBy(project => project.Name))
      .ToArrayAsync(cancellationToken);
  }

  public async Task<Project?> FindInWorkspaceAsync(
    Guid workspaceId,
    Guid projectId,
    CancellationToken cancellationToken)
  {
    var query = Projects
      .AsNoTracking()
      .Where(project => project.WorkspaceId == workspaceId
        && project.Id == projectId);

    return await SelectProjects(query)
      .SingleOrDefaultAsync(cancellationToken);
  }

  public async Task<Project?> UpdateInWorkspaceAsync(
    Guid workspaceId,
    Guid projectId,
    ProjectUpdate update,
    DateTimeOffset updatedAt,
    CancellationToken cancellationToken)
  {
    var archivedAt = update.Status == ProjectStatus.Archived
      ? updatedAt
      : (DateTimeOffset?)null;

    var updated = await Projects
      .Where(project => project.WorkspaceId == workspaceId
        && project.Id == projectId)
      .ExecuteUpdateAsync(setters => setters
        .SetProperty(project => project.Name, update.Name)
        .SetProperty(project => project.Description, update.Description)
        .SetProperty(project => project.Key, update.Key)
        .SetProperty(project => project.Icon, update.Icon)
        .SetProperty(project => project.Color, update.Color)
        .SetProperty(project => project.ProjectOwnerId, update.ProjectOwnerId)
        .SetProperty(project => project.Status, update.Status)
        .SetProperty(project => project.UpdatedAt, updatedAt)
        .SetProperty(project => project.ArchivedAt, archivedAt),
        cancellationToken);

    return updated == 0
      ? null
      : await FindInWorkspaceAsync(workspaceId, projectId, cancellationToken);
  }

  public async Task<bool> ArchiveInWorkspaceAsync(
    Guid workspaceId,
    Guid projectId,
    DateTimeOffset archivedAt,
    CancellationToken cancellationToken)
  {
    var updated = await Projects
      .Where(project => project.WorkspaceId == workspaceId
        && project.Id == projectId)
      .ExecuteUpdateAsync(setters => setters
        .SetProperty(project => project.Status, ProjectStatus.Archived)
        .SetProperty(project => project.UpdatedAt, archivedAt)
        .SetProperty(project => project.ArchivedAt, archivedAt),
        cancellationToken);

    return updated > 0;
  }

  public async Task<bool> PermanentlyDeleteInWorkspaceAsync(
    Guid workspaceId,
    Guid projectId,
    CancellationToken cancellationToken)
  {
    var deleted = await Projects
      .Where(project => project.WorkspaceId == workspaceId
        && project.Id == projectId)
      .ExecuteDeleteAsync(cancellationToken);

    return deleted > 0;
  }

  private IQueryable<Project> SelectProjects(IQueryable<ProjectEntity> query)
  {
    return query
      .GroupJoin(
        AuthUsers.AsNoTracking(),
        project => project.ProjectOwnerId,
        user => user.Id,
        (project, owners) => new { project, owners })
      .SelectMany(
        row => row.owners.DefaultIfEmpty(),
        (row, owner) => new Project(
          row.project.Id,
          row.project.Name,
          row.project.Description,
          row.project.Key,
          row.project.Icon,
          row.project.Color,
          row.project.WorkspaceId,
          row.project.ProjectOwnerId,
          owner == null ? null : owner.Name,
          row.project.Status,
          row.project.CreatedAt,
          row.project.UpdatedAt,
          row.project.ArchivedAt));
  }
}
