using Coordina.Api.Modules.Boards.Domain;
using Coordina.Api.Shared.Text;
using Coordina.Api.Shared.Validation;

namespace Coordina.Api.Modules.Boards.Application;

internal static class BoardRules
{
  public const int MinEstimatedMinutes = 5;
  public const int MaxEstimatedMinutes = 2880;
  private const int MaxLabels = 12;
  private const int MaxCustomLists = 12;
  private const int MaxListTitleLength = 80;
  private const int MaxCardTitleLength = 180;

  private static BoardListSeed[] BuildListSeeds(
    BoardTemplate template,
    string[]? customListTitles)
  {
    if (template == BoardTemplate.Custom)
    {
      var titles = TextNormalizer.DistinctNonBlank(customListTitles, MaxCustomLists);

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

  /// <summary>
  /// Validates a new board and, when the template is valid, returns the lists
  /// it generates.
  /// </summary>
  public static ValidationErrors ValidateBoard(
    string? name,
    string? templateCode,
    string[]? customListTitles,
    out BoardTemplate? template,
    out BoardListSeed[] listSeeds)
  {
    var errors = new ValidationErrors()
      .Required("Name", name, "Board name", maxLength: 120)
      .RequiredCode("Template", templateCode, "Board template", out template);

    listSeeds = template is null
      ? []
      : BuildListSeeds(template.Value, customListTitles);

    if (template == BoardTemplate.Custom && listSeeds.Length == 0)
    {
      errors.Add("CustomListTitles", "Custom boards need at least one list title.");
    }
    else if (template == BoardTemplate.Custom
      && listSeeds.Any(list => list.Title.Length > MaxListTitleLength))
    {
      errors.Add(
        "CustomListTitles",
        $"Custom list titles must be {MaxListTitleLength} characters or fewer.");
    }

    return errors;
  }

  public static ValidationErrors ValidateList(string? title) =>
    new ValidationErrors()
      .Required("Title", title, "List title", maxLength: MaxListTitleLength);

  public static ValidationErrors ValidateCardTitle(string? title) =>
    new ValidationErrors()
      .Required("Title", title, "Card title", maxLength: MaxCardTitleLength);

  public static ValidationErrors ValidateCard(
    string? title,
    string? description,
    string? priorityCode,
    IReadOnlyCollection<string> labels,
    DateOnly? dueDate,
    int? estimatedMinutes,
    DateOnly today,
    out BoardCardPriority? priority) =>
    ValidateCardTitle(title)
      .MaxLength("Description", description, "Card description", 2000)
      .Code("Priority", priorityCode, "Card priority", out priority)
      .AddIf(
        labels.Any(label => label.Length > 32),
        "Labels",
        "Labels must be 32 characters or fewer.")
      .AddIf(
        dueDate < today,
        "DueDate",
        "Due date cannot be in the past.")
      .AddIf(
        estimatedMinutes is < MinEstimatedMinutes or > MaxEstimatedMinutes,
        "EstimatedMinutes",
        $"Effort must be between {MinEstimatedMinutes} and {MaxEstimatedMinutes} minutes.");

  public static string[] NormalizeLabels(string[]? labels) =>
    TextNormalizer.DistinctNonBlank(labels, MaxLabels);
}
