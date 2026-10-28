using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coordina.Api.Core.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class AddWorkspaces : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "workspaces",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_workspaces", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "workspace_members",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
            user_id = table.Column<Guid>(type: "uuid", nullable: false),
            role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
            joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_workspace_members", x => x.id);
            table.ForeignKey(
                name: "FK_workspace_members_workspaces_workspace_id",
                column: x => x.workspace_id,
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateIndex(
          name: "IX_workspace_members_user_id",
          table: "workspace_members",
          column: "user_id");

#pragma warning disable CA1861
      migrationBuilder.CreateIndex(
          name: "IX_workspace_members_workspace_id_user_id",
          table: "workspace_members",
          columns: new[] { "workspace_id", "user_id" },
          unique: true);
#pragma warning restore CA1861
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "workspace_members");

      migrationBuilder.DropTable(
          name: "workspaces");
    }
  }
}
