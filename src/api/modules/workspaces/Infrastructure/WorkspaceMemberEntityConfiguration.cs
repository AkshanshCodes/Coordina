using Coordina.Api.Modules.Workspaces.Domain;
using Coordina.Api.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coordina.Api.Modules.Workspaces.Infrastructure;

public sealed class WorkspaceMemberEntityConfiguration
  : IEntityTypeConfiguration<WorkspaceMemberEntity>
{
  public void Configure(EntityTypeBuilder<WorkspaceMemberEntity> builder)
  {
    builder.ToTable("workspace_members");

    builder.HasKey(member => member.Id);

    builder.Property(member => member.Id)
      .HasColumnName("id");

    builder.Property(member => member.WorkspaceId)
      .HasColumnName("workspace_id")
      .IsRequired();

    builder.Property(member => member.UserId)
      .HasColumnName("user_id")
      .IsRequired();

    builder.Property(member => member.Role)
      .HasColumnName("role")
      .HasConversion(
        role => EnumCode.Format(role),
        value => EnumCode.Parse<WorkspaceRole>(value))
      .HasMaxLength(16)
      .IsRequired();

    builder.Property(member => member.JoinedAt)
      .HasColumnName("joined_at")
      .IsRequired();

    builder.HasIndex(member => new { member.WorkspaceId, member.UserId })
      .IsUnique();

    builder.HasIndex(member => member.UserId);
  }
}
