using Coordina.Api.Modules.Boards.Contracts;
using Coordina.Api.Modules.Projects.Application;

namespace Coordina.Api.Modules.Boards.Application;

public sealed class BoardListService(
  IBoardStore boards,
  IProjectAccessGuard projectAccess) : IBoardListService
{
  public async Task<BoardResult<BoardResponse>> CreateListAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    CreateBoardListRequest request,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var access = await projectAccess.FindWritableAccessAsync(
      workspaceId,
      projectId,
      userId,
      cancellationToken);

    if (access.Status != ProjectAccessStatus.Granted)
    {
      return BoardResults.AccessFailure<BoardResponse>(access.Status);
    }

    var errors = BoardRules.ValidateList(request.Title);
    var taskStatus = BoardRules.ParseListStatus(
      request.TaskStatus,
      out var statusError);

    if (statusError is not null)
    {
      errors["TaskStatus"] = [statusError];
    }

    if (errors.Count > 0)
    {
      return new BoardResult<BoardResponse>(
        BoardResultStatus.ValidationError,
        Errors: errors);
    }

    var board = await boards.CreateListAsync(
      workspaceId,
      projectId,
      boardId,
      request.Title!.Trim(),
      taskStatus,
      DateTimeOffset.UtcNow,
      cancellationToken);

    return board is null
      ? new BoardResult<BoardResponse>(BoardResultStatus.NotFound)
      : new BoardResult<BoardResponse>(
        BoardResultStatus.Success,
        BoardResponseMapper.ToResponse(board));
  }

  public async Task<BoardResult<BoardResponse>> UpdateListAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid listId,
    UpdateBoardListRequest request,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var access = await projectAccess.FindWritableAccessAsync(
      workspaceId,
      projectId,
      userId,
      cancellationToken);

    if (access.Status != ProjectAccessStatus.Granted)
    {
      return BoardResults.AccessFailure<BoardResponse>(access.Status);
    }

    var errors = BoardRules.ValidateList(request.Title);

    if (errors.Count > 0)
    {
      return new BoardResult<BoardResponse>(
        BoardResultStatus.ValidationError,
        Errors: errors);
    }

    var board = await boards.UpdateListAsync(
      workspaceId,
      projectId,
      boardId,
      listId,
      request.Title!.Trim(),
      DateTimeOffset.UtcNow,
      cancellationToken);

    return board is null
      ? new BoardResult<BoardResponse>(BoardResultStatus.NotFound)
      : new BoardResult<BoardResponse>(
        BoardResultStatus.Success,
        BoardResponseMapper.ToResponse(board));
  }
}
