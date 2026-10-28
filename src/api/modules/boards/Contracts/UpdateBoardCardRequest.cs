namespace Coordina.Api.Modules.Boards.Contracts;

public sealed record UpdateBoardCardRequest(
  string? Title,
  string? Description,
  string? Priority,
  DateOnly? DueDate,
  bool ClearDueDate,
  string[]? Labels,
  Guid[]? AssigneeIds,
  bool? IsCompleted,
  int? EstimatedMinutes = null,
  bool ClearEstimatedMinutes = false);
