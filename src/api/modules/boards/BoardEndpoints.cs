using Coordina.Api.Core.Http;
using Coordina.Api.Core.Security;
using Coordina.Api.Modules.Boards.Application;
using Coordina.Api.Modules.Boards.Contracts;

namespace Coordina.Api.Modules.Boards;

public static class BoardEndpoints
{
  public static IEndpointRouteBuilder MapBoardEndpoints(
    this IEndpointRouteBuilder app)
  {
    var projectGroup = app.MapApiGroup(
      "/workspaces/{workspaceId:guid}/projects/{projectId:guid}",
      "Boards");

    projectGroup.MapGet("/board", GetDefault)
      .WithSummary("Get default project board")
      .WithDescription("Returns a full board with lists, cards, labels, and assignees.");

    projectGroup.MapPost("/boards", CreateBoard)
      .WithSummary("Create board")
      .WithDescription("Creates a project board and generates lists from the selected template.");

    var boardGroup = projectGroup.MapGroup("/boards/{boardId:guid}");

    boardGroup.MapGet("/graph", GetGraph)
      .WithSummary("Get board dependency graph")
      .WithDescription("Returns graph-derived planning signals for the board.");

    boardGroup.MapPost("/lists", CreateList)
      .WithSummary("Create board list");

    boardGroup.MapPatch("/lists/{listId:guid}", UpdateList)
      .WithSummary("Update board list title");

    boardGroup.MapPost("/lists/{listId:guid}/cards", CreateCard)
      .WithSummary("Create card");

    boardGroup.MapPatch("/cards/{cardId:guid}", UpdateCard)
      .WithSummary("Update card");

    boardGroup.MapPatch("/cards/{cardId:guid}/move", MoveCard)
      .WithSummary("Move card");

    boardGroup.MapGet("/cards/{cardId:guid}/dependency-analysis", GetCardDependencyAnalysis)
      .WithSummary("Get card dependency analysis")
      .WithDescription("Returns safe dependency suggestions, blockers, and impacted dependents.");

    boardGroup.MapPost("/cards/{cardId:guid}/comments", AddCardComment)
      .WithSummary("Add card comment");

    boardGroup.MapPost("/cards/{cardId:guid}/subtasks", CreateCardSubtask)
      .WithSummary("Create card subtask");

    boardGroup.MapPatch("/cards/{cardId:guid}/subtasks/{subtaskId:guid}", UpdateCardSubtask)
      .WithSummary("Update card subtask");

    boardGroup.MapDelete("/cards/{cardId:guid}/subtasks/{subtaskId:guid}", DeleteCardSubtask)
      .WithSummary("Delete card subtask");

    boardGroup.MapPost("/cards/{cardId:guid}/dependencies", AddCardDependency)
      .WithSummary("Add card dependency");

    boardGroup.MapDelete("/cards/{cardId:guid}/dependencies/{dependsOnCardId:guid}", DeleteCardDependency)
      .WithSummary("Delete card dependency");

    boardGroup.MapDelete("/cards/{cardId:guid}", DeleteCard)
      .WithSummary("Delete card");

    return app;
  }

  private static async Task<IResult> GetDefault(
    Guid workspaceId,
    Guid projectId,
    CurrentUser user,
    IBoardCatalogService boardCatalog,
    CancellationToken cancellationToken)
  {
    var result = await boardCatalog.GetDefaultAsync(
      workspaceId,
      projectId,
      user.Id,
      cancellationToken);

    return result.ToOkResult();
  }

  private static async Task<IResult> CreateBoard(
    Guid workspaceId,
    Guid projectId,
    CreateBoardRequest request,
    CurrentUser user,
    IBoardCatalogService boardCatalog,
    CancellationToken cancellationToken)
  {
    var result = await boardCatalog.CreateAsync(
      workspaceId,
      projectId,
      request,
      user.Id,
      cancellationToken);

    return result.ToCreatedResult(
      board => $"/workspaces/{workspaceId}/projects/{projectId}/boards/{board.Id}");
  }

  private static async Task<IResult> GetGraph(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    int? availableMinutes,
    string? context,
    bool? preferAssigned,
    bool? includeUnassigned,
    CurrentUser user,
    IBoardDependencyService boardDependencies,
    CancellationToken cancellationToken)
  {
    var result = await boardDependencies.GetGraphAsync(
      workspaceId,
      projectId,
      boardId,
      user.Id,
      new BoardPlanningRequest(
        availableMinutes,
        context,
        preferAssigned,
        includeUnassigned),
      cancellationToken);

    return result.ToOkResult();
  }

  private static async Task<IResult> CreateList(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    CreateBoardListRequest request,
    CurrentUser user,
    IBoardListService boardLists,
    CancellationToken cancellationToken)
  {
    var result = await boardLists.CreateListAsync(
      workspaceId,
      projectId,
      boardId,
      request,
      user.Id,
      cancellationToken);

    return result.ToOkResult();
  }

  private static async Task<IResult> UpdateList(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid listId,
    UpdateBoardListRequest request,
    CurrentUser user,
    IBoardListService boardLists,
    CancellationToken cancellationToken)
  {
    var result = await boardLists.UpdateListAsync(
      workspaceId,
      projectId,
      boardId,
      listId,
      request,
      user.Id,
      cancellationToken);

    return result.ToOkResult();
  }

  private static async Task<IResult> CreateCard(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid listId,
    CreateBoardCardRequest request,
    CurrentUser user,
    IBoardCardService boardCards,
    CancellationToken cancellationToken)
  {
    var result = await boardCards.CreateCardAsync(
      workspaceId,
      projectId,
      boardId,
      listId,
      request,
      user.Id,
      cancellationToken);

    return result.ToOkResult();
  }

  private static async Task<IResult> UpdateCard(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    UpdateBoardCardRequest request,
    CurrentUser user,
    IBoardCardService boardCards,
    CancellationToken cancellationToken)
  {
    var result = await boardCards.UpdateCardAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      request,
      user.Id,
      cancellationToken);

    return result.ToOkResult();
  }

  private static async Task<IResult> MoveCard(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    MoveBoardCardRequest request,
    CurrentUser user,
    IBoardCardService boardCards,
    CancellationToken cancellationToken)
  {
    var result = await boardCards.MoveCardAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      request,
      user.Id,
      cancellationToken);

    return result.ToOkResult();
  }

  private static async Task<IResult> GetCardDependencyAnalysis(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    CurrentUser user,
    IBoardDependencyService boardDependencies,
    CancellationToken cancellationToken)
  {
    var result = await boardDependencies.GetCardDependencyAnalysisAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      user.Id,
      cancellationToken);

    return result.ToOkResult();
  }

  private static async Task<IResult> AddCardComment(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    CreateBoardCardCommentRequest request,
    CurrentUser user,
    IBoardCollaborationService boardCollaboration,
    CancellationToken cancellationToken)
  {
    var result = await boardCollaboration.AddCardCommentAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      request,
      user.Id,
      cancellationToken);

    return result.ToOkResult();
  }

  private static async Task<IResult> CreateCardSubtask(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    CreateBoardCardSubtaskRequest request,
    CurrentUser user,
    IBoardCollaborationService boardCollaboration,
    CancellationToken cancellationToken)
  {
    var result = await boardCollaboration.CreateCardSubtaskAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      request,
      user.Id,
      cancellationToken);

    return result.ToOkResult();
  }

  private static async Task<IResult> UpdateCardSubtask(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid subtaskId,
    UpdateBoardCardSubtaskRequest request,
    CurrentUser user,
    IBoardCollaborationService boardCollaboration,
    CancellationToken cancellationToken)
  {
    var result = await boardCollaboration.UpdateCardSubtaskAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      subtaskId,
      request,
      user.Id,
      cancellationToken);

    return result.ToOkResult();
  }

  private static async Task<IResult> DeleteCardSubtask(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid subtaskId,
    CurrentUser user,
    IBoardCollaborationService boardCollaboration,
    CancellationToken cancellationToken)
  {
    var result = await boardCollaboration.DeleteCardSubtaskAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      subtaskId,
      user.Id,
      cancellationToken);

    return result.ToNoContentResult();
  }

  private static async Task<IResult> AddCardDependency(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    AddBoardCardDependencyRequest request,
    CurrentUser user,
    IBoardDependencyService boardDependencies,
    CancellationToken cancellationToken)
  {
    var result = await boardDependencies.AddCardDependencyAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      request,
      user.Id,
      cancellationToken);

    return result.ToOkResult();
  }

  private static async Task<IResult> DeleteCardDependency(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid dependsOnCardId,
    CurrentUser user,
    IBoardDependencyService boardDependencies,
    CancellationToken cancellationToken)
  {
    var result = await boardDependencies.DeleteCardDependencyAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      dependsOnCardId,
      user.Id,
      cancellationToken);

    return result.ToNoContentResult();
  }

  private static async Task<IResult> DeleteCard(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    CurrentUser user,
    IBoardCardService boardCards,
    CancellationToken cancellationToken)
  {
    var result = await boardCards.DeleteCardAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      user.Id,
      cancellationToken);

    return result.ToNoContentResult();
  }
}
