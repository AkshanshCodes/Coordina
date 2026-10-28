namespace Coordina.Api.Modules.Boards.Contracts;

public sealed record BoardCardResponse(
  Guid Id,
  Guid BoardId,
  Guid ListId,
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
  Guid UserId,
  string? Name,
  string? Email);

public sealed record BoardCardCommentResponse(
  Guid Id,
  Guid CardId,
  BoardCardUserResponse Author,
  string Body,
  DateTimeOffset CreatedAt);

public sealed record BoardCardSubtaskResponse(
  Guid Id,
  Guid CardId,
  string Title,
  bool IsCompleted,
  DateTimeOffset? CompletedAt,
  BoardCardUserResponse? CompletedBy,
  int Position,
  DateTimeOffset CreatedAt,
  DateTimeOffset UpdatedAt);

public sealed record BoardCardDependencyResponse(
  Guid CardId,
  string Title,
  bool IsCompleted);
