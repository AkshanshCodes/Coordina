using System.Collections.Concurrent;
using Coordina.Api.Modules.Boards.Application;
using Coordina.Api.Modules.Boards.Domain;

namespace Coordina.Api.Modules.Boards.Infrastructure;

public sealed class InMemoryBoardStore : IBoardStore
{
  private readonly ConcurrentDictionary<Guid, BoardRecord> _boards = new();
  private readonly ConcurrentDictionary<Guid, ListRecord> _lists = new();
  private readonly ConcurrentDictionary<Guid, CardRecord> _cards = new();
  private readonly ConcurrentDictionary<Guid, CommentRecord> _comments = new();
  private readonly ConcurrentDictionary<Guid, SubtaskRecord> _subtasks = new();
  private readonly ConcurrentDictionary<Guid, DependencyRecord> _dependencies = new();

  public Task<ProjectBoard?> FindDefaultForProjectAsync(
    Guid workspaceId,
    Guid projectId,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var board = _boards.Values
      .Where(board => board.WorkspaceId == workspaceId
        && board.ProjectId == projectId)
      .OrderBy(board => board.CreatedAt)
      .FirstOrDefault();

    return Task.FromResult(board is null ? null : ToBoard(board));
  }

  public Task<ProjectBoard?> FindInProjectAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (!_boards.TryGetValue(boardId, out var board)
      || board.WorkspaceId != workspaceId
      || board.ProjectId != projectId)
    {
      return Task.FromResult<ProjectBoard?>(null);
    }

    return Task.FromResult<ProjectBoard?>(ToBoard(board));
  }

  public Task<ProjectBoard> CreateAsync(
    Guid workspaceId,
    Guid projectId,
    string name,
    BoardTemplate boardTemplate,
    IReadOnlyCollection<BoardListSeed> listSeeds,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var board = new BoardRecord(
      Guid.NewGuid(),
      projectId,
      workspaceId,
      name,
      boardTemplate,
      createdAt,
      createdAt);
    _boards[board.Id] = board;

    var position = 0;
    foreach (var listSeed in listSeeds)
    {
      var list = new ListRecord(
        Guid.NewGuid(),
        board.Id,
        projectId,
        workspaceId,
        listSeed.Title,
        position++,
        listSeed.TaskStatus,
        createdAt,
        createdAt);
      _lists[list.Id] = list;

      var cardPosition = 0;
      foreach (var cardSeed in listSeed.Cards)
      {
        var card = new CardRecord(
          Guid.NewGuid(),
          board.Id,
          list.Id,
          projectId,
          workspaceId,
          cardSeed.Title,
          cardSeed.Description,
          cardSeed.Priority,
          null,
          null,
          cardSeed.Labels.ToArray(),
          listSeed.TaskStatus == BoardTaskStatus.Done,
          listSeed.TaskStatus == BoardTaskStatus.Done ? createdAt : null,
          null,
          cardPosition++,
          createdAt,
          createdAt,
          []);
        _cards[card.Id] = card;
      }
    }

    return Task.FromResult(ToBoard(board));
  }

  public Task<ProjectBoard?> CreateListAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    string title,
    BoardTaskStatus taskStatus,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (!TryGetBoard(workspaceId, projectId, boardId, out var board))
    {
      return Task.FromResult<ProjectBoard?>(null);
    }

    var position = _lists.Values
      .Where(list => list.BoardId == boardId)
      .Select(list => (int?)list.Position)
      .Max() + 1 ?? 0;
    var list = new ListRecord(
      Guid.NewGuid(),
      boardId,
      projectId,
      workspaceId,
      title,
      position,
      taskStatus,
      createdAt,
      createdAt);
    _lists[list.Id] = list;
    _boards[boardId] = board with { UpdatedAt = createdAt };

    return Task.FromResult<ProjectBoard?>(ToBoard(_boards[boardId]));
  }

  public Task<ProjectBoard?> UpdateListAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid listId,
    string title,
    DateTimeOffset updatedAt,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (!TryGetBoard(workspaceId, projectId, boardId, out var board)
      || !_lists.TryGetValue(listId, out var list)
      || list.BoardId != boardId)
    {
      return Task.FromResult<ProjectBoard?>(null);
    }

    _lists[listId] = list with { Title = title, UpdatedAt = updatedAt };
    _boards[boardId] = board with { UpdatedAt = updatedAt };

    return Task.FromResult<ProjectBoard?>(ToBoard(_boards[boardId]));
  }

  public Task<ProjectBoardCard?> FindCardAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (!_cards.TryGetValue(cardId, out var card)
      || card.WorkspaceId != workspaceId
      || card.ProjectId != projectId
      || card.BoardId != boardId)
    {
      return Task.FromResult<ProjectBoardCard?>(null);
    }

    return Task.FromResult<ProjectBoardCard?>(ToCard(card));
  }

  public Task<ProjectBoard?> CreateCardAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid listId,
    BoardCardMutation mutation,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (!TryGetBoard(workspaceId, projectId, boardId, out var board)
      || !TryGetList(workspaceId, projectId, boardId, listId, out _))
    {
      return Task.FromResult<ProjectBoard?>(null);
    }

    var position = _cards.Values
      .Where(card => card.BoardId == boardId && card.ListId == listId)
      .Select(card => (int?)card.Position)
      .Max() + 1 ?? 0;
    var card = new CardRecord(
      Guid.NewGuid(),
      boardId,
      listId,
      projectId,
      workspaceId,
      mutation.Title,
      mutation.Description,
      mutation.Priority,
      mutation.DueDate,
      mutation.EstimatedMinutes,
      mutation.Labels.ToArray(),
      mutation.IsCompleted,
      mutation.CompletedAt,
      mutation.CompletedByUserId,
      position,
      createdAt,
      createdAt,
      mutation.AssigneeIds.ToArray());
    _cards[card.Id] = card;
    _boards[boardId] = board with { UpdatedAt = createdAt };

    return Task.FromResult<ProjectBoard?>(ToBoard(_boards[boardId]));
  }

  public Task<ProjectBoard?> UpdateCardAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid listId,
    BoardCardMutation mutation,
    DateTimeOffset updatedAt,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (!TryGetBoard(workspaceId, projectId, boardId, out var board)
      || !TryGetList(workspaceId, projectId, boardId, listId, out _)
      || !_cards.TryGetValue(cardId, out var card)
      || card.BoardId != boardId)
    {
      return Task.FromResult<ProjectBoard?>(null);
    }

    _cards[cardId] = card with
    {
      ListId = listId,
      Position = card.ListId == listId
        ? card.Position
        : NextCardPosition(boardId, listId),
      Title = mutation.Title,
      Description = mutation.Description,
      Priority = mutation.Priority,
      DueDate = mutation.DueDate,
      EstimatedMinutes = mutation.EstimatedMinutes,
      Labels = mutation.Labels.ToArray(),
      IsCompleted = mutation.IsCompleted,
      CompletedAt = mutation.CompletedAt,
      CompletedByUserId = mutation.CompletedByUserId,
      UpdatedAt = updatedAt,
      AssigneeIds = mutation.AssigneeIds.ToArray()
    };
    _boards[boardId] = board with { UpdatedAt = updatedAt };

    return Task.FromResult<ProjectBoard?>(ToBoard(_boards[boardId]));
  }

  public Task<ProjectBoard?> MoveCardAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid listId,
    BoardCardCompletion completion,
    DateTimeOffset updatedAt,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (!TryGetBoard(workspaceId, projectId, boardId, out var board)
      || !TryGetList(workspaceId, projectId, boardId, listId, out _)
      || !_cards.TryGetValue(cardId, out var card)
      || card.BoardId != boardId)
    {
      return Task.FromResult<ProjectBoard?>(null);
    }

    _cards[cardId] = card with
    {
      ListId = listId,
      Position = NextCardPosition(boardId, listId),
      IsCompleted = completion.IsCompleted,
      CompletedAt = completion.CompletedAt,
      CompletedByUserId = completion.CompletedByUserId,
      UpdatedAt = updatedAt
    };
    _boards[boardId] = board with { UpdatedAt = updatedAt };

    return Task.FromResult<ProjectBoard?>(ToBoard(_boards[boardId]));
  }

  public Task<ProjectBoard?> AddCardCommentAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid userId,
    string body,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (!TryGetBoard(workspaceId, projectId, boardId, out var board)
      || !TryGetCard(workspaceId, projectId, boardId, cardId, out var card))
    {
      return Task.FromResult<ProjectBoard?>(null);
    }

    var comment = new CommentRecord(
      Guid.NewGuid(),
      cardId,
      userId,
      body,
      createdAt);
    _comments[comment.Id] = comment;
    _cards[cardId] = card with { UpdatedAt = createdAt };
    _boards[boardId] = board with { UpdatedAt = createdAt };

    return Task.FromResult<ProjectBoard?>(ToBoard(_boards[boardId]));
  }

  public Task<ProjectBoard?> CreateCardSubtaskAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    string title,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (!TryGetBoard(workspaceId, projectId, boardId, out var board)
      || !TryGetCard(workspaceId, projectId, boardId, cardId, out var card))
    {
      return Task.FromResult<ProjectBoard?>(null);
    }

    var position = _subtasks.Values
      .Where(subtask => subtask.CardId == cardId)
      .Select(subtask => (int?)subtask.Position)
      .Max() + 1 ?? 0;
    var subtask = new SubtaskRecord(
      Guid.NewGuid(),
      cardId,
      title,
      false,
      null,
      null,
      position,
      createdAt,
      createdAt);
    _subtasks[subtask.Id] = subtask;
    _cards[cardId] = card with { UpdatedAt = createdAt };
    _boards[boardId] = board with { UpdatedAt = createdAt };

    return Task.FromResult<ProjectBoard?>(ToBoard(_boards[boardId]));
  }

  public Task<ProjectBoard?> UpdateCardSubtaskAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid subtaskId,
    string? title,
    bool? isCompleted,
    Guid userId,
    DateTimeOffset updatedAt,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (!TryGetBoard(workspaceId, projectId, boardId, out var board)
      || !TryGetCard(workspaceId, projectId, boardId, cardId, out var card)
      || !_subtasks.TryGetValue(subtaskId, out var subtask)
      || subtask.CardId != cardId)
    {
      return Task.FromResult<ProjectBoard?>(null);
    }

    var nextIsCompleted = isCompleted ?? subtask.IsCompleted;
    _subtasks[subtaskId] = subtask with
    {
      Title = title ?? subtask.Title,
      IsCompleted = nextIsCompleted,
      CompletedAt = nextIsCompleted
        ? subtask.CompletedAt ?? updatedAt
        : null,
      CompletedByUserId = nextIsCompleted
        ? subtask.CompletedByUserId ?? userId
        : null,
      UpdatedAt = updatedAt
    };
    _cards[cardId] = card with { UpdatedAt = updatedAt };
    _boards[boardId] = board with { UpdatedAt = updatedAt };

    return Task.FromResult<ProjectBoard?>(ToBoard(_boards[boardId]));
  }

  public Task<bool> DeleteCardSubtaskAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid subtaskId,
    DateTimeOffset updatedAt,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (!TryGetBoard(workspaceId, projectId, boardId, out var board)
      || !TryGetCard(workspaceId, projectId, boardId, cardId, out var card)
      || !_subtasks.TryGetValue(subtaskId, out var subtask)
      || subtask.CardId != cardId)
    {
      return Task.FromResult(false);
    }

    var deleted = _subtasks.TryRemove(subtaskId, out _);
    if (deleted)
    {
      _cards[cardId] = card with { UpdatedAt = updatedAt };
      _boards[boardId] = board with { UpdatedAt = updatedAt };
    }

    return Task.FromResult(deleted);
  }

  public Task<ProjectBoard?> AddCardDependencyAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid dependsOnCardId,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (!TryGetBoard(workspaceId, projectId, boardId, out var board)
      || !TryGetCard(workspaceId, projectId, boardId, cardId, out var card)
      || !TryGetCard(workspaceId, projectId, boardId, dependsOnCardId, out _))
    {
      return Task.FromResult<ProjectBoard?>(null);
    }

    var exists = _dependencies.Values.Any(dependency =>
      dependency.CardId == cardId
      && dependency.DependsOnCardId == dependsOnCardId);

    if (!exists)
    {
      var dependency = new DependencyRecord(
        Guid.NewGuid(),
        cardId,
        dependsOnCardId,
        createdAt);
      _dependencies[dependency.Id] = dependency;
      _cards[cardId] = card with { UpdatedAt = createdAt };
      _boards[boardId] = board with { UpdatedAt = createdAt };
    }

    return Task.FromResult<ProjectBoard?>(ToBoard(_boards[boardId]));
  }

  public Task<bool> DeleteCardDependencyAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid dependsOnCardId,
    DateTimeOffset updatedAt,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (!TryGetBoard(workspaceId, projectId, boardId, out var board)
      || !TryGetCard(workspaceId, projectId, boardId, cardId, out var card))
    {
      return Task.FromResult(false);
    }

    var dependency = _dependencies.Values.FirstOrDefault(candidate =>
      candidate.CardId == cardId
      && candidate.DependsOnCardId == dependsOnCardId);

    if (dependency is null)
    {
      return Task.FromResult(false);
    }

    var deleted = _dependencies.TryRemove(dependency.Id, out _);
    if (deleted)
    {
      _cards[cardId] = card with { UpdatedAt = updatedAt };
      _boards[boardId] = board with { UpdatedAt = updatedAt };
    }

    return Task.FromResult(deleted);
  }

  public Task<bool> DeleteCardAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (!_cards.TryGetValue(cardId, out var card)
      || card.WorkspaceId != workspaceId
      || card.ProjectId != projectId
      || card.BoardId != boardId)
    {
      return Task.FromResult(false);
    }

    foreach (var dependency in _dependencies.Values
      .Where(dependency => dependency.CardId == cardId
        || dependency.DependsOnCardId == cardId)
      .ToArray())
    {
      _dependencies.TryRemove(dependency.Id, out _);
    }

    foreach (var comment in _comments.Values
      .Where(comment => comment.CardId == cardId)
      .ToArray())
    {
      _comments.TryRemove(comment.Id, out _);
    }

    foreach (var subtask in _subtasks.Values
      .Where(subtask => subtask.CardId == cardId)
      .ToArray())
    {
      _subtasks.TryRemove(subtask.Id, out _);
    }

    return Task.FromResult(_cards.TryRemove(cardId, out _));
  }

  private int NextCardPosition(Guid boardId, Guid listId)
  {
    return _cards.Values
      .Where(candidate => candidate.BoardId == boardId
        && candidate.ListId == listId)
      .Select(candidate => (int?)candidate.Position)
      .Max() + 1 ?? 0;
  }

  private bool TryGetBoard(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    out BoardRecord board)
  {
    return _boards.TryGetValue(boardId, out board!)
      && board.WorkspaceId == workspaceId
      && board.ProjectId == projectId;
  }

  private bool TryGetList(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid listId,
    out ListRecord list)
  {
    return _lists.TryGetValue(listId, out list!)
      && list.WorkspaceId == workspaceId
      && list.ProjectId == projectId
      && list.BoardId == boardId;
  }

  private bool TryGetCard(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    out CardRecord card)
  {
    return _cards.TryGetValue(cardId, out card!)
      && card.WorkspaceId == workspaceId
      && card.ProjectId == projectId
      && card.BoardId == boardId;
  }

  private ProjectBoard ToBoard(BoardRecord board)
  {
    return new ProjectBoard(
      board.Id,
      board.ProjectId,
      board.WorkspaceId,
      board.Name,
      board.Template,
      board.CreatedAt,
      board.UpdatedAt,
      _lists.Values
        .Where(list => list.BoardId == board.Id)
        .OrderBy(list => list.Position)
        .Select(ToList)
        .ToArray());
  }

  private ProjectBoardList ToList(ListRecord list)
  {
    return new ProjectBoardList(
      list.Id,
      list.BoardId,
      list.ProjectId,
      list.WorkspaceId,
      list.Title,
      list.Position,
      list.TaskStatus,
      list.CreatedAt,
      list.UpdatedAt,
      _cards.Values
        .Where(card => card.ListId == list.Id)
        .OrderBy(card => card.Position)
        .Select(ToCard)
        .ToArray());
  }

  private ProjectBoardCard ToCard(CardRecord card)
  {
    return new ProjectBoardCard(
      card.Id,
      card.BoardId,
      card.ListId,
      card.ProjectId,
      card.WorkspaceId,
      card.Title,
      card.Description,
      card.Priority,
      card.DueDate,
      card.EstimatedMinutes,
      card.Labels,
      card.IsCompleted,
      card.CompletedAt,
      card.CompletedByUserId is null
        ? null
        : new ProjectBoardCardUser(card.CompletedByUserId.Value, null, null),
      card.Position,
      card.CreatedAt,
      card.UpdatedAt,
      card.AssigneeIds
        .Select(userId => new ProjectBoardCardAssignee(userId, null, null))
        .ToArray(),
      _comments.Values
        .Where(comment => comment.CardId == card.Id)
        .OrderBy(comment => comment.CreatedAt)
        .Select(comment => new ProjectBoardCardComment(
          comment.Id,
          comment.CardId,
          new ProjectBoardCardUser(comment.UserId, null, null),
          comment.Body,
          comment.CreatedAt))
        .ToArray(),
      _subtasks.Values
        .Where(subtask => subtask.CardId == card.Id)
        .OrderBy(subtask => subtask.Position)
        .Select(subtask => new ProjectBoardCardSubtask(
          subtask.Id,
          subtask.CardId,
          subtask.Title,
          subtask.IsCompleted,
          subtask.CompletedAt,
          subtask.CompletedByUserId is null
            ? null
            : new ProjectBoardCardUser(subtask.CompletedByUserId.Value, null, null),
          subtask.Position,
          subtask.CreatedAt,
          subtask.UpdatedAt))
        .ToArray(),
      _dependencies.Values
        .Where(dependency => dependency.CardId == card.Id)
        .Select(dependency => _cards.TryGetValue(
          dependency.DependsOnCardId,
          out var target)
            ? new ProjectBoardCardDependency(
              target.Id,
              target.Title,
              target.IsCompleted)
            : null)
        .OfType<ProjectBoardCardDependency>()
        .ToArray());
  }

  private sealed record BoardRecord(
    Guid Id,
    Guid ProjectId,
    Guid WorkspaceId,
    string Name,
    BoardTemplate Template,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

  private sealed record ListRecord(
    Guid Id,
    Guid BoardId,
    Guid ProjectId,
    Guid WorkspaceId,
    string Title,
    int Position,
    BoardTaskStatus TaskStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

  private sealed record CardRecord(
    Guid Id,
    Guid BoardId,
    Guid ListId,
    Guid ProjectId,
    Guid WorkspaceId,
    string Title,
    string? Description,
    BoardCardPriority? Priority,
    DateOnly? DueDate,
    int? EstimatedMinutes,
    IReadOnlyCollection<string> Labels,
    bool IsCompleted,
    DateTimeOffset? CompletedAt,
    Guid? CompletedByUserId,
    int Position,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<Guid> AssigneeIds);

  private sealed record CommentRecord(
    Guid Id,
    Guid CardId,
    Guid UserId,
    string Body,
    DateTimeOffset CreatedAt);

  private sealed record SubtaskRecord(
    Guid Id,
    Guid CardId,
    string Title,
    bool IsCompleted,
    DateTimeOffset? CompletedAt,
    Guid? CompletedByUserId,
    int Position,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

  private sealed record DependencyRecord(
    Guid Id,
    Guid CardId,
    Guid DependsOnCardId,
    DateTimeOffset CreatedAt);
}
