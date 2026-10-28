using System.Security.Cryptography;
using System.Text;
using Coordina.Api.Modules.Workspaces.Contracts;
using Coordina.Api.Modules.Workspaces.Domain;

namespace Coordina.Api.Modules.Workspaces.Application;

public sealed class WorkspaceService(IWorkspaceStore workspaces)
  : IWorkspaceService
{
  private static readonly TimeSpan InviteLifetime = TimeSpan.FromDays(7);

  public async Task<WorkspaceResult<WorkspaceResponse>> CreateAsync(
    CreateWorkspaceRequest request,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var errors = ValidateName(request.Name);

    if (errors.Count > 0)
    {
      return new WorkspaceResult<WorkspaceResponse>(
        WorkspaceResultStatus.ValidationError,
        Errors: errors);
    }

    var workspace = await workspaces.CreateAsync(
      request.Name.Trim(),
      userId,
      DateTimeOffset.UtcNow,
      cancellationToken);

    return new WorkspaceResult<WorkspaceResponse>(
      WorkspaceResultStatus.Success,
      ToResponse(workspace));
  }

  public async Task<IReadOnlyCollection<WorkspaceResponse>> ListAsync(
    Guid userId,
    CancellationToken cancellationToken)
  {
    var userWorkspaces = await workspaces.ListForUserAsync(
      userId,
      cancellationToken);

    return userWorkspaces.Select(ToResponse).ToArray();
  }

  public async Task<WorkspaceResult<WorkspaceResponse>> GetAsync(
    Guid workspaceId,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var workspace = await workspaces.FindForUserAsync(
      workspaceId,
      userId,
      cancellationToken);

    return workspace is null
      ? new WorkspaceResult<WorkspaceResponse>(WorkspaceResultStatus.NotFound)
      : new WorkspaceResult<WorkspaceResponse>(
        WorkspaceResultStatus.Success,
        ToResponse(workspace));
  }

  public async Task<WorkspaceResult<WorkspaceResponse>> JoinAsync(
    JoinWorkspaceRequest request,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var inviteCode = NormalizeInviteCode(request.InviteCode);

    if (inviteCode.Length != 12)
    {
      return new WorkspaceResult<WorkspaceResponse>(
        WorkspaceResultStatus.ValidationError,
        Errors: new Dictionary<string, string[]>
        {
          [nameof(request.InviteCode)] = ["Enter a valid invitation code."]
        });
    }

    var invite = await workspaces.FindUsableInviteAsync(
      HashInviteCode(inviteCode),
      DateTimeOffset.UtcNow,
      cancellationToken);

    if (invite is null)
    {
      return new WorkspaceResult<WorkspaceResponse>(
        WorkspaceResultStatus.NotFound,
        Message: "Invitation code is invalid or has already been used.");
    }

    var existingMembership = await workspaces.FindForUserAsync(
      invite.WorkspaceId,
      userId,
      cancellationToken);

    if (existingMembership is not null)
    {
      return new WorkspaceResult<WorkspaceResponse>(
        WorkspaceResultStatus.Conflict,
        Message: "You already belong to this workspace.");
    }

    var outcome = await workspaces.ConsumeInviteAsync(
      invite.Id,
      userId,
      DateTimeOffset.UtcNow,
      cancellationToken);

    return outcome.Status switch
    {
      ConsumeInviteStatus.Consumed => new WorkspaceResult<WorkspaceResponse>(
        WorkspaceResultStatus.Success,
        ToResponse(outcome.Workspace!)),
      ConsumeInviteStatus.AlreadyMember => new WorkspaceResult<WorkspaceResponse>(
        WorkspaceResultStatus.Conflict,
        Message: "You already belong to this workspace."),
      _ => new WorkspaceResult<WorkspaceResponse>(
        WorkspaceResultStatus.NotFound,
        Message: "Invitation code is invalid or has already been used.")
    };
  }

  public async Task<WorkspaceResult<CreateWorkspaceInviteResponse>> CreateInviteAsync(
    Guid workspaceId,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var role = await workspaces.FindUserRoleAsync(
      workspaceId,
      userId,
      cancellationToken);

    if (role is null)
    {
      return new WorkspaceResult<CreateWorkspaceInviteResponse>(
        WorkspaceResultStatus.NotFound);
    }

    if (role != WorkspaceRole.Owner)
    {
      return new WorkspaceResult<CreateWorkspaceInviteResponse>(
        WorkspaceResultStatus.Forbidden,
        Message: "Only workspace owners can create invitation codes.");
    }

    var code = GenerateInviteCode();
    var now = DateTimeOffset.UtcNow;
    var expiresAt = now.Add(InviteLifetime);

    await workspaces.CreateInviteAsync(
      workspaceId,
      userId,
      HashInviteCode(code),
      now,
      expiresAt,
      cancellationToken);

    return new WorkspaceResult<CreateWorkspaceInviteResponse>(
      WorkspaceResultStatus.Success,
      new CreateWorkspaceInviteResponse(code, expiresAt));
  }

  public async Task<WorkspaceResult<IReadOnlyCollection<WorkspaceMemberResponse>>> ListMembersAsync(
    Guid workspaceId,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var role = await workspaces.FindUserRoleAsync(
      workspaceId,
      userId,
      cancellationToken);

    if (role is null)
    {
      return new WorkspaceResult<IReadOnlyCollection<WorkspaceMemberResponse>>(
        WorkspaceResultStatus.NotFound);
    }

    var members = await workspaces.ListMembersAsync(
      workspaceId,
      cancellationToken);

    return new WorkspaceResult<IReadOnlyCollection<WorkspaceMemberResponse>>(
      WorkspaceResultStatus.Success,
      members.Select(ToResponse).ToArray());
  }

  public async Task<WorkspaceResult<object>> RemoveMemberAsync(
    Guid workspaceId,
    Guid memberUserId,
    Guid currentUserId,
    CancellationToken cancellationToken)
  {
    var currentUserRole = await workspaces.FindUserRoleAsync(
      workspaceId,
      currentUserId,
      cancellationToken);

    if (currentUserRole is null)
    {
      return new WorkspaceResult<object>(WorkspaceResultStatus.NotFound);
    }

    if (currentUserRole != WorkspaceRole.Owner)
    {
      return new WorkspaceResult<object>(
        WorkspaceResultStatus.Forbidden,
        Message: "Only workspace owners can remove members.");
    }

    if (memberUserId == currentUserId)
    {
      return new WorkspaceResult<object>(
        WorkspaceResultStatus.ValidationError,
        Errors: new Dictionary<string, string[]>
        {
          [nameof(memberUserId)] = ["Owners cannot remove themselves."]
        });
    }

    var memberRole = await workspaces.FindUserRoleAsync(
      workspaceId,
      memberUserId,
      cancellationToken);

    if (memberRole is null)
    {
      return new WorkspaceResult<object>(WorkspaceResultStatus.NotFound);
    }

    if (memberRole == WorkspaceRole.Owner)
    {
      return new WorkspaceResult<object>(
        WorkspaceResultStatus.Forbidden,
        Message: "Owner members cannot be removed.");
    }

    var removed = await workspaces.RemoveMemberAsync(
      workspaceId,
      memberUserId,
      cancellationToken);

    return removed
      ? new WorkspaceResult<object>(WorkspaceResultStatus.Success)
      : new WorkspaceResult<object>(WorkspaceResultStatus.NotFound);
  }

  public async Task<WorkspaceResult<object>> DeleteAsync(
    Guid workspaceId,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var role = await workspaces.FindUserRoleAsync(
      workspaceId,
      userId,
      cancellationToken);

    if (role is null)
    {
      return new WorkspaceResult<object>(WorkspaceResultStatus.NotFound);
    }

    if (role != WorkspaceRole.Owner)
    {
      return new WorkspaceResult<object>(
        WorkspaceResultStatus.Forbidden,
        Message: "Only workspace owners can delete a workspace.");
    }

    await workspaces.DeleteAsync(workspaceId, cancellationToken);

    return new WorkspaceResult<object>(WorkspaceResultStatus.Success);
  }

  private static Dictionary<string, string[]> ValidateName(string name)
  {
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(name))
    {
      errors[nameof(name)] = ["Workspace name is required."];
    }
    else if (name.Trim().Length < 2)
    {
      errors[nameof(name)] = ["Workspace name must be at least 2 characters."];
    }
    else if (name.Trim().Length > 80)
    {
      errors[nameof(name)] = ["Workspace name must be 80 characters or fewer."];
    }

    return errors;
  }

  private static string GenerateInviteCode()
  {
    return Convert.ToHexString(RandomNumberGenerator.GetBytes(6));
  }

  private static string NormalizeInviteCode(string value) =>
    value.Trim().Replace("-", string.Empty).ToUpperInvariant();

  private static string HashInviteCode(string value)
  {
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
    return Convert.ToHexString(bytes);
  }

  private static WorkspaceResponse ToResponse(Workspace workspace)
  {
    return new WorkspaceResponse(
      workspace.Id.ToString(),
      workspace.Name,
      workspace.CurrentUserRole.ToString().ToUpperInvariant(),
      workspace.CreatedAt);
  }

  private static WorkspaceMemberResponse ToResponse(WorkspaceMember member)
  {
    return new WorkspaceMemberResponse(
      member.UserId.ToString(),
      member.Name,
      member.Email,
      member.Role.ToString().ToUpperInvariant(),
      member.JoinedAt);
  }
}
