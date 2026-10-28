using Coordina.Api.Modules.Boards.Application.Graph;
using Coordina.Api.Modules.Boards.Contracts;
using Coordina.Api.Modules.Projects.Application;

namespace Coordina.Api.Modules.Boards.Application;

public sealed class BoardDependencyService(
  IBoardStore boards,
  IProjectAccessGuard projectAccess,
  IGraphEngine<Guid> graphEngine) : IBoardDependencyService
{
  public async Task<BoardResult<BoardGraphResponse>> GetGraphAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid userId,
    BoardPlanningRequest planning,
    CancellationToken cancellationToken)
  {
    var preferences = BoardPlanningPreferences.Parse(
      planning,
      userId,
      out var errors);

    if (preferences is null)
    {
      return new BoardResult<BoardGraphResponse>(
        BoardResultStatus.ValidationError,
        Errors: errors);
    }

    var access = await projectAccess.FindAccessAsync(
      workspaceId,
      projectId,
      userId,
      cancellationToken);

    if (access is null)
    {
      return new BoardResult<BoardGraphResponse>(BoardResultStatus.NotFound);
    }

    var board = await boards.FindInProjectAsync(
      workspaceId,
      projectId,
      boardId,
      cancellationToken);

    return board is null
      ? new BoardResult<BoardGraphResponse>(BoardResultStatus.NotFound)
      : new BoardResult<BoardGraphResponse>(
        BoardResultStatus.Success,
        BoardGraphResponseMapper.ToResponse(
          BoardDependencyGraph.AnalyzeBoard(graphEngine, board, preferences)));
  }

  public async Task<BoardResult<BoardCardDependencyAnalysisResponse>> GetCardDependencyAnalysisAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var access = await projectAccess.FindAccessAsync(
      workspaceId,
      projectId,
      userId,
      cancellationToken);

    if (access is null)
    {
      return new BoardResult<BoardCardDependencyAnalysisResponse>(
        BoardResultStatus.NotFound);
    }

    var board = await boards.FindInProjectAsync(
      workspaceId,
      projectId,
      boardId,
      cancellationToken);

    if (board is null)
    {
      return new BoardResult<BoardCardDependencyAnalysisResponse>(
        BoardResultStatus.NotFound);
    }

    var analysis = BoardDependencyGraph.AnalyzeCard(
      graphEngine,
      board,
      cardId);

    return analysis is null
      ? new BoardResult<BoardCardDependencyAnalysisResponse>(
        BoardResultStatus.NotFound)
      : new BoardResult<BoardCardDependencyAnalysisResponse>(
        BoardResultStatus.Success,
        BoardGraphResponseMapper.ToResponse(analysis));
  }

  public async Task<BoardResult<BoardResponse>> AddCardDependencyAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    AddBoardCardDependencyRequest request,
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

    if (request.DependsOnCardId == Guid.Empty || request.DependsOnCardId == cardId)
    {
      return new BoardResult<BoardResponse>(
        BoardResultStatus.ValidationError,
        Errors: new Dictionary<string, string[]>
        {
          [nameof(request.DependsOnCardId)] = ["Choose another card from this board."]
        });
    }

    var boardSnapshot = await boards.FindInProjectAsync(
      workspaceId,
      projectId,
      boardId,
      cancellationToken);

    if (boardSnapshot is null)
    {
      return new BoardResult<BoardResponse>(BoardResultStatus.NotFound);
    }

    var dependencyEvaluation = BoardDependencyGraph.EvaluateDependency(
      graphEngine,
      boardSnapshot,
      cardId,
      request.DependsOnCardId);

    if (dependencyEvaluation.Status is GraphEdgeStatus.MissingSource
      or GraphEdgeStatus.MissingTarget)
    {
      return new BoardResult<BoardResponse>(BoardResultStatus.NotFound);
    }

    if (dependencyEvaluation.Status == GraphEdgeStatus.Duplicate)
    {
      return new BoardResult<BoardResponse>(
        BoardResultStatus.Success,
        BoardResponseMapper.ToResponse(boardSnapshot));
    }

    if (dependencyEvaluation.Status == GraphEdgeStatus.Cycle)
    {
      return new BoardResult<BoardResponse>(
        BoardResultStatus.Conflict,
        Message: "Adding this dependency would create a cycle.");
    }

    var board = await boards.AddCardDependencyAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      request.DependsOnCardId,
      DateTimeOffset.UtcNow,
      cancellationToken);

    return board is null
      ? new BoardResult<BoardResponse>(BoardResultStatus.NotFound)
      : new BoardResult<BoardResponse>(
        BoardResultStatus.Success,
        BoardResponseMapper.ToResponse(board));
  }

  public async Task<BoardResult<object>> DeleteCardDependencyAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid dependsOnCardId,
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
      return BoardResults.AccessFailure<object>(access.Status);
    }

    var deleted = await boards.DeleteCardDependencyAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      dependsOnCardId,
      DateTimeOffset.UtcNow,
      cancellationToken);

    return deleted
      ? new BoardResult<object>(BoardResultStatus.Success)
      : new BoardResult<object>(BoardResultStatus.NotFound);
  }
}
