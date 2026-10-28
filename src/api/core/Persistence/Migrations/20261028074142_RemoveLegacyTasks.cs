using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coordina.Api.Core.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class RemoveLegacyTasks : Migration
  {
    private static readonly string[] ScopeColumns = ["workspace_id", "project_id"];

    private static readonly string[] ScopeStatusColumns =
      ["workspace_id", "project_id", "status"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "tasks");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "tasks",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            project_id = table.Column<Guid>(type: "uuid", nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
            priority = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
            status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
            title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
            updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_tasks", x => x.id);
            table.ForeignKey(
                      name: "FK_tasks_projects_project_id",
                      column: x => x.project_id,
                      principalTable: "projects",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateIndex(
          name: "IX_tasks_project_id",
          table: "tasks",
          column: "project_id");

      migrationBuilder.CreateIndex(
          name: "IX_tasks_workspace_id_project_id",
          table: "tasks",
          columns: ScopeColumns);

      migrationBuilder.CreateIndex(
          name: "IX_tasks_workspace_id_project_id_status",
          table: "tasks",
          columns: ScopeStatusColumns);
    }
  }
}
