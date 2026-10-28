namespace Coordina.Api.Modules.Boards.Domain;

public sealed record ProjectBoardList(
  Guid Id,
  Guid BoardId,
  Guid ProjectId,
  Guid WorkspaceId,
  string Title,
  int Position,
  BoardTaskStatus TaskStatus,
  DateTimeOffset CreatedAt,
  DateTimeOffset UpdatedAt,
  IReadOnlyCollection<ProjectBoardCard> Cards);
