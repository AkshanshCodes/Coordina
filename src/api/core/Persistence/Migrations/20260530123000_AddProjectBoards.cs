using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coordina.Api.Core.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class AddProjectBoards : Migration
  {
    private static readonly string[] BoardScopeColumns =
    [
      "workspace_id",
      "project_id"
    ];

    private static readonly string[] ListScopeColumns =
    [
      "workspace_id",
      "project_id",
      "board_id"
    ];

    private static readonly string[] ListPositionColumns =
    [
      "board_id",
      "position"
    ];

    private static readonly string[] CardScopeColumns =
    [
      "workspace_id",
      "project_id",
      "board_id"
    ];

    private static readonly string[] CardPositionColumns =
    [
      "board_id",
      "list_id",
      "position"
    ];

    private static readonly string[] AssigneeUniqueColumns =
    [
      "card_id",
      "user_id"
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "boards",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            project_id = table.Column<Guid>(type: "uuid", nullable: false),
            workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
            name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
            template = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_boards", x => x.id);
            table.ForeignKey(
                      name: "FK_boards_projects_project_id",
                      column: x => x.project_id,
                      principalTable: "projects",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "board_lists",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            board_id = table.Column<Guid>(type: "uuid", nullable: false),
            project_id = table.Column<Guid>(type: "uuid", nullable: false),
            workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
            title = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
            position = table.Column<int>(type: "integer", nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_board_lists", x => x.id);
            table.ForeignKey(
                      name: "FK_board_lists_boards_board_id",
                      column: x => x.board_id,
                      principalTable: "boards",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "board_cards",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            board_id = table.Column<Guid>(type: "uuid", nullable: false),
            list_id = table.Column<Guid>(type: "uuid", nullable: false),
            project_id = table.Column<Guid>(type: "uuid", nullable: false),
            workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
            title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
            description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            priority = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
            due_date = table.Column<DateOnly>(type: "date", nullable: true),
            labels = table.Column<string[]>(type: "text[]", nullable: false),
            position = table.Column<int>(type: "integer", nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_board_cards", x => x.id);
            table.ForeignKey(
                      name: "FK_board_cards_board_lists_list_id",
                      column: x => x.list_id,
                      principalTable: "board_lists",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                      name: "FK_board_cards_boards_board_id",
                      column: x => x.board_id,
                      principalTable: "boards",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "board_card_assignees",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            card_id = table.Column<Guid>(type: "uuid", nullable: false),
            user_id = table.Column<Guid>(type: "uuid", nullable: false),
            assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_board_card_assignees", x => x.id);
            table.ForeignKey(
                      name: "FK_board_card_assignees_board_cards_card_id",
                      column: x => x.card_id,
                      principalTable: "board_cards",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateIndex(
          name: "IX_board_card_assignees_card_id_user_id",
          table: "board_card_assignees",
          columns: AssigneeUniqueColumns,
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_board_card_assignees_user_id",
          table: "board_card_assignees",
          column: "user_id");

      migrationBuilder.CreateIndex(
          name: "IX_board_cards_board_id_list_id_position",
          table: "board_cards",
          columns: CardPositionColumns);

      migrationBuilder.CreateIndex(
          name: "IX_board_cards_list_id",
          table: "board_cards",
          column: "list_id");

      migrationBuilder.CreateIndex(
          name: "IX_board_cards_workspace_id_project_id_board_id",
          table: "board_cards",
          columns: CardScopeColumns);

      migrationBuilder.CreateIndex(
          name: "IX_board_lists_board_id_position",
          table: "board_lists",
          columns: ListPositionColumns);

      migrationBuilder.CreateIndex(
          name: "IX_board_lists_workspace_id_project_id_board_id",
          table: "board_lists",
          columns: ListScopeColumns);

      migrationBuilder.CreateIndex(
          name: "IX_boards_project_id",
          table: "boards",
          column: "project_id");

      migrationBuilder.CreateIndex(
          name: "IX_boards_workspace_id_project_id",
          table: "boards",
          columns: BoardScopeColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "board_card_assignees");

      migrationBuilder.DropTable(
          name: "board_cards");

      migrationBuilder.DropTable(
          name: "board_lists");

      migrationBuilder.DropTable(
          name: "boards");
    }
  }
}
