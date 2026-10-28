using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coordina.Api.Core.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class AddProjectLifecycle : Migration
  {
    private static readonly string[] WorkspaceStatusIndexColumns =
    [
        "workspace_id",
            "status"
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<DateTimeOffset>(
          name: "archived_at",
          table: "projects",
          type: "timestamp with time zone",
          nullable: true);

      migrationBuilder.AddColumn<string>(
          name: "color",
          table: "projects",
          type: "character varying(32)",
          maxLength: 32,
          nullable: true);

      migrationBuilder.AddColumn<string>(
          name: "icon",
          table: "projects",
          type: "character varying(16)",
          maxLength: 16,
          nullable: true);

      migrationBuilder.AddColumn<string>(
          name: "key",
          table: "projects",
          type: "character varying(12)",
          maxLength: 12,
          nullable: true);

      migrationBuilder.AddColumn<Guid>(
          name: "project_owner_id",
          table: "projects",
          type: "uuid",
          nullable: true);

      migrationBuilder.AddColumn<string>(
          name: "status",
          table: "projects",
          type: "character varying(16)",
          maxLength: 16,
          nullable: false,
          defaultValue: "ACTIVE");

      migrationBuilder.AddColumn<DateTimeOffset>(
          name: "updated_at",
          table: "projects",
          type: "timestamp with time zone",
          nullable: false,
          defaultValueSql: "CURRENT_TIMESTAMP");

      migrationBuilder.Sql("""
                UPDATE projects AS p
                SET project_owner_id = (
                        SELECT wm.user_id
                        FROM workspace_members AS wm
                        WHERE wm.workspace_id = p.workspace_id
                          AND wm.role = 'OWNER'
                        ORDER BY wm.joined_at
                        LIMIT 1
                    ),
                    updated_at = p.created_at,
                    status = 'ACTIVE'
                """);

      migrationBuilder.AlterColumn<Guid>(
          name: "project_owner_id",
          table: "projects",
          type: "uuid",
          nullable: false,
          oldClrType: typeof(Guid),
          oldType: "uuid",
          oldNullable: true);

      migrationBuilder.CreateIndex(
          name: "IX_projects_project_owner_id",
          table: "projects",
          column: "project_owner_id");

      migrationBuilder.CreateIndex(
          name: "IX_projects_workspace_id_status",
          table: "projects",
          columns: WorkspaceStatusIndexColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropIndex(
          name: "IX_projects_project_owner_id",
          table: "projects");

      migrationBuilder.DropIndex(
          name: "IX_projects_workspace_id_status",
          table: "projects");

      migrationBuilder.DropColumn(
          name: "archived_at",
          table: "projects");

      migrationBuilder.DropColumn(
          name: "color",
          table: "projects");

      migrationBuilder.DropColumn(
          name: "icon",
          table: "projects");

      migrationBuilder.DropColumn(
          name: "key",
          table: "projects");

      migrationBuilder.DropColumn(
          name: "project_owner_id",
          table: "projects");

      migrationBuilder.DropColumn(
          name: "status",
          table: "projects");

      migrationBuilder.DropColumn(
          name: "updated_at",
          table: "projects");
    }
  }
}
