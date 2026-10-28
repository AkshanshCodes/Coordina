namespace Coordina.Api.Modules.Boards.Domain;

public enum BoardTaskStatus
{
  Todo,
  InProgress,
  Done
}

public static class BoardTaskStatusCodec
{
  public const string Todo = "TODO";
  public const string InProgress = "IN_PROGRESS";
  public const string Done = "DONE";

  public static string Format(BoardTaskStatus status) => status switch
  {
    BoardTaskStatus.Todo => Todo,
    BoardTaskStatus.InProgress => InProgress,
    BoardTaskStatus.Done => Done,
    _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
  };

  public static BoardTaskStatus Parse(string value) =>
    TryParse(value, out var status)
      ? status
      : throw new FormatException($"Unknown board task status '{value}'.");

  public static bool TryParse(string? value, out BoardTaskStatus status)
  {
    switch (value?.Trim().ToUpperInvariant())
    {
      case Todo:
        status = BoardTaskStatus.Todo;
        return true;
      case InProgress:
        status = BoardTaskStatus.InProgress;
        return true;
      case Done:
        status = BoardTaskStatus.Done;
        return true;
      default:
        status = default;
        return false;
    }
  }
}
