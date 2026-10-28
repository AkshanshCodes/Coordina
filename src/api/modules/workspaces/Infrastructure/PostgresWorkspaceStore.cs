using Coordina.Api.Core.Persistence;
using Coordina.Api.Modules.Auth.Infrastructure;
using Coordina.Api.Modules.Workspaces.Application;
using Coordina.Api.Modules.Workspaces.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Coordina.Api.Modules.Workspaces.Infrastructure;

public sealed class PostgresWorkspaceStore(CoordinaDbContext dbContext)
  : IWorkspaceStore
{
  private DbSet<AuthUserEntity> AuthUsers => dbContext.Set<AuthUserEntity>();

  private DbSet<WorkspaceEntity> Workspaces => dbContext.Set<WorkspaceEntity>();

  private DbSet<WorkspaceMemberEntity> WorkspaceMembers => dbContext.Set<WorkspaceMemberEntity>();

  private DbSet<WorkspaceInviteEntity> WorkspaceInvites => dbContext.Set<WorkspaceInviteEntity>();

  public async Task<Workspace> CreateAsync(
    string name,
    Guid ownerUserId,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken)
  {
    var entity = new WorkspaceEntity
    {
      Id = Guid.NewGuid(),
      Name = name,
      CreatedAt = createdAt,
      Members =
      [
        new WorkspaceMemberEntity
        {
          Id = Guid.NewGuid(),
          UserId = ownerUserId,
          Role = WorkspaceRole.Owner,
          JoinedAt = createdAt
        }
      ]
    };

    Workspaces.Add(entity);
    await dbContext.SaveChangesAsync(cancellationToken);

    return new Workspace(
      entity.Id,
      entity.Name,
      entity.CreatedAt,
      WorkspaceRole.Owner);
  }

  public async Task<IReadOnlyCollection<Workspace>> ListForUserAsync(
    Guid userId,
    CancellationToken cancellationToken)
  {
    return await WorkspaceMembers
      .AsNoTracking()
      .Where(member => member.UserId == userId)
      .OrderBy(member => member.Workspace!.Name)
      .Select(member => new Workspace(
        member.WorkspaceId,
        member.Workspace!.Name,
        member.Workspace.CreatedAt,
        member.Role))
      .ToArrayAsync(cancellationToken);
  }

  public async Task<Workspace?> FindForUserAsync(
    Guid workspaceId,
    Guid userId,
    CancellationToken cancellationToken)
  {
    return await WorkspaceMembers
      .AsNoTracking()
      .Where(member => member.WorkspaceId == workspaceId
        && member.UserId == userId)
      .Select(member => new Workspace(
        member.WorkspaceId,
        member.Workspace!.Name,
        member.Workspace.CreatedAt,
        member.Role))
      .SingleOrDefaultAsync(cancellationToken);
  }

  public async Task<WorkspaceRole?> FindUserRoleAsync(
    Guid workspaceId,
    Guid userId,
    CancellationToken cancellationToken)
  {
    return await WorkspaceMembers
      .AsNoTracking()
      .Where(member => member.WorkspaceId == workspaceId
        && member.UserId == userId)
      .Select(member => (WorkspaceRole?)member.Role)
      .SingleOrDefaultAsync(cancellationToken);
  }

  public async Task<Workspace?> FindByIdAsync(
    Guid workspaceId,
    CancellationToken cancellationToken)
  {
    return await Workspaces
      .AsNoTracking()
      .Where(workspace => workspace.Id == workspaceId)
      .Select(workspace => new Workspace(
        workspace.Id,
        workspace.Name,
        workspace.CreatedAt,
        WorkspaceRole.Member))
      .SingleOrDefaultAsync(cancellationToken);
  }

  public async Task<IReadOnlyCollection<WorkspaceMember>> ListMembersAsync(
    Guid workspaceId,
    CancellationToken cancellationToken)
  {
    return await WorkspaceMembers
      .AsNoTracking()
      .Where(member => member.WorkspaceId == workspaceId)
      .Join(
        AuthUsers.AsNoTracking(),
        member => member.UserId,
        user => user.Id,
        (member, user) => new
        {
          member.UserId,
          user.Name,
          user.Email,
          member.Role,
          member.JoinedAt
        })
      .OrderBy(member => member.Role == WorkspaceRole.Owner ? 0 : 1)
      .ThenBy(member => member.Name)
      .Select(member => new WorkspaceMember(
        member.UserId,
        member.Name,
        member.Email,
        member.Role,
        member.JoinedAt))
      .ToArrayAsync(cancellationToken);
  }

  public async Task<Workspace> AddMemberAsync(
    Guid workspaceId,
    Guid userId,
    DateTimeOffset joinedAt,
    CancellationToken cancellationToken)
  {
    WorkspaceMembers.Add(new WorkspaceMemberEntity
    {
      Id = Guid.NewGuid(),
      WorkspaceId = workspaceId,
      UserId = userId,
      Role = WorkspaceRole.Member,
      JoinedAt = joinedAt
    });

    await dbContext.SaveChangesAsync(cancellationToken);

    var workspace = await FindForUserAsync(
      workspaceId,
      userId,
      cancellationToken);

    return workspace ?? throw new InvalidOperationException(
      "Workspace membership was not created.");
  }

  public async Task<WorkspaceInvite> CreateInviteAsync(
    Guid workspaceId,
    Guid createdByUserId,
    string codeHash,
    DateTimeOffset createdAt,
    DateTimeOffset expiresAt,
    CancellationToken cancellationToken)
  {
    var entity = new WorkspaceInviteEntity
    {
      Id = Guid.NewGuid(),
      WorkspaceId = workspaceId,
      CreatedByUserId = createdByUserId,
      CodeHash = codeHash,
      CreatedAt = createdAt,
      ExpiresAt = expiresAt
    };

    WorkspaceInvites.Add(entity);
    await dbContext.SaveChangesAsync(cancellationToken);

    return new WorkspaceInvite(
      entity.Id,
      entity.WorkspaceId,
      entity.ExpiresAt,
      entity.ConsumedAt);
  }

  public async Task<WorkspaceInvite?> FindUsableInviteAsync(
    string codeHash,
    DateTimeOffset now,
    CancellationToken cancellationToken)
  {
    return await WorkspaceInvites
      .AsNoTracking()
      .Where(invite => invite.CodeHash == codeHash
        && invite.ConsumedAt == null
        && invite.ExpiresAt > now)
      .Select(invite => new WorkspaceInvite(
        invite.Id,
        invite.WorkspaceId,
        invite.ExpiresAt,
        invite.ConsumedAt))
      .SingleOrDefaultAsync(cancellationToken);
  }

  public async Task<ConsumeInviteOutcome> ConsumeInviteAsync(
    Guid inviteId,
    Guid userId,
    DateTimeOffset consumedAt,
    CancellationToken cancellationToken)
  {
    var workspaceId = await WorkspaceInvites
      .Where(invite => invite.Id == inviteId)
      .Select(invite => (Guid?)invite.WorkspaceId)
      .SingleOrDefaultAsync(cancellationToken);

    if (workspaceId is null)
    {
      return new ConsumeInviteOutcome(ConsumeInviteStatus.Unavailable);
    }

    await using var transaction = await dbContext.Database
      .BeginTransactionAsync(cancellationToken);

    var consumed = await WorkspaceInvites
      .Where(candidate => candidate.Id == inviteId
        && candidate.ConsumedAt == null
        && candidate.ExpiresAt > consumedAt)
      .ExecuteUpdateAsync(updates => updates
        .SetProperty(candidate => candidate.ConsumedAt, consumedAt)
        .SetProperty(candidate => candidate.ConsumedByUserId, userId),
        cancellationToken);

    if (consumed == 0)
    {
      await transaction.RollbackAsync(cancellationToken);
      return new ConsumeInviteOutcome(ConsumeInviteStatus.Unavailable);
    }

    WorkspaceMembers.Add(new WorkspaceMemberEntity
    {
      Id = Guid.NewGuid(),
      WorkspaceId = workspaceId.Value,
      UserId = userId,
      Role = WorkspaceRole.Member,
      JoinedAt = consumedAt
    });

    try
    {
      await dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException exception)
      when (exception.InnerException is PostgresException
      {
        SqlState: PostgresErrorCodes.UniqueViolation
      })
    {
      await transaction.RollbackAsync(cancellationToken);
      dbContext.ChangeTracker.Clear();
      return new ConsumeInviteOutcome(ConsumeInviteStatus.AlreadyMember);
    }

    await transaction.CommitAsync(cancellationToken);

    var workspace = await FindForUserAsync(
      workspaceId.Value,
      userId,
      cancellationToken);

    return new ConsumeInviteOutcome(ConsumeInviteStatus.Consumed, workspace);
  }

  public async Task<bool> RemoveMemberAsync(
    Guid workspaceId,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var deleted = await WorkspaceMembers
      .Where(member => member.WorkspaceId == workspaceId
        && member.UserId == userId
        && member.Role != WorkspaceRole.Owner)
      .ExecuteDeleteAsync(cancellationToken);

    return deleted > 0;
  }

  public async Task DeleteAsync(
    Guid workspaceId,
    CancellationToken cancellationToken)
  {
    await Workspaces
      .Where(workspace => workspace.Id == workspaceId)
      .ExecuteDeleteAsync(cancellationToken);
  }
}
