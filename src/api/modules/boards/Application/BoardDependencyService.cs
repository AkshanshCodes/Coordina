using Coordina.Api.Core.Results;
using Coordina.Api.Core.Time;
using Coordina.Api.Modules.Boards.Application.Graph;
using Coordina.Api.Modules.Boards.Contracts;
using Coordina.Api.Modules.Projects.Application;

namespace Coordina.Api.Modules.Boards.Application;

public sealed class BoardDependencyService(
  IBoardStore boards,
  IProjectAccessGuard projectAccess,
  IGraphEngine<Guid> graphEngine,
  TimeProvider clock) : IBoardDependencyService
{
  public async Task<Result<BoardGraphResponse>> GetGraphAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid userId,
    BoardPlanningRequest planning,
    CancellationToken cancellationToken)
  {
    var preferences = BoardPlanningPreferences.Parse(planning, userId);

    if (!preferences.IsSuccess)
    {
      return preferences.Error;
    }

    var access = await projectAccess.AuthorizeReadAsync(
      workspaceId,
      projectId,
      userId,
      cancellationToken);

    if (!access.IsSuccess)
    {
      return access.Error;
    }

    var board = await boards.FindInProjectAsync(
      workspaceId,
      projectId,
      boardId,
      cancellationToken);

    return board is null
      ? Error.NotFound()
      : Result.Success(BoardGraphResponseMapper.ToResponse(
        BoardDependencyGraph.AnalyzeBoard(
          graphEngine,
          board,
          preferences.Value,
          clock.GetUtcToday())));
  }

  public async Task<Result<BoardCardDependencyAnalysisResponse>> GetCardDependencyAnalysisAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
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

    var board = await boards.FindInProjectAsync(
      workspaceId,
      projectId,
      boardId,
      cancellationToken);

    if (board is null)
    {
      return Error.NotFound();
    }

    var analysis = BoardDependencyGraph.AnalyzeCard(
      graphEngine,
      board,
      cardId);

    return analysis is null
      ? Error.NotFound()
      : Result.Success(BoardGraphResponseMapper.ToResponse(analysis));
  }

  public async Task<Result<BoardResponse>> AddCardDependencyAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    AddBoardCardDependencyRequest request,
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

    if (request.DependsOnCardId == Guid.Empty || request.DependsOnCardId == cardId)
    {
      return Error.Validation(
        nameof(request.DependsOnCardId),
        "Choose another card from this board.");
    }

    var boardSnapshot = await boards.FindInProjectAsync(
      workspaceId,
      projectId,
      boardId,
      cancellationToken);

    if (boardSnapshot is null)
    {
      return Error.NotFound();
    }

    var dependencyEvaluation = BoardDependencyGraph.EvaluateDependency(
      graphEngine,
      boardSnapshot,
      cardId,
      request.DependsOnCardId);

    switch (dependencyEvaluation.Status)
    {
      case GraphEdgeStatus.MissingSource or GraphEdgeStatus.MissingTarget:
        return Error.NotFound();
      case GraphEdgeStatus.Duplicate:
        return Result.Success(BoardResponseMapper.ToResponse(boardSnapshot));
      case GraphEdgeStatus.Cycle:
        return Error.Conflict("Adding this dependency would create a cycle.");
    }

    var board = await boards.AddCardDependencyAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      request.DependsOnCardId,
      clock.GetUtcNow(),
      cancellationToken);

    return board is null
      ? Error.NotFound()
      : Result.Success(BoardResponseMapper.ToResponse(board));
  }

  public async Task<Result> DeleteCardDependencyAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid dependsOnCardId,
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

    var deleted = await boards.DeleteCardDependencyAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      dependsOnCardId,
      clock.GetUtcNow(),
      cancellationToken);

    return deleted ? Result.Success() : Error.NotFound();
  }
}
