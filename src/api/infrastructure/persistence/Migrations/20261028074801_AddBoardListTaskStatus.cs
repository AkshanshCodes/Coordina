using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coordina.Api.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class AddBoardListTaskStatus : Migration
  {
    private const string BackfillListStatusSql = """
      UPDATE board_lists AS l
      SET task_status = CASE
        WHEN r.list_rank = 1 THEN 'TODO'
        WHEN r.list_rank = r.list_total THEN 'DONE'
        ELSE 'IN_PROGRESS'
      END
      FROM (
        SELECT id,
          ROW_NUMBER() OVER (PARTITION BY board_id ORDER BY position, created_at, id) AS list_rank,
          COUNT(*) OVER (PARTITION BY board_id) AS list_total
        FROM board_lists
      ) AS r
      WHERE l.id = r.id;

      UPDATE board_lists AS l
      SET task_status = 'TODO'
      FROM boards AS b
      WHERE l.board_id = b.id
        AND l.task_status = 'IN_PROGRESS'
        AND ((b.template = 'AGILE_SCRUM' AND l.title = 'Sprint')
          OR (b.template = 'PRODUCT_ROADMAP' AND l.title = 'Planned'));
      """;

    private const string AlignCardCompletionSql = """
      UPDATE board_cards AS c
      SET is_completed = TRUE,
        completed_at = COALESCE(c.completed_at, c.updated_at)
      FROM board_lists AS l
      WHERE c.list_id = l.id
        AND l.task_status = 'DONE'
        AND NOT c.is_completed;

      UPDATE board_cards AS c
      SET is_completed = FALSE,
        completed_at = NULL,
        completed_by_user_id = NULL
      FROM board_lists AS l
      WHERE c.list_id = l.id
        AND l.task_status <> 'DONE'
        AND c.is_completed;
      """;

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<string>(
          name: "task_status",
          table: "board_lists",
          type: "character varying(16)",
          maxLength: 16,
          nullable: true);

      migrationBuilder.Sql(BackfillListStatusSql);
      migrationBuilder.Sql(AlignCardCompletionSql);

      migrationBuilder.AlterColumn<string>(
          name: "task_status",
          table: "board_lists",
          type: "character varying(16)",
          maxLength: 16,
          nullable: false,
          oldClrType: typeof(string),
          oldType: "character varying(16)",
          oldMaxLength: 16,
          oldNullable: true);

      migrationBuilder.AddCheckConstraint(
          name: "ck_board_lists_task_status",
          table: "board_lists",
          sql: "task_status IN ('TODO', 'IN_PROGRESS', 'DONE')");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropCheckConstraint(
          name: "ck_board_lists_task_status",
          table: "board_lists");

      migrationBuilder.DropColumn(
          name: "task_status",
          table: "board_lists");
    }
  }
}
