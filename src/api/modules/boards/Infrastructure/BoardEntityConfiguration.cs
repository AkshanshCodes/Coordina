using Coordina.Api.Modules.Boards.Domain;
using Coordina.Api.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coordina.Api.Modules.Boards.Infrastructure;

public sealed class BoardEntityConfiguration
  : IEntityTypeConfiguration<BoardEntity>
{
  public void Configure(EntityTypeBuilder<BoardEntity> builder)
  {
    builder.ToTable("boards");

    builder.HasKey(board => board.Id);

    builder.Property(board => board.Id)
      .HasColumnName("id");

    builder.Property(board => board.ProjectId)
      .HasColumnName("project_id")
      .IsRequired();

    builder.Property(board => board.WorkspaceId)
      .HasColumnName("workspace_id")
      .IsRequired();

    builder.Property(board => board.Name)
      .HasColumnName("name")
      .HasMaxLength(120)
      .IsRequired();

    builder.Property(board => board.Template)
      .HasColumnName("template")
      .HasConversion(
        template => EnumCode.Format(template),
        value => EnumCode.Parse<BoardTemplate>(value))
      .HasMaxLength(24)
      .IsRequired();

    builder.Property(board => board.CreatedAt)
      .HasColumnName("created_at")
      .IsRequired();

    builder.Property(board => board.UpdatedAt)
      .HasColumnName("updated_at")
      .IsRequired();

    builder.HasIndex(board => new { board.WorkspaceId, board.ProjectId });
    builder.HasIndex(board => board.ProjectId);

    builder.HasOne(board => board.Project)
      .WithMany()
      .HasForeignKey(board => board.ProjectId)
      .OnDelete(DeleteBehavior.Cascade);
  }
}
