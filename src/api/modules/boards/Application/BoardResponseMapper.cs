using Coordina.Api.Modules.Boards.Contracts;
using Coordina.Api.Modules.Boards.Domain;

namespace Coordina.Api.Modules.Boards.Application;

internal static class BoardResponseMapper
{
  public static BoardResponse ToResponse(ProjectBoard board) =>
    new(
      board.Id,
      board.ProjectId,
      board.Name,
      ToApiTemplate(board.Template),
      board.CreatedAt,
      board.UpdatedAt,
      board.Lists
        .OrderBy(list => list.Position)
        .Select(ToResponse)
        .ToArray());

  private static BoardListResponse ToResponse(ProjectBoardList list) =>
    new(
      list.Id,
      list.BoardId,
      list.Title,
      list.Position,
      BoardTaskStatusCodec.Format(list.TaskStatus),
      list.CreatedAt,
      list.UpdatedAt,
      list.Cards
        .OrderBy(card => card.Position)
        .Select(ToResponse)
        .ToArray());

  private static BoardCardResponse ToResponse(ProjectBoardCard card) =>
    new(
      card.Id,
      card.BoardId,
      card.ListId,
      card.Title,
      card.Description,
      card.Priority?.ToString().ToUpperInvariant(),
      card.DueDate,
      card.Labels,
      card.IsCompleted,
      card.CompletedAt,
      card.CompletedBy is null ? null : ToUserResponse(card.CompletedBy),
      card.Position,
      card.CreatedAt,
      card.UpdatedAt,
      card.Assignees
        .Select(assignee => new BoardCardAssigneeResponse(
          assignee.UserId,
          assignee.Name,
          assignee.Email))
        .ToArray(),
      card.Comments
        .OrderBy(comment => comment.CreatedAt)
        .Select(comment => new BoardCardCommentResponse(
          comment.Id,
          comment.CardId,
          ToUserResponse(comment.Author),
          comment.Body,
          comment.CreatedAt))
        .ToArray(),
      card.Subtasks
        .OrderBy(subtask => subtask.Position)
        .Select(subtask => new BoardCardSubtaskResponse(
          subtask.Id,
          subtask.CardId,
          subtask.Title,
          subtask.IsCompleted,
          subtask.CompletedAt,
          subtask.CompletedBy is null ? null : ToUserResponse(subtask.CompletedBy),
          subtask.Position,
          subtask.CreatedAt,
          subtask.UpdatedAt))
        .ToArray(),
      card.Dependencies
        .Select(dependency => new BoardCardDependencyResponse(
          dependency.CardId,
          dependency.Title,
          dependency.IsCompleted))
        .ToArray());

  private static BoardCardUserResponse ToUserResponse(ProjectBoardCardUser user) =>
    new(user.UserId, user.Name, user.Email);

  private static string ToApiTemplate(BoardTemplate template)
  {
    return template switch
    {
      BoardTemplate.AgileScrum => "AGILE_SCRUM",
      BoardTemplate.BugTracking => "BUG_TRACKING",
      BoardTemplate.ProductRoadmap => "PRODUCT_ROADMAP",
      BoardTemplate.Custom => "CUSTOM",
      _ => "BASIC"
    };
  }
}
