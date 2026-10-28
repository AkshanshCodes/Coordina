using Coordina.Api.Infrastructure.Persistence;
using Coordina.Api.Modules.Boards.Application;
using Coordina.Api.Modules.Boards.Domain;
using Microsoft.EntityFrameworkCore;

namespace Coordina.Api.Modules.Boards.Infrastructure;

public sealed class PostgresBoardStore(CoordinaDbContext dbContext)
  : IBoardStore
{
  public async Task<ProjectBoard?> FindDefaultForProjectAsync(
    Guid workspaceId,
    Guid projectId,
    CancellationToken cancellationToken)
  {
    var boardId = await dbContext.Boards
      .AsNoTracking()
      .Where(board => board.WorkspaceId == workspaceId
        && board.ProjectId == projectId)
      .OrderBy(board => board.CreatedAt)
      .Select(board => (Guid?)board.Id)
      .FirstOrDefaultAsync(cancellationToken);

    return boardId is null
      ? null
      : await FindInProjectAsync(
        workspaceId,
        projectId,
        boardId.Value,
        cancellationToken);
  }

  public async Task<ProjectBoard?> FindInProjectAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    CancellationToken cancellationToken)
  {
    var board = await dbContext.Boards
      .AsNoTracking()
      .Where(board => board.WorkspaceId == workspaceId
        && board.ProjectId == projectId
        && board.Id == boardId)
      .SingleOrDefaultAsync(cancellationToken);

    if (board is null)
    {
      return null;
    }

    var lists = await dbContext.BoardLists
      .AsNoTracking()
      .Where(list => list.WorkspaceId == workspaceId
        && list.ProjectId == projectId
        && list.BoardId == boardId)
      .OrderBy(list => list.Position)
      .ToArrayAsync(cancellationToken);

    var cards = await dbContext.BoardCards
      .AsNoTracking()
      .Where(card => card.WorkspaceId == workspaceId
        && card.ProjectId == projectId
        && card.BoardId == boardId)
      .OrderBy(card => card.Position)
      .ToArrayAsync(cancellationToken);

    var cardIds = cards.Select(card => card.Id).ToArray();
    var completedUserIds = cards
      .Select(card => card.CompletedByUserId)
      .OfType<Guid>()
      .ToArray();
    var assignees = await dbContext.BoardCardAssignees
      .AsNoTracking()
      .Where(assignee => cardIds.Contains(assignee.CardId))
      .Join(
        dbContext.AuthUsers.AsNoTracking(),
        assignee => assignee.UserId,
        user => user.Id,
        (assignee, user) => new
        {
          assignee.CardId,
          assignee.UserId,
          user.Name,
          user.Email
        })
      .ToArrayAsync(cancellationToken);
    var comments = await dbContext.BoardCardComments
      .AsNoTracking()
      .Where(comment => cardIds.Contains(comment.CardId))
      .Join(
        dbContext.AuthUsers.AsNoTracking(),
        comment => comment.UserId,
        user => user.Id,
        (comment, user) => new ProjectBoardCardComment(
          comment.Id,
          comment.CardId,
          new ProjectBoardCardUser(user.Id, user.Name, user.Email),
          comment.Body,
          comment.CreatedAt))
      .ToArrayAsync(cancellationToken);
    var subtasks = await dbContext.BoardCardSubtasks
      .AsNoTracking()
      .Where(subtask => cardIds.Contains(subtask.CardId))
      .OrderBy(subtask => subtask.Position)
      .ToArrayAsync(cancellationToken);
    var subtaskCompletedUserIds = subtasks
      .Select(subtask => subtask.CompletedByUserId)
      .OfType<Guid>()
      .ToArray();
    var userIds = completedUserIds
      .Concat(subtaskCompletedUserIds)
      .Distinct()
      .ToArray();
    var users = await dbContext.AuthUsers
      .AsNoTracking()
      .Where(user => userIds.Contains(user.Id))
      .Select(user => new ProjectBoardCardUser(user.Id, user.Name, user.Email))
      .ToDictionaryAsync(user => user.UserId, cancellationToken);
    var dependencies = await dbContext.BoardCardDependencies
      .AsNoTracking()
      .Where(dependency => cardIds.Contains(dependency.CardId))
      .ToArrayAsync(cancellationToken);
    var cardsById = cards.ToDictionary(card => card.Id);
    var dependenciesByCard = dependencies
      .GroupBy(dependency => dependency.CardId)
      .ToDictionary(
        group => group.Key,
        group => group
          .Where(dependency => cardsById.ContainsKey(dependency.DependsOnCardId))
          .Select(dependency =>
          {
            var target = cardsById[dependency.DependsOnCardId];
            return new ProjectBoardCardDependency(
              target.Id,
              target.Title,
              target.IsCompleted);
          })
          .ToArray());
    var commentsByCard = comments
      .GroupBy(comment => comment.CardId)
      .ToDictionary(
        group => group.Key,
        group => group
          .OrderBy(comment => comment.CreatedAt)
          .ToArray());
    var subtasksByCard = subtasks
      .GroupBy(subtask => subtask.CardId)
      .ToDictionary(
        group => group.Key,
        group => group
          .OrderBy(subtask => subtask.Position)
          .Select(subtask => new ProjectBoardCardSubtask(
            subtask.Id,
            subtask.CardId,
            subtask.Title,
            subtask.IsCompleted,
            subtask.CompletedAt,
            subtask.CompletedByUserId is not null
              && users.TryGetValue(subtask.CompletedByUserId.Value, out var completedBy)
                ? completedBy
                : null,
            subtask.Position,
            subtask.CreatedAt,
            subtask.UpdatedAt))
          .ToArray());

    var cardsByList = cards
      .GroupBy(card => card.ListId)
      .ToDictionary(
        group => group.Key,
        group => group
          .OrderBy(card => card.Position)
          .Select(card => ToCard(
            card,
            assignees
              .Where(assignee => assignee.CardId == card.Id)
              .Select(assignee => new ProjectBoardCardAssignee(
                assignee.UserId,
                assignee.Name,
                assignee.Email))
              .ToArray(),
            commentsByCard.TryGetValue(card.Id, out var cardComments)
              ? cardComments
              : [],
            subtasksByCard.TryGetValue(card.Id, out var cardSubtasks)
              ? cardSubtasks
              : [],
            dependenciesByCard.TryGetValue(card.Id, out var cardDependencies)
              ? cardDependencies
              : [],
            card.CompletedByUserId is not null
              && users.TryGetValue(card.CompletedByUserId.Value, out var completedBy)
                ? completedBy
                : null))
          .ToArray());

    return new ProjectBoard(
      board.Id,
      board.ProjectId,
      board.WorkspaceId,
      board.Name,
      board.Template,
      board.CreatedAt,
      board.UpdatedAt,
      lists
        .Select(list => ToList(
          list,
          cardsByList.TryGetValue(list.Id, out var listCards)
            ? listCards
            : []))
        .ToArray());
  }

  public async Task<ProjectBoard> CreateAsync(
    Guid workspaceId,
    Guid projectId,
    string name,
    BoardTemplate boardTemplate,
    IReadOnlyCollection<BoardListSeed> listSeeds,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken)
  {
    var board = new BoardEntity
    {
      Id = Guid.NewGuid(),
      WorkspaceId = workspaceId,
      ProjectId = projectId,
      Name = name,
      Template = boardTemplate,
      CreatedAt = createdAt,
      UpdatedAt = createdAt
    };

    var position = 0;
    foreach (var listSeed in listSeeds)
    {
      var list = new BoardListEntity
      {
        Id = Guid.NewGuid(),
        BoardId = board.Id,
        WorkspaceId = workspaceId,
        ProjectId = projectId,
        Title = listSeed.Title,
        Position = position++,
        TaskStatus = listSeed.TaskStatus,
        CreatedAt = createdAt,
        UpdatedAt = createdAt
      };

      var cardPosition = 0;
      foreach (var cardSeed in listSeed.Cards)
      {
        list.Cards.Add(new BoardCardEntity
        {
          Id = Guid.NewGuid(),
          BoardId = board.Id,
          ListId = list.Id,
          WorkspaceId = workspaceId,
          ProjectId = projectId,
          Title = cardSeed.Title,
          Description = cardSeed.Description,
          Priority = cardSeed.Priority,
          Labels = cardSeed.Labels.ToArray(),
          IsCompleted = listSeed.TaskStatus == BoardTaskStatus.Done,
          CompletedAt = listSeed.TaskStatus == BoardTaskStatus.Done
            ? createdAt
            : null,
          Position = cardPosition++,
          CreatedAt = createdAt,
          UpdatedAt = createdAt
        });
      }

      board.Lists.Add(list);
    }

    dbContext.Boards.Add(board);
    await dbContext.SaveChangesAsync(cancellationToken);

    return await FindInProjectAsync(
        workspaceId,
        projectId,
        board.Id,
        cancellationToken)
      ?? throw new InvalidOperationException("Board was not created.");
  }

  public async Task<ProjectBoard?> CreateListAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    string title,
    BoardTaskStatus taskStatus,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken)
  {
    var boardExists = await BoardExistsAsync(
      workspaceId,
      projectId,
      boardId,
      cancellationToken);

    if (!boardExists)
    {
      return null;
    }

    var nextPosition = await dbContext.BoardLists
      .Where(list => list.WorkspaceId == workspaceId
        && list.ProjectId == projectId
        && list.BoardId == boardId)
      .Select(list => (int?)list.Position)
      .MaxAsync(cancellationToken) + 1 ?? 0;

    dbContext.BoardLists.Add(new BoardListEntity
    {
      Id = Guid.NewGuid(),
      WorkspaceId = workspaceId,
      ProjectId = projectId,
      BoardId = boardId,
      Title = title,
      Position = nextPosition,
      TaskStatus = taskStatus,
      CreatedAt = createdAt,
      UpdatedAt = createdAt
    });

    await TouchBoardAsync(boardId, createdAt, cancellationToken);
    await dbContext.SaveChangesAsync(cancellationToken);

    return await FindInProjectAsync(
      workspaceId,
      projectId,
      boardId,
      cancellationToken);
  }

  public async Task<ProjectBoard?> UpdateListAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid listId,
    string title,
    DateTimeOffset updatedAt,
    CancellationToken cancellationToken)
  {
    var updated = await dbContext.BoardLists
      .Where(list => list.WorkspaceId == workspaceId
        && list.ProjectId == projectId
        && list.BoardId == boardId
        && list.Id == listId)
      .ExecuteUpdateAsync(setters => setters
        .SetProperty(list => list.Title, title)
        .SetProperty(list => list.UpdatedAt, updatedAt),
        cancellationToken);

    if (updated == 0)
    {
      return null;
    }

    await TouchBoardAsync(boardId, updatedAt, cancellationToken);

    return await FindInProjectAsync(
      workspaceId,
      projectId,
      boardId,
      cancellationToken);
  }

  public async Task<ProjectBoardCard?> FindCardAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    CancellationToken cancellationToken)
  {
    var board = await FindInProjectAsync(
      workspaceId,
      projectId,
      boardId,
      cancellationToken);

    return board?.Lists
      .SelectMany(list => list.Cards)
      .SingleOrDefault(card => card.Id == cardId);
  }

  public async Task<ProjectBoard?> CreateCardAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid listId,
    BoardCardMutation mutation,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken)
  {
    var listExists = await ListExistsAsync(
      workspaceId,
      projectId,
      boardId,
      listId,
      cancellationToken);

    if (!listExists)
    {
      return null;
    }

    var nextPosition = await dbContext.BoardCards
      .Where(card => card.WorkspaceId == workspaceId
        && card.ProjectId == projectId
        && card.BoardId == boardId
        && card.ListId == listId)
      .Select(card => (int?)card.Position)
      .MaxAsync(cancellationToken) + 1 ?? 0;

    var card = new BoardCardEntity
    {
      Id = Guid.NewGuid(),
      WorkspaceId = workspaceId,
      ProjectId = projectId,
      BoardId = boardId,
      ListId = listId,
      Title = mutation.Title,
      Description = mutation.Description,
      Priority = mutation.Priority,
      DueDate = mutation.DueDate,
      Labels = mutation.Labels.ToArray(),
      IsCompleted = mutation.IsCompleted,
      CompletedAt = mutation.CompletedAt,
      CompletedByUserId = mutation.CompletedByUserId,
      Position = nextPosition,
      CreatedAt = createdAt,
      UpdatedAt = createdAt,
      Assignees = mutation.AssigneeIds
        .Select(assigneeId => new BoardCardAssigneeEntity
        {
          Id = Guid.NewGuid(),
          UserId = assigneeId,
          AssignedAt = createdAt
        })
        .ToArray()
    };

    dbContext.BoardCards.Add(card);
    await TouchBoardAsync(boardId, createdAt, cancellationToken);
    await dbContext.SaveChangesAsync(cancellationToken);

    return await FindInProjectAsync(
      workspaceId,
      projectId,
      boardId,
      cancellationToken);
  }

  public async Task<ProjectBoard?> UpdateCardAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid listId,
    BoardCardMutation mutation,
    DateTimeOffset updatedAt,
    CancellationToken cancellationToken)
  {
    var card = await dbContext.BoardCards
      .Include(card => card.Assignees)
      .SingleOrDefaultAsync(card => card.WorkspaceId == workspaceId
        && card.ProjectId == projectId
        && card.BoardId == boardId
        && card.Id == cardId,
        cancellationToken);

    if (card is null)
    {
      return null;
    }

    if (card.ListId != listId)
    {
      var listExists = await ListExistsAsync(
        workspaceId,
        projectId,
        boardId,
        listId,
        cancellationToken);

      if (!listExists)
      {
        return null;
      }

      card.Position = await NextCardPositionAsync(
        workspaceId,
        projectId,
        boardId,
        listId,
        cancellationToken);
      card.ListId = listId;
    }

    card.Title = mutation.Title;
    card.Description = mutation.Description;
    card.Priority = mutation.Priority;
    card.DueDate = mutation.DueDate;
    card.Labels = mutation.Labels.ToArray();
    card.IsCompleted = mutation.IsCompleted;
    card.CompletedAt = mutation.CompletedAt;
    card.CompletedByUserId = mutation.CompletedByUserId;
    card.UpdatedAt = updatedAt;

    var requestedAssigneeIds = mutation.AssigneeIds.ToHashSet();
    var existingAssignees = card.Assignees.ToArray();
    var existingAssigneeIds = existingAssignees
      .Select(assignee => assignee.UserId)
      .ToHashSet();

    dbContext.BoardCardAssignees.RemoveRange(
      existingAssignees.Where(assignee => !requestedAssigneeIds.Contains(assignee.UserId)));

    dbContext.BoardCardAssignees.AddRange(
      requestedAssigneeIds
        .Where(assigneeId => !existingAssigneeIds.Contains(assigneeId))
        .Select(assigneeId => new BoardCardAssigneeEntity
        {
          Id = Guid.NewGuid(),
          CardId = cardId,
          UserId = assigneeId,
          AssignedAt = updatedAt
        }));

    await TouchBoardAsync(boardId, updatedAt, cancellationToken);
    await dbContext.SaveChangesAsync(cancellationToken);

    return await FindInProjectAsync(
      workspaceId,
      projectId,
      boardId,
      cancellationToken);
  }

  public async Task<ProjectBoard?> MoveCardAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid listId,
    BoardCardCompletion completion,
    DateTimeOffset updatedAt,
    CancellationToken cancellationToken)
  {
    var listExists = await ListExistsAsync(
      workspaceId,
      projectId,
      boardId,
      listId,
      cancellationToken);

    if (!listExists)
    {
      return null;
    }

    var nextPosition = await NextCardPositionAsync(
      workspaceId,
      projectId,
      boardId,
      listId,
      cancellationToken);

    var updated = await dbContext.BoardCards
      .Where(card => card.WorkspaceId == workspaceId
        && card.ProjectId == projectId
        && card.BoardId == boardId
        && card.Id == cardId)
      .ExecuteUpdateAsync(setters => setters
        .SetProperty(card => card.ListId, listId)
        .SetProperty(card => card.Position, nextPosition)
        .SetProperty(card => card.IsCompleted, completion.IsCompleted)
        .SetProperty(card => card.CompletedAt, completion.CompletedAt)
        .SetProperty(card => card.CompletedByUserId, completion.CompletedByUserId)
        .SetProperty(card => card.UpdatedAt, updatedAt),
        cancellationToken);

    if (updated == 0)
    {
      return null;
    }

    await TouchBoardAsync(boardId, updatedAt, cancellationToken);

    return await FindInProjectAsync(
      workspaceId,
      projectId,
      boardId,
      cancellationToken);
  }

  public async Task<ProjectBoard?> AddCardCommentAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid userId,
    string body,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken)
  {
    var cardExists = await CardExistsAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      cancellationToken);

    if (!cardExists)
    {
      return null;
    }

    dbContext.BoardCardComments.Add(new BoardCardCommentEntity
    {
      Id = Guid.NewGuid(),
      CardId = cardId,
      UserId = userId,
      Body = body,
      CreatedAt = createdAt
    });

    await TouchCardAsync(cardId, createdAt, cancellationToken);
    await TouchBoardAsync(boardId, createdAt, cancellationToken);
    await dbContext.SaveChangesAsync(cancellationToken);

    return await FindInProjectAsync(
      workspaceId,
      projectId,
      boardId,
      cancellationToken);
  }

  public async Task<ProjectBoard?> CreateCardSubtaskAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    string title,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken)
  {
    var cardExists = await CardExistsAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      cancellationToken);

    if (!cardExists)
    {
      return null;
    }

    var nextPosition = await dbContext.BoardCardSubtasks
      .Where(subtask => subtask.CardId == cardId)
      .Select(subtask => (int?)subtask.Position)
      .MaxAsync(cancellationToken) + 1 ?? 0;

    dbContext.BoardCardSubtasks.Add(new BoardCardSubtaskEntity
    {
      Id = Guid.NewGuid(),
      CardId = cardId,
      Title = title,
      IsCompleted = false,
      Position = nextPosition,
      CreatedAt = createdAt,
      UpdatedAt = createdAt
    });

    await TouchCardAsync(cardId, createdAt, cancellationToken);
    await TouchBoardAsync(boardId, createdAt, cancellationToken);
    await dbContext.SaveChangesAsync(cancellationToken);

    return await FindInProjectAsync(
      workspaceId,
      projectId,
      boardId,
      cancellationToken);
  }

  public async Task<ProjectBoard?> UpdateCardSubtaskAsync(
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
    var cardExists = await CardExistsAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      cancellationToken);

    if (!cardExists)
    {
      return null;
    }

    var subtask = await dbContext.BoardCardSubtasks
      .SingleOrDefaultAsync(subtask => subtask.CardId == cardId
        && subtask.Id == subtaskId,
        cancellationToken);

    if (subtask is null)
    {
      return null;
    }

    if (title is not null)
    {
      subtask.Title = title;
    }

    if (isCompleted is not null && subtask.IsCompleted != isCompleted.Value)
    {
      subtask.IsCompleted = isCompleted.Value;
      subtask.CompletedAt = isCompleted.Value ? updatedAt : null;
      subtask.CompletedByUserId = isCompleted.Value ? userId : null;
    }

    subtask.UpdatedAt = updatedAt;

    await TouchCardAsync(cardId, updatedAt, cancellationToken);
    await TouchBoardAsync(boardId, updatedAt, cancellationToken);
    await dbContext.SaveChangesAsync(cancellationToken);

    return await FindInProjectAsync(
      workspaceId,
      projectId,
      boardId,
      cancellationToken);
  }

  public async Task<bool> DeleteCardSubtaskAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid subtaskId,
    DateTimeOffset updatedAt,
    CancellationToken cancellationToken)
  {
    var cardExists = await CardExistsAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      cancellationToken);

    if (!cardExists)
    {
      return false;
    }

    var deleted = await dbContext.BoardCardSubtasks
      .Where(subtask => subtask.CardId == cardId && subtask.Id == subtaskId)
      .ExecuteDeleteAsync(cancellationToken);

    if (deleted == 0)
    {
      return false;
    }

    await TouchCardAsync(cardId, updatedAt, cancellationToken);
    await TouchBoardAsync(boardId, updatedAt, cancellationToken);

    return true;
  }

  public async Task<ProjectBoard?> AddCardDependencyAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid dependsOnCardId,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken)
  {
    var cardExists = await CardExistsAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      cancellationToken);
    var dependencyExists = await CardExistsAsync(
      workspaceId,
      projectId,
      boardId,
      dependsOnCardId,
      cancellationToken);

    if (!cardExists || !dependencyExists)
    {
      return null;
    }

    var alreadyLinked = await dbContext.BoardCardDependencies
      .AnyAsync(dependency => dependency.CardId == cardId
        && dependency.DependsOnCardId == dependsOnCardId,
        cancellationToken);

    if (!alreadyLinked)
    {
      dbContext.BoardCardDependencies.Add(new BoardCardDependencyEntity
      {
        Id = Guid.NewGuid(),
        CardId = cardId,
        DependsOnCardId = dependsOnCardId,
        CreatedAt = createdAt
      });

      await TouchCardAsync(cardId, createdAt, cancellationToken);
      await TouchBoardAsync(boardId, createdAt, cancellationToken);
      await dbContext.SaveChangesAsync(cancellationToken);
    }

    return await FindInProjectAsync(
      workspaceId,
      projectId,
      boardId,
      cancellationToken);
  }

  public async Task<bool> DeleteCardDependencyAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid dependsOnCardId,
    DateTimeOffset updatedAt,
    CancellationToken cancellationToken)
  {
    var cardExists = await CardExistsAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      cancellationToken);

    if (!cardExists)
    {
      return false;
    }

    var deleted = await dbContext.BoardCardDependencies
      .Where(dependency => dependency.CardId == cardId
        && dependency.DependsOnCardId == dependsOnCardId)
      .ExecuteDeleteAsync(cancellationToken);

    if (deleted == 0)
    {
      return false;
    }

    await TouchCardAsync(cardId, updatedAt, cancellationToken);
    await TouchBoardAsync(boardId, updatedAt, cancellationToken);

    return true;
  }

  public async Task<bool> DeleteCardAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    CancellationToken cancellationToken)
  {
    var cardExists = await CardExistsAsync(
      workspaceId,
      projectId,
      boardId,
      cardId,
      cancellationToken);

    if (!cardExists)
    {
      return false;
    }

    await dbContext.BoardCardDependencies
      .Where(dependency => dependency.CardId == cardId
        || dependency.DependsOnCardId == cardId)
      .ExecuteDeleteAsync(cancellationToken);

    var deleted = await dbContext.BoardCards
      .Where(card => card.Id == cardId)
      .ExecuteDeleteAsync(cancellationToken);

    return deleted > 0;
  }

  private async Task<bool> BoardExistsAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    CancellationToken cancellationToken)
  {
    return await dbContext.Boards
      .AsNoTracking()
      .AnyAsync(board => board.WorkspaceId == workspaceId
        && board.ProjectId == projectId
        && board.Id == boardId,
        cancellationToken);
  }

  private async Task<bool> ListExistsAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid listId,
    CancellationToken cancellationToken)
  {
    return await dbContext.BoardLists
      .AsNoTracking()
      .AnyAsync(list => list.WorkspaceId == workspaceId
        && list.ProjectId == projectId
        && list.BoardId == boardId
        && list.Id == listId,
        cancellationToken);
  }

  private async Task<int> NextCardPositionAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid listId,
    CancellationToken cancellationToken)
  {
    return await dbContext.BoardCards
      .Where(card => card.WorkspaceId == workspaceId
        && card.ProjectId == projectId
        && card.BoardId == boardId
        && card.ListId == listId)
      .Select(card => (int?)card.Position)
      .MaxAsync(cancellationToken) + 1 ?? 0;
  }

  private async Task<bool> CardExistsAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    CancellationToken cancellationToken)
  {
    return await dbContext.BoardCards
      .AsNoTracking()
      .AnyAsync(card => card.WorkspaceId == workspaceId
        && card.ProjectId == projectId
        && card.BoardId == boardId
        && card.Id == cardId,
        cancellationToken);
  }

  private async Task TouchCardAsync(
    Guid cardId,
    DateTimeOffset updatedAt,
    CancellationToken cancellationToken)
  {
    await dbContext.BoardCards
      .Where(card => card.Id == cardId)
      .ExecuteUpdateAsync(setters => setters
        .SetProperty(card => card.UpdatedAt, updatedAt),
        cancellationToken);
  }

  private async Task TouchBoardAsync(
    Guid boardId,
    DateTimeOffset updatedAt,
    CancellationToken cancellationToken)
  {
    await dbContext.Boards
      .Where(board => board.Id == boardId)
      .ExecuteUpdateAsync(setters => setters
        .SetProperty(board => board.UpdatedAt, updatedAt),
        cancellationToken);
  }

  private static ProjectBoardList ToList(
    BoardListEntity entity,
    IReadOnlyCollection<ProjectBoardCard> cards) =>
    new(
      entity.Id,
      entity.BoardId,
      entity.ProjectId,
      entity.WorkspaceId,
      entity.Title,
      entity.Position,
      entity.TaskStatus,
      entity.CreatedAt,
      entity.UpdatedAt,
      cards);

  private static ProjectBoardCard ToCard(
    BoardCardEntity entity,
    IReadOnlyCollection<ProjectBoardCardAssignee> assignees,
    IReadOnlyCollection<ProjectBoardCardComment> comments,
    IReadOnlyCollection<ProjectBoardCardSubtask> subtasks,
    IReadOnlyCollection<ProjectBoardCardDependency> dependencies,
    ProjectBoardCardUser? completedBy) =>
    new(
      entity.Id,
      entity.BoardId,
      entity.ListId,
      entity.ProjectId,
      entity.WorkspaceId,
      entity.Title,
      entity.Description,
      entity.Priority,
      entity.DueDate,
      entity.Labels,
      entity.IsCompleted,
      entity.CompletedAt,
      completedBy,
      entity.Position,
      entity.CreatedAt,
      entity.UpdatedAt,
      assignees,
      comments,
      subtasks,
      dependencies);
}
