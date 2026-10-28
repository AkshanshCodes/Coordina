using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coordina.Api.Core.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class AddWorkspaceInvitesAndMembers : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "workspace_invites",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
            created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
            consumed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
            code_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_workspace_invites", x => x.id);
            table.ForeignKey(
                      name: "FK_workspace_invites_workspaces_workspace_id",
                      column: x => x.workspace_id,
                      principalTable: "workspaces",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateIndex(
          name: "IX_workspace_invites_code_hash",
          table: "workspace_invites",
          column: "code_hash",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_workspace_invites_workspace_id",
          table: "workspace_invites",
          column: "workspace_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "workspace_invites");
    }
  }
}
