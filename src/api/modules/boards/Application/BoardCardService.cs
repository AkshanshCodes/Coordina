using Coordina.Api.Modules.Boards.Contracts;
using Coordina.Api.Modules.Boards.Domain;
using Coordina.Api.Modules.Projects.Application;
using Coordina.Api.Modules.Workspaces.Application;

namespace Coordina.Api.Modules.Boards.Application;

public sealed class BoardCardService(
  IBoardStore boards,
  IWorkspaceStore workspaces,
  IProjectAccessGuard projectAccess) : IBoardCardService
{
  public async Task<BoardResult<BoardResponse>> CreateCardAsync(
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
      DateTimeOffset.UtcNow,
      cancellationToken);

    if (mutation.Status != BoardResultStatus.Success)
    {
      return new BoardResult<BoardResponse>(
        mutation.Status,
        Errors: mutation.Errors,
        Message: mutation.Message);
    }

    var current = await boards.FindInProjectAsync(
      workspaceId,
      projectId,
      boardId,
      cancellationToken);
    var list = current?.Lists.SingleOrDefault(candidate => candidate.Id == listId);

    if (list is null)
    {
      return new BoardResult<BoardResponse>(BoardResultStatus.NotFound);
    }

    var createdAt = DateTimeOffset.UtcNow;
    var completion = BoardWorkflow.CompletionIn(list, null, userId, createdAt);
    var board = await boards.CreateCardAsync(
      workspaceId,
      projectId,
      boardId,
      listId,
      mutation.Value! with
      {
        IsCompleted = completion.IsCompleted,
        CompletedAt = completion.CompletedAt,
        CompletedByUserId = completion.CompletedByUserId
      },
      createdAt,
      cancellationToken);

    return board is null
      ? new BoardResult<BoardResponse>(BoardResultStatus.NotFound)
      : new BoardResult<BoardResponse>(
        BoardResultStatus.Success,
        BoardResponseMapper.ToResponse(board));
  }

  public async Task<BoardResult<BoardResponse>> UpdateCardAsync(
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
      return new BoardResult<BoardResponse>(BoardResultStatus.NotFound);
    }

    var mutation = await BuildCardMutationAsync(
      workspaceId,
      projectId,
      userId,
      request.Title ?? current.Title,
      request.Description ?? current.Description,
      request.Priority is null ? current.Priority?.ToString() : request.Priority,
      request.ClearDueDate ? null : request.DueDate ?? current.DueDate,
      request.ClearEstimatedMinutes
        ? null
        : request.EstimatedMinutes ?? current.EstimatedMinutes,
      request.Labels ?? current.Labels.ToArray(),
      request.AssigneeIds ?? current.Assignees.Select(assignee => assignee.UserId).ToArray(),
      request.IsCompleted ?? current.IsCompleted,
      current,
      DateTimeOffset.UtcNow,
      cancellationToken);

    if (mutation.Status != BoardResultStatus.Success)
    {
      return new BoardResult<BoardResponse>(
        mutation.Status,
        Errors: mutation.Errors,
        Message: mutation.Message);
    }

    var targetListId = current.ListId;

    if (mutation.Value!.IsCompleted != current.IsCompleted)
    {
      var targetStatus = mutation.Value.IsCompleted
        ? BoardTaskStatus.Done
        : BoardTaskStatus.Todo;
      var targetList = BoardWorkflow.FindFirstList(currentBoard, targetStatus);

      if (targetList is null)
      {
        return new BoardResult<BoardResponse>(
          BoardResultStatus.Conflict,
          Message: mutation.Value.IsCompleted
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
      DateTimeOffset.UtcNow,
      cancellationToken);

    return board is null
      ? new BoardResult<BoardResponse>(BoardResultStatus.NotFound)
      : new BoardResult<BoardResponse>(
        BoardResultStatus.Success,
        BoardResponseMapper.ToResponse(board));
  }

  public async Task<BoardResult<BoardResponse>> MoveCardAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    MoveBoardCardRequest request,
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

    if (request.ListId == Guid.Empty)
    {
      return new BoardResult<BoardResponse>(
        BoardResultStatus.ValidationError,
        Errors: new Dictionary<string, string[]>
        {
          [nameof(request.ListId)] = ["Target list is required."]
        });
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
      return new BoardResult<BoardResponse>(BoardResultStatus.NotFound);
    }

    var movedAt = DateTimeOffset.UtcNow;
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
      ? new BoardResult<BoardResponse>(BoardResultStatus.NotFound)
      : new BoardResult<BoardResponse>(
        BoardResultStatus.Success,
        BoardResponseMapper.ToResponse(board));
  }

  public async Task<BoardResult<object>> DeleteCardAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
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

    if (!access.Access!.CanManageDestructiveActions(userId))
    {
      return new BoardResult<object>(
        BoardResultStatus.Forbidden,
        Message: "Only workspace owners or project owners can delete cards.");
    }

    var deleted = await boards.DeleteCardAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      cancellationToken);

    return deleted
      ? new BoardResult<object>(BoardResultStatus.Success)
      : new BoardResult<object>(BoardResultStatus.NotFound);
  }

  private async Task<BoardResult<BoardCardMutation>> BuildCardMutationAsync(
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
    var access = await projectAccess.FindWritableAccessAsync(
      workspaceId,
      projectId,
      userId,
      cancellationToken);

    if (access.Status != ProjectAccessStatus.Granted)
    {
      return BoardResults.AccessFailure<BoardCardMutation>(access.Status);
    }

    var parsedPriority = BoardRules.ParsePriority(priority, out var priorityError);
    var normalizedLabels = BoardRules.NormalizeLabels(labels);
    var normalizedAssignees = (assigneeIds ?? [])
      .Where(id => id != Guid.Empty)
      .Distinct()
      .ToArray();
    var errors = BoardRules.ValidateCard(
      title,
      description,
      priorityError,
      normalizedLabels,
      dueDate,
      estimatedMinutes);

    if (normalizedAssignees.Length > 0)
    {
      var members = await workspaces.ListMembersAsync(
        workspaceId,
        cancellationToken);
      var memberIds = members.Select(member => member.UserId).ToHashSet();
      var invalidAssignees = normalizedAssignees
        .Where(assigneeId => !memberIds.Contains(assigneeId))
        .ToArray();

      if (invalidAssignees.Length > 0)
      {
        errors["AssigneeIds"] = [
          "All assignees must belong to the workspace."
        ];
      }
    }

    if (errors.Count > 0)
    {
      return new BoardResult<BoardCardMutation>(
        BoardResultStatus.ValidationError,
        Errors: errors);
    }

    return new BoardResult<BoardCardMutation>(
      BoardResultStatus.Success,
      new BoardCardMutation(
        title!.Trim(),
        description is null && current is not null
          ? current.Description
          : BoardRules.NormalizeText(description),
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
