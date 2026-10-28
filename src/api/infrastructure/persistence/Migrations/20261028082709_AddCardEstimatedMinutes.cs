using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coordina.Api.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class AddCardEstimatedMinutes : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<int>(
          name: "estimated_minutes",
          table: "board_cards",
          type: "integer",
          nullable: true);

      migrationBuilder.AddCheckConstraint(
          name: "ck_board_cards_estimated_minutes",
          table: "board_cards",
          sql: "estimated_minutes IS NULL OR estimated_minutes BETWEEN 5 AND 2880");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropCheckConstraint(
          name: "ck_board_cards_estimated_minutes",
          table: "board_cards");

      migrationBuilder.DropColumn(
          name: "estimated_minutes",
          table: "board_cards");
    }
  }
}
