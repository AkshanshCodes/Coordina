using Coordina.Api.Modules.Boards.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coordina.Api.Modules.Boards.Infrastructure;

public sealed class BoardListEntityConfiguration
  : IEntityTypeConfiguration<BoardListEntity>
{
  public void Configure(EntityTypeBuilder<BoardListEntity> builder)
  {
    builder.ToTable("board_lists", table => table.HasCheckConstraint(
      "ck_board_lists_task_status",
      "task_status IN ('TODO', 'IN_PROGRESS', 'DONE')"));

    builder.HasKey(list => list.Id);

    builder.Property(list => list.Id)
      .HasColumnName("id");

    builder.Property(list => list.BoardId)
      .HasColumnName("board_id")
      .IsRequired();

    builder.Property(list => list.ProjectId)
      .HasColumnName("project_id")
      .IsRequired();

    builder.Property(list => list.WorkspaceId)
      .HasColumnName("workspace_id")
      .IsRequired();

    builder.Property(list => list.Title)
      .HasColumnName("title")
      .HasMaxLength(80)
      .IsRequired();

    builder.Property(list => list.Position)
      .HasColumnName("position")
      .IsRequired();

    builder.Property(list => list.TaskStatus)
      .HasColumnName("task_status")
      .HasConversion(
        status => BoardTaskStatusCodec.Format(status),
        value => BoardTaskStatusCodec.Parse(value))
      .HasMaxLength(16)
      .IsRequired();

    builder.Property(list => list.CreatedAt)
      .HasColumnName("created_at")
      .IsRequired();

    builder.Property(list => list.UpdatedAt)
      .HasColumnName("updated_at")
      .IsRequired();

    builder.HasIndex(list => new { list.WorkspaceId, list.ProjectId, list.BoardId });
    builder.HasIndex(list => new { list.BoardId, list.Position });

    builder.HasOne(list => list.Board)
      .WithMany(board => board.Lists)
      .HasForeignKey(list => list.BoardId)
      .OnDelete(DeleteBehavior.Cascade);
  }
}
