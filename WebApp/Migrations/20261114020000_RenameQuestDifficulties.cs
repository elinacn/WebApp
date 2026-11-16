using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebApp.Models;

namespace WebApp.Migrations;

// Data-only migration
[DbContext(typeof(SheetsDbContext))]
[Migration("20261114020000_RenameQuestDifficulties")]
public class RenameQuestDifficulties : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            UPDATE Quests SET Difficulty = CASE lower(trim(Difficulty))
                WHEN 'apprentice' THEN 'Beginner'
                WHEN 'beginner' THEN 'Beginner'
                WHEN 'knight' THEN 'Intermediate'
                WHEN 'intermediate' THEN 'Intermediate'
                WHEN 'expert' THEN 'Expert'
                WHEN 'master' THEN 'Master'
                ELSE Difficulty END;");
    }

    // Multiple old spellings become one name
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
