using Coordina.Api.Modules.Projects.Domain;
using Coordina.Api.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coordina.Api.Modules.Projects.Infrastructure;

public sealed class ProjectEntityConfiguration
  : IEntityTypeConfiguration<ProjectEntity>
{
  public void Configure(EntityTypeBuilder<ProjectEntity> builder)
  {
    builder.ToTable("projects");

    builder.HasKey(project => project.Id);

    builder.Property(project => project.Id)
      .HasColumnName("id");

    builder.Property(project => project.Name)
      .HasColumnName("name")
      .HasMaxLength(100)
      .IsRequired();

    builder.Property(project => project.Description)
      .HasColumnName("description")
      .HasMaxLength(500);

    builder.Property(project => project.Key)
      .HasColumnName("key")
      .HasMaxLength(12);

    builder.Property(project => project.Icon)
      .HasColumnName("icon")
      .HasMaxLength(16);

    builder.Property(project => project.Color)
      .HasColumnName("color")
      .HasMaxLength(32);

    builder.Property(project => project.WorkspaceId)
      .HasColumnName("workspace_id")
      .IsRequired();

    builder.Property(project => project.ProjectOwnerId)
      .HasColumnName("project_owner_id")
      .IsRequired();

    builder.Property(project => project.Status)
      .HasColumnName("status")
      .HasConversion(
        status => EnumCode.Format(status),
        value => EnumCode.Parse<ProjectStatus>(value))
      .HasMaxLength(16)
      .IsRequired();

    builder.Property(project => project.CreatedAt)
      .HasColumnName("created_at")
      .IsRequired();

    builder.Property(project => project.UpdatedAt)
      .HasColumnName("updated_at")
      .IsRequired();

    builder.Property(project => project.ArchivedAt)
      .HasColumnName("archived_at");

    builder.HasIndex(project => project.WorkspaceId);
    builder.HasIndex(project => new { project.WorkspaceId, project.Name });
    builder.HasIndex(project => new { project.WorkspaceId, project.Status });
    builder.HasIndex(project => project.ProjectOwnerId);
  }
}
