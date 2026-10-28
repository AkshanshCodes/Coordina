namespace Coordina.Api.Modules.Boards.Contracts;

public sealed record BoardPlanningRequest(
  int? AvailableMinutes = null,
  string? Context = null,
  bool? PreferAssigned = null,
  bool? IncludeUnassigned = null);
