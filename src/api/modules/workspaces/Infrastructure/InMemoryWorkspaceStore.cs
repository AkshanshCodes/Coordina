using System.Collections.Concurrent;
using Coordina.Api.Modules.Workspaces.Application;
using Coordina.Api.Modules.Workspaces.Domain;

namespace Coordina.Api.Modules.Workspaces.Infrastructure;

public sealed class InMemoryWorkspaceStore : IWorkspaceStore
{
  private readonly ConcurrentDictionary<Guid, WorkspaceRecord> _workspaces = new();
  private readonly ConcurrentDictionary<(Guid WorkspaceId, Guid UserId), WorkspaceRole>
    _memberships = new();
  private readonly ConcurrentDictionary<Guid, InviteRecord> _invites = new();
  private readonly Lock _inviteLock = new();

  public Task<Workspace> CreateAsync(
    string name,
    Guid ownerUserId,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var record = new WorkspaceRecord(Guid.NewGuid(), name, createdAt);
    _workspaces[record.Id] = record;
    _memberships[(record.Id, ownerUserId)] = WorkspaceRole.Owner;

    return Task.FromResult(ToWorkspace(record, WorkspaceRole.Owner));
  }

  public Task<IReadOnlyCollection<Workspace>> ListForUserAsync(
    Guid userId,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var workspaces = _memberships
      .Where(membership => membership.Key.UserId == userId)
      .Select(membership => _workspaces.TryGetValue(
        membership.Key.WorkspaceId,
        out var workspace)
          ? ToWorkspace(workspace, membership.Value)
          : null)
      .OfType<Workspace>()
      .OrderBy(workspace => workspace.Name)
      .ToArray();

    return Task.FromResult<IReadOnlyCollection<Workspace>>(workspaces);
  }

  public Task<Workspace?> FindForUserAsync(
    Guid workspaceId,
    Guid userId,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (!_workspaces.TryGetValue(workspaceId, out var workspace)
      || !_memberships.TryGetValue((workspaceId, userId), out var role))
    {
      return Task.FromResult<Workspace?>(null);
    }

    return Task.FromResult<Workspace?>(ToWorkspace(workspace, role));
  }

  public Task<WorkspaceRole?> FindUserRoleAsync(
    Guid workspaceId,
    Guid userId,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    return Task.FromResult(
      _memberships.TryGetValue((workspaceId, userId), out var role)
        ? role
        : (WorkspaceRole?)null);
  }

  public Task<Workspace?> FindByIdAsync(
    Guid workspaceId,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    return Task.FromResult(
      _workspaces.TryGetValue(workspaceId, out var workspace)
        ? ToWorkspace(workspace, WorkspaceRole.Member)
        : null);
  }

  public Task<IReadOnlyCollection<WorkspaceMember>> ListMembersAsync(
    Guid workspaceId,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var members = _memberships
      .Where(membership => membership.Key.WorkspaceId == workspaceId)
      .OrderBy(membership => membership.Value == WorkspaceRole.Owner ? 0 : 1)
      .Select(membership => new WorkspaceMember(
        membership.Key.UserId,
        null,
        null,
        membership.Value,
        _workspaces[workspaceId].CreatedAt))
      .ToArray();

    return Task.FromResult<IReadOnlyCollection<WorkspaceMember>>(members);
  }

  public Task<Workspace> AddMemberAsync(
    Guid workspaceId,
    Guid userId,
    DateTimeOffset joinedAt,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var workspace = _workspaces[workspaceId];
    _memberships.TryAdd((workspaceId, userId), WorkspaceRole.Member);

    return Task.FromResult(ToWorkspace(workspace, WorkspaceRole.Member));
  }

  public Task<WorkspaceInvite> CreateInviteAsync(
    Guid workspaceId,
    Guid createdByUserId,
    string codeHash,
    DateTimeOffset createdAt,
    DateTimeOffset expiresAt,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var invite = new InviteRecord(
      Guid.NewGuid(),
      workspaceId,
      codeHash,
      expiresAt,
      null);
    _invites[invite.Id] = invite;

    return Task.FromResult(ToInvite(invite));
  }

  public Task<WorkspaceInvite?> FindUsableInviteAsync(
    string codeHash,
    DateTimeOffset now,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var invite = _invites.Values.SingleOrDefault(invite =>
      invite.CodeHash == codeHash
      && invite.ConsumedAt is null
      && invite.ExpiresAt > now);

    return Task.FromResult(invite is null ? null : ToInvite(invite));
  }

  public Task<ConsumeInviteOutcome> ConsumeInviteAsync(
    Guid inviteId,
    Guid userId,
    DateTimeOffset consumedAt,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    lock (_inviteLock)
    {
      if (!_invites.TryGetValue(inviteId, out var invite)
        || invite.ConsumedAt is not null
        || invite.ExpiresAt <= consumedAt)
      {
        return Task.FromResult(
          new ConsumeInviteOutcome(ConsumeInviteStatus.Unavailable));
      }

      if (!_memberships.TryAdd((invite.WorkspaceId, userId), WorkspaceRole.Member))
      {
        return Task.FromResult(
          new ConsumeInviteOutcome(ConsumeInviteStatus.AlreadyMember));
      }

      _invites[inviteId] = invite with { ConsumedAt = consumedAt };

      return Task.FromResult(new ConsumeInviteOutcome(
        ConsumeInviteStatus.Consumed,
        ToWorkspace(_workspaces[invite.WorkspaceId], WorkspaceRole.Member)));
    }
  }

  public Task<bool> RemoveMemberAsync(
    Guid workspaceId,
    Guid userId,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (_memberships.TryGetValue((workspaceId, userId), out var role)
      && role == WorkspaceRole.Owner)
    {
      return Task.FromResult(false);
    }

    return Task.FromResult(_memberships.TryRemove((workspaceId, userId), out _));
  }

  public Task DeleteAsync(Guid workspaceId, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    _workspaces.TryRemove(workspaceId, out _);

    foreach (var membership in _memberships.Keys
      .Where(key => key.WorkspaceId == workspaceId)
      .ToArray())
    {
      _memberships.TryRemove(membership, out _);
    }

    return Task.CompletedTask;
  }

  private static Workspace ToWorkspace(
    WorkspaceRecord record,
    WorkspaceRole role) =>
    new(record.Id, record.Name, record.CreatedAt, role);

  private static WorkspaceInvite ToInvite(InviteRecord record) =>
    new(record.Id, record.WorkspaceId, record.ExpiresAt, record.ConsumedAt);

  private sealed record WorkspaceRecord(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt);

  private sealed record InviteRecord(
    Guid Id,
    Guid WorkspaceId,
    string CodeHash,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ConsumedAt);
}
