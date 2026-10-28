using System.Security.Claims;
using Coordina.Api.Modules.Boards.Application;
using Coordina.Api.Modules.Boards.Contracts;

namespace Coordina.Api.Modules.Boards;

public static class BoardEndpoints
{
  public static IEndpointRouteBuilder MapBoardEndpoints(
    this IEndpointRouteBuilder app)
  {
    var projectGroup = app
      .MapGroup("/workspaces/{workspaceId:guid}/projects/{projectId:guid}")
      .RequireAuthorization()
      .WithTags("Boards");

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
    ClaimsPrincipal user,
    IBoardCatalogService boardCatalog,
    CancellationToken cancellationToken)
  {
    if (!TryGetUserId(user, out var userId))
    {
      return Results.Unauthorized();
    }

    var result = await boardCatalog.GetDefaultAsync(
      workspaceId,
      projectId,
      userId,
      cancellationToken);

    return ToResult(result);
  }

  private static async Task<IResult> CreateBoard(
    Guid workspaceId,
    Guid projectId,
    CreateBoardRequest request,
    ClaimsPrincipal user,
    IBoardCatalogService boardCatalog,
    CancellationToken cancellationToken)
  {
    if (!TryGetUserId(user, out var userId))
    {
      return Results.Unauthorized();
    }

    var result = await boardCatalog.CreateAsync(
      workspaceId,
      projectId,
      request,
      userId,
      cancellationToken);

    return result.Status switch
    {
      BoardResultStatus.Success => Results.Created(
        $"/workspaces/{workspaceId}/projects/{projectId}/boards/{result.Value!.Id}",
        result.Value),
      _ => ToResult(result)
    };
  }

  private static async Task<IResult> GetGraph(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    int? availableMinutes,
    string? context,
    bool? preferAssigned,
    bool? includeUnassigned,
    ClaimsPrincipal user,
    IBoardDependencyService boardDependencies,
    CancellationToken cancellationToken)
  {
    if (!TryGetUserId(user, out var userId))
    {
      return Results.Unauthorized();
    }

    var result = await boardDependencies.GetGraphAsync(
      workspaceId,
      projectId,
      boardId,
      userId,
      new BoardPlanningRequest(
        availableMinutes,
        context,
        preferAssigned,
        includeUnassigned),
      cancellationToken);

    return ToResult(result);
  }

  private static async Task<IResult> CreateList(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    CreateBoardListRequest request,
    ClaimsPrincipal user,
    IBoardListService boardLists,
    CancellationToken cancellationToken)
  {
    if (!TryGetUserId(user, out var userId))
    {
      return Results.Unauthorized();
    }

    var result = await boardLists.CreateListAsync(
      workspaceId,
      projectId,
      boardId,
      request,
      userId,
      cancellationToken);

    return ToResult(result);
  }

  private static async Task<IResult> UpdateList(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid listId,
    UpdateBoardListRequest request,
    ClaimsPrincipal user,
    IBoardListService boardLists,
    CancellationToken cancellationToken)
  {
    if (!TryGetUserId(user, out var userId))
    {
      return Results.Unauthorized();
    }

    var result = await boardLists.UpdateListAsync(
      workspaceId,
      projectId,
      boardId,
      listId,
      request,
      userId,
      cancellationToken);

    return ToResult(result);
  }

  private static async Task<IResult> CreateCard(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid listId,
    CreateBoardCardRequest request,
    ClaimsPrincipal user,
    IBoardCardService boardCards,
    CancellationToken cancellationToken)
  {
    if (!TryGetUserId(user, out var userId))
    {
      return Results.Unauthorized();
    }

    var result = await boardCards.CreateCardAsync(
      workspaceId,
      projectId,
      boardId,
      listId,
      request,
      userId,
      cancellationToken);

    return ToResult(result);
  }

  private static async Task<IResult> UpdateCard(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    UpdateBoardCardRequest request,
    ClaimsPrincipal user,
    IBoardCardService boardCards,
    CancellationToken cancellationToken)
  {
    if (!TryGetUserId(user, out var userId))
    {
      return Results.Unauthorized();
    }

    var result = await boardCards.UpdateCardAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      request,
      userId,
      cancellationToken);

    return ToResult(result);
  }

  private static async Task<IResult> MoveCard(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    MoveBoardCardRequest request,
    ClaimsPrincipal user,
    IBoardCardService boardCards,
    CancellationToken cancellationToken)
  {
    if (!TryGetUserId(user, out var userId))
    {
      return Results.Unauthorized();
    }

    var result = await boardCards.MoveCardAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      request,
      userId,
      cancellationToken);

    return ToResult(result);
  }

  private static async Task<IResult> GetCardDependencyAnalysis(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    ClaimsPrincipal user,
    IBoardDependencyService boardDependencies,
    CancellationToken cancellationToken)
  {
    if (!TryGetUserId(user, out var userId))
    {
      return Results.Unauthorized();
    }

    var result = await boardDependencies.GetCardDependencyAnalysisAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      userId,
      cancellationToken);

    return ToResult(result);
  }

  private static async Task<IResult> AddCardComment(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    CreateBoardCardCommentRequest request,
    ClaimsPrincipal user,
    IBoardCollaborationService boardCollaboration,
    CancellationToken cancellationToken)
  {
    if (!TryGetUserId(user, out var userId))
    {
      return Results.Unauthorized();
    }

    var result = await boardCollaboration.AddCardCommentAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      request,
      userId,
      cancellationToken);

    return ToResult(result);
  }

  private static async Task<IResult> CreateCardSubtask(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    CreateBoardCardSubtaskRequest request,
    ClaimsPrincipal user,
    IBoardCollaborationService boardCollaboration,
    CancellationToken cancellationToken)
  {
    if (!TryGetUserId(user, out var userId))
    {
      return Results.Unauthorized();
    }

    var result = await boardCollaboration.CreateCardSubtaskAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      request,
      userId,
      cancellationToken);

    return ToResult(result);
  }

  private static async Task<IResult> UpdateCardSubtask(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid subtaskId,
    UpdateBoardCardSubtaskRequest request,
    ClaimsPrincipal user,
    IBoardCollaborationService boardCollaboration,
    CancellationToken cancellationToken)
  {
    if (!TryGetUserId(user, out var userId))
    {
      return Results.Unauthorized();
    }

    var result = await boardCollaboration.UpdateCardSubtaskAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      subtaskId,
      request,
      userId,
      cancellationToken);

    return ToResult(result);
  }

  private static async Task<IResult> DeleteCardSubtask(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid subtaskId,
    ClaimsPrincipal user,
    IBoardCollaborationService boardCollaboration,
    CancellationToken cancellationToken)
  {
    if (!TryGetUserId(user, out var userId))
    {
      return Results.Unauthorized();
    }

    var result = await boardCollaboration.DeleteCardSubtaskAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      subtaskId,
      userId,
      cancellationToken);

    return ToEmptyResult(result);
  }

  private static async Task<IResult> AddCardDependency(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    AddBoardCardDependencyRequest request,
    ClaimsPrincipal user,
    IBoardDependencyService boardDependencies,
    CancellationToken cancellationToken)
  {
    if (!TryGetUserId(user, out var userId))
    {
      return Results.Unauthorized();
    }

    var result = await boardDependencies.AddCardDependencyAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      request,
      userId,
      cancellationToken);

    return ToResult(result);
  }

  private static async Task<IResult> DeleteCardDependency(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid dependsOnCardId,
    ClaimsPrincipal user,
    IBoardDependencyService boardDependencies,
    CancellationToken cancellationToken)
  {
    if (!TryGetUserId(user, out var userId))
    {
      return Results.Unauthorized();
    }

    var result = await boardDependencies.DeleteCardDependencyAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      dependsOnCardId,
      userId,
      cancellationToken);

    return ToEmptyResult(result);
  }

  private static async Task<IResult> DeleteCard(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    ClaimsPrincipal user,
    IBoardCardService boardCards,
    CancellationToken cancellationToken)
  {
    if (!TryGetUserId(user, out var userId))
    {
      return Results.Unauthorized();
    }

    var result = await boardCards.DeleteCardAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      userId,
      cancellationToken);

    return ToEmptyResult(result);
  }

  private static IResult ToEmptyResult<T>(BoardResult<T> result)
  {
    return result.Status switch
    {
      BoardResultStatus.Success => Results.NoContent(),
      BoardResultStatus.Forbidden => Results.Forbid(),
      BoardResultStatus.NotFound => Results.NotFound(),
      BoardResultStatus.Conflict => Results.Conflict(new
      {
        message = result.Message
      }),
      _ => Results.BadRequest()
    };
  }

  private static IResult ToResult<T>(BoardResult<T> result)
  {
    return result.Status switch
    {
      BoardResultStatus.Success => Results.Ok(result.Value),
      BoardResultStatus.ValidationError => Results.ValidationProblem(
        result.Errors?.ToDictionary() ?? new Dictionary<string, string[]>()),
      BoardResultStatus.Forbidden => Results.Forbid(),
      BoardResultStatus.NotFound => Results.NotFound(),
      BoardResultStatus.Conflict => Results.Conflict(new
      {
        message = result.Message
      }),
      _ => Results.BadRequest()
    };
  }

  private static bool TryGetUserId(
    ClaimsPrincipal user,
    out Guid userId)
  {
    return Guid.TryParse(
      user.FindFirstValue(ClaimTypes.NameIdentifier),
      out userId);
  }
}
