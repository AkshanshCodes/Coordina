namespace Coordina.Api.Modules.Boards.Contracts;

public sealed record CreateBoardCardRequest(
  string? Title,
  string? Description,
  string? Priority,
  DateOnly? DueDate,
  string[]? Labels,
  Guid[]? AssigneeIds,
  int? EstimatedMinutes = null);
