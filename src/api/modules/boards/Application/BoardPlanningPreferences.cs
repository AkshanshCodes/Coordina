using System.Text.RegularExpressions;
using Coordina.Api.Modules.Boards.Contracts;

namespace Coordina.Api.Modules.Boards.Application;

internal sealed partial record BoardPlanningPreferences(
  Guid UserId,
  int AvailableMinutes,
  string? Context,
  bool PreferAssigned,
  bool IncludeUnassigned)
{
  private const int MaxContextLength = 32;

  public static BoardPlanningPreferences? Parse(
    BoardPlanningRequest request,
    Guid userId,
    out Dictionary<string, string[]> errors)
  {
    errors = [];
    var availableMinutes = request.AvailableMinutes ?? 0;

    if (availableMinutes != 0
      && availableMinutes is < BoardRules.MinEstimatedMinutes
        or > BoardRules.MaxEstimatedMinutes)
    {
      errors["AvailableMinutes"] = [
        $"Available minutes must be 0 or between {BoardRules.MinEstimatedMinutes} and {BoardRules.MaxEstimatedMinutes}."
      ];
    }

    var context = NormalizeContext(request.Context);

    if (context?.Length > MaxContextLength)
    {
      errors["Context"] = [
        $"Context must be {MaxContextLength} characters or fewer."
      ];
    }

    return errors.Count > 0
      ? null
      : new BoardPlanningPreferences(
        userId,
        availableMinutes,
        context,
        request.PreferAssigned ?? true,
        request.IncludeUnassigned ?? true);
  }

  public bool MatchesContext(IEnumerable<string> labels) =>
    Context is not null
    && labels.Any(label => string.Equals(
      NormalizeContext(label),
      Context,
      StringComparison.OrdinalIgnoreCase));

  private static string? NormalizeContext(string? value)
  {
    var normalized = WhitespacePattern()
      .Replace(value?.Trim().TrimStart('#') ?? string.Empty, "-");

    return normalized.Length == 0 ? null : normalized;
  }

  [GeneratedRegex(@"\s+")]
  private static partial Regex WhitespacePattern();
}
