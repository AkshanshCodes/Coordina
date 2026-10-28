namespace Coordina.Api.Modules.Boards.Domain;

public sealed record ProjectBoardCard(
  Guid Id,
  Guid BoardId,
  Guid ListId,
  Guid ProjectId,
  Guid WorkspaceId,
  string Title,
  string? Description,
  BoardCardPriority? Priority,
  DateOnly? DueDate,
  int? EstimatedMinutes,
  IReadOnlyCollection<string> Labels,
  bool IsCompleted,
  DateTimeOffset? CompletedAt,
  ProjectBoardCardUser? CompletedBy,
  int Position,
  DateTimeOffset CreatedAt,
  DateTimeOffset UpdatedAt,
  IReadOnlyCollection<ProjectBoardCardAssignee> Assignees,
  IReadOnlyCollection<ProjectBoardCardComment> Comments,
  IReadOnlyCollection<ProjectBoardCardSubtask> Subtasks,
  IReadOnlyCollection<ProjectBoardCardDependency> Dependencies);

public sealed record ProjectBoardCardUser(
  Guid UserId,
  string? Name,
  string? Email);

public sealed record ProjectBoardCardComment(
  Guid Id,
  Guid CardId,
  ProjectBoardCardUser Author,
  string Body,
  DateTimeOffset CreatedAt);

public sealed record ProjectBoardCardSubtask(
  Guid Id,
  Guid CardId,
  string Title,
  bool IsCompleted,
  DateTimeOffset? CompletedAt,
  ProjectBoardCardUser? CompletedBy,
  int Position,
  DateTimeOffset CreatedAt,
  DateTimeOffset UpdatedAt);

public sealed record ProjectBoardCardDependency(
  Guid CardId,
  string Title,
  bool IsCompleted);
