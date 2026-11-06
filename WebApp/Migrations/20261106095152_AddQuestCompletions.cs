using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestCompletions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuestCompletions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuestId = table.Column<int>(type: "INTEGER", nullable: false),
                    BestScore = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxScore = table.Column<int>(type: "INTEGER", nullable: false),
                    Attempts = table.Column<int>(type: "INTEGER", nullable: false),
                    FirstCompletedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastCompletedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestCompletions_Quests_QuestId",
                        column: x => x.QuestId,
                        principalTable: "Quests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestCompletions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestCompletions_QuestId",
                table: "QuestCompletions",
                column: "QuestId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestCompletions_UserId_QuestId",
                table: "QuestCompletions",
                columns: new[] { "UserId", "QuestId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestCompletions");
        }
    }
}
