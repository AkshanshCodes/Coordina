using Coordina.Api.Modules.Boards.Domain;

namespace Coordina.Api.Modules.Boards.Application;

internal static class BoardWorkflow
{
  public static ProjectBoardList? FindFirstList(
    ProjectBoard board,
    BoardTaskStatus status) =>
    board.Lists
      .Where(list => list.TaskStatus == status)
      .OrderBy(list => list.Position)
      .FirstOrDefault();

  public static BoardCardCompletion CompletionIn(
    ProjectBoardList list,
    ProjectBoardCard? card,
    Guid userId,
    DateTimeOffset at)
  {
    if (list.TaskStatus != BoardTaskStatus.Done)
    {
      return BoardCardCompletion.Open;
    }

    return card is { IsCompleted: true }
      ? new BoardCardCompletion(true, card.CompletedAt, card.CompletedBy?.UserId)
      : new BoardCardCompletion(true, at, userId);
  }
}
