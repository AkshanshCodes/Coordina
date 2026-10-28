using Coordina.Api.Modules.Boards.Contracts;

namespace Coordina.Api.Modules.Boards.Application;

public interface IBoardDependencyService
{
  Task<BoardResult<BoardGraphResponse>> GetGraphAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid userId,
    BoardPlanningRequest planning,
    CancellationToken cancellationToken);

  Task<BoardResult<BoardCardDependencyAnalysisResponse>> GetCardDependencyAnalysisAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid userId,
    CancellationToken cancellationToken);

  Task<BoardResult<BoardResponse>> AddCardDependencyAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    AddBoardCardDependencyRequest request,
    Guid userId,
    CancellationToken cancellationToken);

  Task<BoardResult<object>> DeleteCardDependencyAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid dependsOnCardId,
    Guid userId,
    CancellationToken cancellationToken);
}
