using Coordina.Api.Core.Results;
using Coordina.Api.Modules.Boards.Contracts;
using Coordina.Api.Modules.Projects.Application;

namespace Coordina.Api.Modules.Boards.Application;

public sealed class BoardCatalogService(
  IBoardStore boards,
  IProjectAccessGuard projectAccess,
  TimeProvider clock) : IBoardCatalogService
{
  public async Task<Result<BoardResponse>> GetDefaultAsync(
    Guid workspaceId,
    Guid projectId,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var access = await projectAccess.AuthorizeReadAsync(
      workspaceId,
      projectId,
      userId,
      cancellationToken);

    if (!access.IsSuccess)
    {
      return access.Error;
    }

    var board = await boards.FindDefaultForProjectAsync(
      workspaceId,
      projectId,
      cancellationToken);

    return board is null
      ? Error.NotFound()
      : Result.Success(BoardResponseMapper.ToResponse(board));
  }

  public async Task<Result<BoardResponse>> CreateAsync(
    Guid workspaceId,
    Guid projectId,
    CreateBoardRequest request,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var access = await projectAccess.AuthorizeWriteAsync(
      workspaceId,
      projectId,
      userId,
      cancellationToken);

    if (!access.IsSuccess)
    {
      return access.Error;
    }

    var errors = BoardRules.ValidateBoard(
      request.Name,
      request.Template,
      request.CustomListTitles,
      out var template,
      out var listSeeds);

    if (errors.HasErrors)
    {
      return errors.ToError();
    }

    var board = await boards.CreateAsync(
      workspaceId,
      projectId,
      request.Name!.Trim(),
      template!.Value,
      listSeeds,
      clock.GetUtcNow(),
      cancellationToken);

    return Result.Success(BoardResponseMapper.ToResponse(board));
  }
}
