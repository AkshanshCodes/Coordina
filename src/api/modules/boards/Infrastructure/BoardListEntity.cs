using Coordina.Api.Modules.Boards.Domain;

namespace Coordina.Api.Modules.Boards.Infrastructure;

public sealed class BoardListEntity
{
  public Guid Id { get; set; }
  public Guid BoardId { get; set; }
  public Guid ProjectId { get; set; }
  public Guid WorkspaceId { get; set; }
  public required string Title { get; set; }
  public int Position { get; set; }
  public BoardTaskStatus TaskStatus { get; set; }
  public DateTimeOffset CreatedAt { get; set; }
  public DateTimeOffset UpdatedAt { get; set; }

  public BoardEntity? Board { get; set; }
  public ICollection<BoardCardEntity> Cards { get; set; } = [];
}
