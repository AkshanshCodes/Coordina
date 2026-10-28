using Coordina.Api.Modules.Boards.Domain;

namespace Coordina.Api.Modules.Boards.Application;

public interface IBoardStore
{
  Task<ProjectBoard?> FindDefaultForProjectAsync(
    Guid workspaceId,
    Guid projectId,
    CancellationToken cancellationToken);

  Task<ProjectBoard?> FindInProjectAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    CancellationToken cancellationToken);

  Task<ProjectBoard> CreateAsync(
    Guid workspaceId,
    Guid projectId,
    string name,
    BoardTemplate boardTemplate,
    IReadOnlyCollection<BoardListSeed> listSeeds,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken);

  Task<ProjectBoard?> CreateListAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    string title,
    BoardTaskStatus taskStatus,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken);

  Task<ProjectBoard?> UpdateListAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid listId,
    string title,
    DateTimeOffset updatedAt,
    CancellationToken cancellationToken);

  Task<ProjectBoardCard?> FindCardAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    CancellationToken cancellationToken);

  Task<ProjectBoard?> CreateCardAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid listId,
    BoardCardMutation mutation,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken);

  Task<ProjectBoard?> UpdateCardAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    BoardCardMutation mutation,
    DateTimeOffset updatedAt,
    CancellationToken cancellationToken);

  Task<ProjectBoard?> MoveCardAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid listId,
    DateTimeOffset updatedAt,
    CancellationToken cancellationToken);

  Task<ProjectBoard?> AddCardCommentAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid userId,
    string body,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken);

  Task<ProjectBoard?> CreateCardSubtaskAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    string title,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken);

  Task<ProjectBoard?> UpdateCardSubtaskAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid subtaskId,
    string? title,
    bool? isCompleted,
    Guid userId,
    DateTimeOffset updatedAt,
    CancellationToken cancellationToken);

  Task<bool> DeleteCardSubtaskAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid subtaskId,
    DateTimeOffset updatedAt,
    CancellationToken cancellationToken);

  Task<ProjectBoard?> AddCardDependencyAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid dependsOnCardId,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken);

  Task<bool> DeleteCardDependencyAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    Guid dependsOnCardId,
    DateTimeOffset updatedAt,
    CancellationToken cancellationToken);

  Task<bool> DeleteCardAsync(
    Guid workspaceId,
    Guid projectId,
    Guid boardId,
    Guid cardId,
    CancellationToken cancellationToken);
}

public sealed record BoardCardMutation(
  string Title,
  string? Description,
  BoardCardPriority? Priority,
  DateOnly? DueDate,
  IReadOnlyCollection<string> Labels,
  IReadOnlyCollection<Guid> AssigneeIds,
  bool IsCompleted,
  DateTimeOffset? CompletedAt,
  Guid? CompletedByUserId);

public sealed record BoardListSeed(
  string Title,
  BoardTaskStatus TaskStatus,
  IReadOnlyCollection<BoardCardSeed> Cards);

public sealed record BoardCardSeed(
  string Title,
  string? Description,
  BoardCardPriority? Priority,
  IReadOnlyCollection<string> Labels);
