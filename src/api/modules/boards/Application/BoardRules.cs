using Coordina.Api.Modules.Boards.Domain;

namespace Coordina.Api.Modules.Boards.Application;

internal static class BoardRules
{
  public const int MinEstimatedMinutes = 5;
  public const int MaxEstimatedMinutes = 2880;

  public static BoardTemplate? ParseTemplate(
    string? template,
    out string? error)
  {
    error = null;

    if (string.IsNullOrWhiteSpace(template))
    {
      error = "Board template is required.";
      return null;
    }

    var normalized = template.Replace("_", string.Empty, StringComparison.Ordinal);

    if (Enum.TryParse<BoardTemplate>(normalized, true, out var parsed))
    {
      return parsed;
    }

    error = "Board template must be BASIC, AGILE_SCRUM, BUG_TRACKING, PRODUCT_ROADMAP, or CUSTOM.";
    return null;
  }

  public static BoardCardPriority? ParsePriority(
    string? priority,
    out string? error)
  {
    error = null;

    if (string.IsNullOrWhiteSpace(priority))
    {
      return null;
    }

    if (Enum.TryParse<BoardCardPriority>(priority, true, out var parsed))
    {
      return parsed;
    }

    error = "Card priority must be LOW, MEDIUM, or HIGH.";
    return null;
  }

  public static BoardListSeed[] BuildListSeeds(
    BoardTemplate template,
    string[]? customListTitles)
  {
    if (template == BoardTemplate.Custom)
    {
      var titles = (customListTitles ?? [])
        .Select(NormalizeText)
        .OfType<string>()
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(12)
        .ToArray();

      return titles
        .Select((title, index) => new BoardListSeed(
          title,
          CustomListStatus(index, titles.Length),
          []))
        .ToArray();
    }

    return BoardTemplateCatalog.GetLists(template)
      .Select(list => new BoardListSeed(
        list.Title,
        list.TaskStatus,
        list.Cards
          .Select(card => new BoardCardSeed(
            card.Title,
            card.Description,
            card.Priority,
            card.Labels))
          .ToArray()))
      .ToArray();
  }

  public static BoardTaskStatus ParseListStatus(
    string? taskStatus,
    out string? error)
  {
    error = null;

    if (string.IsNullOrWhiteSpace(taskStatus))
    {
      return BoardTaskStatus.Todo;
    }

    if (BoardTaskStatusCodec.TryParse(taskStatus, out var parsed))
    {
      return parsed;
    }

    error = "List task status must be TODO, IN_PROGRESS, or DONE.";
    return BoardTaskStatus.Todo;
  }

  private static BoardTaskStatus CustomListStatus(int index, int count)
  {
    if (index == 0)
    {
      return BoardTaskStatus.Todo;
    }

    return index == count - 1
      ? BoardTaskStatus.Done
      : BoardTaskStatus.InProgress;
  }

  public static Dictionary<string, string[]> ValidateBoard(
    string? name,
    BoardTemplate? template,
    string? templateError,
    BoardListSeed[] listSeeds)
  {
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(name))
    {
      errors["Name"] = ["Board name is required."];
    }
    else if (name.Trim().Length > 120)
    {
      errors["Name"] = ["Board name must be 120 characters or fewer."];
    }

    if (templateError is not null)
    {
      errors["Template"] = [templateError];
    }

    if (template == BoardTemplate.Custom && listSeeds.Length == 0)
    {
      errors["CustomListTitles"] = [
        "Custom boards need at least one list title."
      ];
    }
    else if (template == BoardTemplate.Custom
      && listSeeds.Any(list => list.Title.Length > 80))
    {
      errors["CustomListTitles"] = [
        "Custom list titles must be 80 characters or fewer."
      ];
    }

    return errors;
  }

  public static Dictionary<string, string[]> ValidateList(string? title)
  {
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(title))
    {
      errors["Title"] = ["List title is required."];
    }
    else if (title.Trim().Length > 80)
    {
      errors["Title"] = ["List title must be 80 characters or fewer."];
    }

    return errors;
  }

  public static Dictionary<string, string[]> ValidateCard(
    string? title,
    string? description,
    string? priorityError,
    IReadOnlyCollection<string> labels,
    DateOnly? dueDate,
    int? estimatedMinutes = null)
  {
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(title))
    {
      errors["Title"] = ["Card title is required."];
    }
    else if (title.Trim().Length > 180)
    {
      errors["Title"] = ["Card title must be 180 characters or fewer."];
    }

    if (description?.Trim().Length > 2000)
    {
      errors["Description"] = [
        "Card description must be 2000 characters or fewer."
      ];
    }

    if (priorityError is not null)
    {
      errors["Priority"] = [priorityError];
    }

    if (labels.Any(label => label.Length > 32))
    {
      errors["Labels"] = ["Labels must be 32 characters or fewer."];
    }

    if (dueDate is not null
      && dueDate.Value < DateOnly.FromDateTime(DateTime.UtcNow))
    {
      errors["DueDate"] = ["Due date cannot be in the past."];
    }

    if (estimatedMinutes is < MinEstimatedMinutes or > MaxEstimatedMinutes)
    {
      errors["EstimatedMinutes"] = [
        $"Effort must be between {MinEstimatedMinutes} and {MaxEstimatedMinutes} minutes."
      ];
    }

    return errors;
  }

  public static string[] NormalizeLabels(string[]? labels)
  {
    return (labels ?? [])
      .Select(NormalizeText)
      .OfType<string>()
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .Take(12)
      .ToArray();
  }

  public static string? NormalizeText(string? value)
  {
    var normalized = value?.Trim();
    return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
  }
}
