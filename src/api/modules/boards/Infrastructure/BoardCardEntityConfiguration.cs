using Coordina.Api.Modules.Boards.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coordina.Api.Modules.Boards.Infrastructure;

public sealed class BoardCardEntityConfiguration
  : IEntityTypeConfiguration<BoardCardEntity>
{
  public void Configure(EntityTypeBuilder<BoardCardEntity> builder)
  {
    builder.ToTable("board_cards", table => table.HasCheckConstraint(
      "ck_board_cards_estimated_minutes",
      "estimated_minutes IS NULL OR estimated_minutes BETWEEN 5 AND 2880"));

    builder.HasKey(card => card.Id);

    builder.Property(card => card.Id)
      .HasColumnName("id");

    builder.Property(card => card.BoardId)
      .HasColumnName("board_id")
      .IsRequired();

    builder.Property(card => card.ListId)
      .HasColumnName("list_id")
      .IsRequired();

    builder.Property(card => card.ProjectId)
      .HasColumnName("project_id")
      .IsRequired();

    builder.Property(card => card.WorkspaceId)
      .HasColumnName("workspace_id")
      .IsRequired();

    builder.Property(card => card.Title)
      .HasColumnName("title")
      .HasMaxLength(180)
      .IsRequired();

    builder.Property(card => card.Description)
      .HasColumnName("description")
      .HasMaxLength(2000);

    builder.Property(card => card.Priority)
      .HasColumnName("priority")
      .HasConversion(
        priority => priority == null
          ? null
          : priority.Value.ToString().ToUpperInvariant(),
        value => string.IsNullOrWhiteSpace(value)
          ? null
          : Enum.Parse<BoardCardPriority>(value, true))
      .HasMaxLength(12);

    builder.Property(card => card.DueDate)
      .HasColumnName("due_date");

    builder.Property(card => card.EstimatedMinutes)
      .HasColumnName("estimated_minutes");

    builder.Property(card => card.Labels)
      .HasColumnName("labels")
      .HasColumnType("text[]")
      .IsRequired();

    builder.Property(card => card.IsCompleted)
      .HasColumnName("is_completed")
      .HasDefaultValue(false)
      .IsRequired();

    builder.Property(card => card.CompletedAt)
      .HasColumnName("completed_at");

    builder.Property(card => card.CompletedByUserId)
      .HasColumnName("completed_by_user_id");

    builder.Property(card => card.Position)
      .HasColumnName("position")
      .IsRequired();

    builder.Property(card => card.CreatedAt)
      .HasColumnName("created_at")
      .IsRequired();

    builder.Property(card => card.UpdatedAt)
      .HasColumnName("updated_at")
      .IsRequired();

    builder.HasIndex(card => new { card.WorkspaceId, card.ProjectId, card.BoardId });
    builder.HasIndex(card => new { card.BoardId, card.ListId, card.Position });
    builder.HasIndex(card => card.CompletedByUserId);
    builder.HasIndex(card => card.ListId);

    builder.HasOne(card => card.Board)
      .WithMany()
      .HasForeignKey(card => card.BoardId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(card => card.List)
      .WithMany(list => list.Cards)
      .HasForeignKey(card => card.ListId)
      .OnDelete(DeleteBehavior.Cascade);
  }
}
