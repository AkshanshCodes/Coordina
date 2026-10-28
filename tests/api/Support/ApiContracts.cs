namespace Coordina.Api.Tests.Support;

// Wire contracts as the web client sees them. They are declared here, not
// borrowed from the API, so a renamed or retyped JSON field breaks a test
// instead of silently breaking the frontend.

public sealed record AuthResponse(
  string AccessToken,
  DateTimeOffset ExpiresAt,
  CurrentUserResponse User);

public sealed record CurrentUserResponse(
  string Id,
  string Email,
  string Name);

public sealed record WorkspaceResponse(
  string Id,
  string Name,
  string Role,
  DateTimeOffset CreatedAt);

public sealed record WorkspaceMemberResponse(
  string UserId,
  string? Name,
  string? Email,
  string Role,
  DateTimeOffset JoinedAt);

public sealed record CreateWorkspaceInviteResponse(
  string Code,
  DateTimeOffset ExpiresAt);

public sealed record ProjectResponse(
  string Id,
  string Name,
  string? Description,
  string? Key,
  string? Icon,
  string? Color,
  string WorkspaceId,
  string ProjectOwnerId,
  string? ProjectOwnerName,
  string Status,
  DateTimeOffset CreatedAt,
  DateTimeOffset UpdatedAt,
  DateTimeOffset? ArchivedAt);

public sealed record BoardResponse(
  string Id,
  string ProjectId,
  string Name,
  string Template,
  DateTimeOffset CreatedAt,
  DateTimeOffset UpdatedAt,
  IReadOnlyCollection<BoardListResponse> Lists);

public sealed record BoardListResponse(
  string Id,
  string BoardId,
  string Title,
  int Position,
  string TaskStatus,
  DateTimeOffset CreatedAt,
  DateTimeOffset UpdatedAt,
  IReadOnlyCollection<BoardCardResponse> Cards);

public sealed record BoardCardResponse(
  string Id,
  string BoardId,
  string ListId,
  string Title,
  string? Description,
  string? Priority,
  DateOnly? DueDate,
  int? EstimatedMinutes,
  IReadOnlyCollection<string> Labels,
  bool IsCompleted,
  DateTimeOffset? CompletedAt,
  BoardCardUserResponse? CompletedBy,
  int Position,
  DateTimeOffset CreatedAt,
  DateTimeOffset UpdatedAt,
  IReadOnlyCollection<BoardCardAssigneeResponse> Assignees,
  IReadOnlyCollection<BoardCardCommentResponse> Comments,
  IReadOnlyCollection<BoardCardSubtaskResponse> Subtasks,
  IReadOnlyCollection<BoardCardDependencyResponse> Dependencies);

public sealed record BoardCardUserResponse(
  string UserId,
  string? Name,
  string? Email);

public sealed record BoardCardAssigneeResponse(
  string UserId,
  string? Name,
  string? Email);

public sealed record BoardCardCommentResponse(
  string Id,
  string CardId,
  BoardCardUserResponse Author,
  string Body,
  DateTimeOffset CreatedAt);

public sealed record BoardCardSubtaskResponse(
  string Id,
  string CardId,
  string Title,
  bool IsCompleted,
  DateTimeOffset? CompletedAt,
  BoardCardUserResponse? CompletedBy,
  int Position,
  DateTimeOffset CreatedAt,
  DateTimeOffset UpdatedAt);

public sealed record BoardCardDependencyResponse(
  string CardId,
  string Title,
  bool IsCompleted);

public sealed record BoardGraphResponse(
  string BoardId,
  bool IsAcyclic,
  int AvailableMinutes,
  string? Context,
  IReadOnlyCollection<BoardGraphCardResponse> ReadyCards,
  IReadOnlyCollection<BoardGraphCardResponse> UnblockedCards,
  IReadOnlyCollection<BoardGraphCardResponse> DependencyOrder,
  IReadOnlyCollection<BoardGraphCardResponse> CriticalPath,
  IReadOnlyCollection<BoardGraphCardResponse> NextCards,
  IReadOnlyCollection<BoardGraphPlanItemResponse> PlanItems);

public sealed record BoardGraphCardResponse(
  string Id,
  string ListId,
  string Title,
  bool IsCompleted,
  int DependencyCount,
  int DependentCount,
  int DependencyDepth,
  int DependentDepth,
  int TransitiveDependentCount,
  bool IsCriticalPath);

public sealed record BoardGraphPlanItemResponse(
  BoardGraphCardResponse Card,
  int Score,
  bool IsActionable,
  int BlockerCount,
  int? EstimatedMinutes,
  bool FitsAvailableTime,
  bool IsAssignedToMe,
  bool IsContextMatch,
  bool IsInProgress,
  bool IsShortestAlternative,
  IReadOnlyCollection<string> Reasons);

public sealed record BoardCardDependencyAnalysisResponse(
  string CardId,
  bool IsStructurallyReady,
  bool IsUnblocked,
  bool IsOnCriticalPath,
  int DependencyDepth,
  int DependentDepth,
  int TransitiveDependentCount,
  IReadOnlyCollection<BoardGraphCardResponse> BlockingDependencies,
  IReadOnlyCollection<BoardGraphCardResponse> SuggestedDependencies,
  IReadOnlyCollection<BoardGraphCardResponse> ImpactedDependents,
  IReadOnlyCollection<BoardGraphCardResponse> DirectlyUnlockedDependents);
