using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coordina.Api.Core.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class AddBoardCardCollaboration : Migration
  {
    private static readonly string[] CommentTimelineColumns =
    [
        "card_id",
            "created_at"
    ];

    private static readonly string[] DependencyUniqueColumns =
    [
        "card_id",
            "depends_on_card_id"
    ];

    private static readonly string[] SubtaskPositionColumns =
    [
        "card_id",
            "position"
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<DateTimeOffset>(
          name: "completed_at",
          table: "board_cards",
          type: "timestamp with time zone",
          nullable: true);

      migrationBuilder.AddColumn<Guid>(
          name: "completed_by_user_id",
          table: "board_cards",
          type: "uuid",
          nullable: true);

      migrationBuilder.AddColumn<bool>(
          name: "is_completed",
          table: "board_cards",
          type: "boolean",
          nullable: false,
          defaultValue: false);

      migrationBuilder.CreateTable(
          name: "board_card_comments",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            card_id = table.Column<Guid>(type: "uuid", nullable: false),
            user_id = table.Column<Guid>(type: "uuid", nullable: false),
            body = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_board_card_comments", x => x.id);
            table.ForeignKey(
                      name: "FK_board_card_comments_board_cards_card_id",
                      column: x => x.card_id,
                      principalTable: "board_cards",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "board_card_dependencies",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            card_id = table.Column<Guid>(type: "uuid", nullable: false),
            depends_on_card_id = table.Column<Guid>(type: "uuid", nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_board_card_dependencies", x => x.id);
            table.ForeignKey(
                      name: "FK_board_card_dependencies_board_cards_card_id",
                      column: x => x.card_id,
                      principalTable: "board_cards",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "board_card_subtasks",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            card_id = table.Column<Guid>(type: "uuid", nullable: false),
            title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
            is_completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
            completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            completed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
            position = table.Column<int>(type: "integer", nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_board_card_subtasks", x => x.id);
            table.ForeignKey(
                      name: "FK_board_card_subtasks_board_cards_card_id",
                      column: x => x.card_id,
                      principalTable: "board_cards",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateIndex(
          name: "IX_board_cards_completed_by_user_id",
          table: "board_cards",
          column: "completed_by_user_id");

      migrationBuilder.CreateIndex(
          name: "IX_board_card_comments_card_id",
          table: "board_card_comments",
          column: "card_id");

      migrationBuilder.CreateIndex(
          name: "IX_board_card_comments_card_id_created_at",
          table: "board_card_comments",
          columns: CommentTimelineColumns);

      migrationBuilder.CreateIndex(
          name: "IX_board_card_comments_user_id",
          table: "board_card_comments",
          column: "user_id");

      migrationBuilder.CreateIndex(
          name: "IX_board_card_dependencies_card_id_depends_on_card_id",
          table: "board_card_dependencies",
          columns: DependencyUniqueColumns,
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_board_card_dependencies_depends_on_card_id",
          table: "board_card_dependencies",
          column: "depends_on_card_id");

      migrationBuilder.CreateIndex(
          name: "IX_board_card_subtasks_card_id_position",
          table: "board_card_subtasks",
          columns: SubtaskPositionColumns);

      migrationBuilder.CreateIndex(
          name: "IX_board_card_subtasks_completed_by_user_id",
          table: "board_card_subtasks",
          column: "completed_by_user_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "board_card_comments");

      migrationBuilder.DropTable(
          name: "board_card_dependencies");

      migrationBuilder.DropTable(
          name: "board_card_subtasks");

      migrationBuilder.DropIndex(
          name: "IX_board_cards_completed_by_user_id",
          table: "board_cards");

      migrationBuilder.DropColumn(
          name: "completed_at",
          table: "board_cards");

      migrationBuilder.DropColumn(
          name: "completed_by_user_id",
          table: "board_cards");

      migrationBuilder.DropColumn(
          name: "is_completed",
          table: "board_cards");
    }
  }
}
