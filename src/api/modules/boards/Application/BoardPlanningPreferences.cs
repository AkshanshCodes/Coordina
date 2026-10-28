using System.Text.RegularExpressions;
using Coordina.Api.Core.Results;
using Coordina.Api.Modules.Boards.Contracts;
using Coordina.Api.Shared.Validation;

namespace Coordina.Api.Modules.Boards.Application;

internal sealed partial record BoardPlanningPreferences(
  Guid UserId,
  int AvailableMinutes,
  string? Context,
  bool PreferAssigned,
  bool IncludeUnassigned)
{
  private const int MaxContextLength = 32;

  public static Result<BoardPlanningPreferences> Parse(
    BoardPlanningRequest request,
    Guid userId)
  {
    var availableMinutes = request.AvailableMinutes ?? 0;
    var context = NormalizeContext(request.Context);
    var errors = new ValidationErrors()
      .AddIf(
        availableMinutes != 0
          && availableMinutes is < BoardRules.MinEstimatedMinutes
            or > BoardRules.MaxEstimatedMinutes,
        "AvailableMinutes",
        $"Available minutes must be 0 or between {BoardRules.MinEstimatedMinutes} and {BoardRules.MaxEstimatedMinutes}.")
      .AddIf(
        context?.Length > MaxContextLength,
        "Context",
        $"Context must be {MaxContextLength} characters or fewer.");

    return errors.HasErrors
      ? errors.ToError()
      : Result.Success(new BoardPlanningPreferences(
        userId,
        availableMinutes,
        context,
        request.PreferAssigned ?? true,
        request.IncludeUnassigned ?? true));
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
