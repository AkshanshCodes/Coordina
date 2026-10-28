using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coordina.Api.Core.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class AddProjects : Migration
  {
    private static readonly string[] WorkspaceNameIndexColumns =
    [
      "workspace_id",
      "name"
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "projects",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_projects", x => x.id);
            table.ForeignKey(
                      name: "FK_projects_workspaces_workspace_id",
                      column: x => x.workspace_id,
                      principalTable: "workspaces",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateIndex(
          name: "IX_projects_workspace_id",
          table: "projects",
          column: "workspace_id");

      migrationBuilder.CreateIndex(
          name: "IX_projects_workspace_id_name",
          table: "projects",
          columns: WorkspaceNameIndexColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "projects");
    }
  }
}
