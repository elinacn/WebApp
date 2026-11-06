using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Migrations
{
    /// <inheritdoc />
    public partial class ResetCodePoints : Migration
    {
        /// <inheritdoc />
        // One-off reset when quest completions started awarding code points:
        // the demo users' made-up scores are removed and everyone starts at 0.
        // Runs once per database, so each teammate's copy is reset on their next start.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM QuestCompletions;");
            migrationBuilder.Sql("UPDATE Users SET Points = 0, QuizzesCompleted = 0;");
        }

        /// <inheritdoc />
        // The old scores can't be restored, so undoing this migration changes nothing
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
