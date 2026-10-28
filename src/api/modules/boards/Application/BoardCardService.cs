using Coordina.Api.Core.Results;
using Coordina.Api.Core.Time;
using Coordina.Api.Modules.Boards.Contracts;
using Coordina.Api.Modules.Boards.Domain;
using Coordina.Api.Modules.Projects.Application;
using Coordina.Api.Modules.Workspaces.Application;
using Coordina.Api.Shared.Enums;
using Coordina.Api.Shared.Text;

namespace Coordina.Api.Modules.Boards.Application;

public sealed class BoardCardService(
  IBoardStore boards,
  IWorkspaceStore workspaces,
  IProjectAccessGuard projectAccess,
  TimeProvider clock) : IBoardCardService
{
  public async Task<Result<BoardResponse>> CreateCardAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid listId,
    CreateBoardCardRequest request,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var mutation = await BuildCardMutationAsync(
      workspaceId,
      projectId,
      userId,
      request.Title,
      request.Description,
      request.Priority,
      request.DueDate,
      request.EstimatedMinutes,
      request.Labels,
      request.AssigneeIds,
      false,
      null,
      clock.GetUtcNow(),
      cancellationToken);

    if (!mutation.IsSuccess)
    {
      return mutation.Error;
    }

    var current = await boards.FindInProjectAsync(
      workspaceId,
      projectId,
      boardId,
      cancellationToken);
    var list = current?.Lists.SingleOrDefault(candidate => candidate.Id == listId);

    if (list is null)
    {
      return Error.NotFound();
    }

    var createdAt = clock.GetUtcNow();
    var completion = BoardWorkflow.CompletionIn(list, null, userId, createdAt);
    var board = await boards.CreateCardAsync(
      workspaceId,
      projectId,
      boardId,
      listId,
      mutation.Value with
      {
        IsCompleted = completion.IsCompleted,
        CompletedAt = completion.CompletedAt,
        CompletedByUserId = completion.CompletedByUserId
      },
      createdAt,
      cancellationToken);

    return board is null
      ? Error.NotFound()
      : Result.Success(BoardResponseMapper.ToResponse(board));
  }

  public async Task<Result<BoardResponse>> UpdateCardAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    UpdateBoardCardRequest request,
    Guid userId,
    CancellationToken cancellationToken)
  {
    var currentBoard = await boards.FindInProjectAsync(
      workspaceId,
      projectId,
      boardId,
      cancellationToken);
    var current = currentBoard?.Lists
      .SelectMany(list => list.Cards)
      .SingleOrDefault(card => card.Id == cardId);

    if (currentBoard is null || current is null)
    {
      return Error.NotFound();
    }

    var mutation = await BuildCardMutationAsync(
      workspaceId,
      projectId,
      userId,
      request.Title ?? current.Title,
      request.Description ?? current.Description,
      request.Priority ?? (current.Priority is null ? null : EnumCode.Format(current.Priority.Value)),
      request.ClearDueDate ? null : request.DueDate ?? current.DueDate,
      request.ClearEstimatedMinutes
        ? null
        : request.EstimatedMinutes ?? current.EstimatedMinutes,
      request.Labels ?? current.Labels.ToArray(),
      request.AssigneeIds ?? current.Assignees.Select(assignee => assignee.UserId).ToArray(),
      request.IsCompleted ?? current.IsCompleted,
      current,
      clock.GetUtcNow(),
      cancellationToken);

    if (!mutation.IsSuccess)
    {
      return mutation.Error;
    }

    var targetListId = current.ListId;

    if (mutation.Value.IsCompleted != current.IsCompleted)
    {
      var targetStatus = mutation.Value.IsCompleted
        ? BoardTaskStatus.Done
        : BoardTaskStatus.Todo;
      var targetList = BoardWorkflow.FindFirstList(currentBoard, targetStatus);

      if (targetList is null)
      {
        return Error.Conflict(mutation.Value.IsCompleted
          ? "Add a list with status DONE to this board before completing cards."
          : "Add a list with status TODO to this board before reopening cards.");
      }

      targetListId = targetList.Id;
    }

    var board = await boards.UpdateCardAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      targetListId,
      mutation.Value,
      clock.GetUtcNow(),
      cancellationToken);

    return board is null
      ? Error.NotFound()
      : Result.Success(BoardResponseMapper.ToResponse(board));
  }

  public async Task<Result<BoardResponse>> MoveCardAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    MoveBoardCardRequest request,
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

    if (request.ListId == Guid.Empty)
    {
      return Error.Validation(nameof(request.ListId), "Target list is required.");
    }

    var current = await boards.FindInProjectAsync(
      workspaceId,
      projectId,
      boardId,
      cancellationToken);
    var targetList = current?.Lists
      .SingleOrDefault(list => list.Id == request.ListId);
    var card = current?.Lists
      .SelectMany(list => list.Cards)
      .SingleOrDefault(candidate => candidate.Id == cardId);

    if (targetList is null || card is null)
    {
      return Error.NotFound();
    }

    var movedAt = clock.GetUtcNow();
    var board = await boards.MoveCardAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      request.ListId,
      BoardWorkflow.CompletionIn(targetList, card, userId, movedAt),
      movedAt,
      cancellationToken);

    return board is null
      ? Error.NotFound()
      : Result.Success(BoardResponseMapper.ToResponse(board));
  }

  public async Task<Result> DeleteCardAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
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

    if (!access.Value.CanManage(userId))
    {
      return Error.Forbidden(
        "Only workspace owners or project owners can delete cards.");
    }

    var deleted = await boards.DeleteCardAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      cancellationToken);

    return deleted ? Result.Success() : Error.NotFound();
  }

  private async Task<Result<BoardCardMutation>> BuildCardMutationAsync(
    Guid workspaceId,
    Guid projectId,
    Guid userId,
    string? title,
    string? description,
    string? priority,
    DateOnly? dueDate,
    int? estimatedMinutes,
    string[]? labels,
    Guid[]? assigneeIds,
    bool isCompleted,
    ProjectBoardCard? current,
    DateTimeOffset updatedAt,
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

    var normalizedLabels = BoardRules.NormalizeLabels(labels);
    var normalizedAssignees = (assigneeIds ?? [])
      .Where(id => id != Guid.Empty)
      .Distinct()
      .ToArray();
    var errors = BoardRules.ValidateCard(
      title,
      description,
      priority,
      normalizedLabels,
      dueDate,
      estimatedMinutes,
      clock.GetUtcToday(),
      out var parsedPriority);

    if (normalizedAssignees.Length > 0)
    {
      var members = await workspaces.ListMembersAsync(
        workspaceId,
        cancellationToken);
      var memberIds = members.Select(member => member.UserId).ToHashSet();

      errors.AddIf(
        normalizedAssignees.Any(assigneeId => !memberIds.Contains(assigneeId)),
        "AssigneeIds",
        "All assignees must belong to the workspace.");
    }

    if (errors.HasErrors)
    {
      return errors.ToError();
    }

    return Result.Success(new BoardCardMutation(
      title!.Trim(),
      description is null && current is not null
        ? current.Description
        : TextNormalizer.TrimToNull(description),
      parsedPriority,
      dueDate,
      estimatedMinutes,
      normalizedLabels,
      normalizedAssignees,
      isCompleted,
      isCompleted
        ? current?.CompletedAt ?? updatedAt
        : null,
      isCompleted
        ? current?.CompletedBy?.UserId ?? userId
        : null));
  }
}
