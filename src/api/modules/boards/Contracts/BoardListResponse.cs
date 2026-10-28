namespace Coordina.Api.Modules.Boards.Contracts;

public sealed record BoardListResponse(
  Guid Id,
  Guid BoardId,
  string Title,
  int Position,
  string TaskStatus,
  DateTimeOffset CreatedAt,
  DateTimeOffset UpdatedAt,
  IReadOnlyCollection<BoardCardResponse> Cards);
