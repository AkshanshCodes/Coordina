namespace Coordina.Api.Modules.Boards.Application.Graph;

public sealed record GraphWorkItem<TNode>(
  TNode Node,
  bool IsCompleted,
  int Priority,
  int DueDays,
  int SubtaskTotal,
  int SubtaskCompleted,
  int Position,
  int EstimatedMinutes = 0,
  GraphAssignment Assignment = GraphAssignment.Unassigned,
  bool IsInProgress = false,
  bool IsContextMatch = false)
  where TNode : notnull
{
  public const int NoDueDate = int.MaxValue;
}

public enum GraphAssignment
{
  Unassigned = 0,
  Requester = 1,
  Others = 2
}

public sealed record GraphPlanningOptions(
  int AvailableMinutes = 0,
  bool PreferAssigned = true,
  bool IncludeUnassigned = true)
{
  public static GraphPlanningOptions Default { get; } = new();
}
