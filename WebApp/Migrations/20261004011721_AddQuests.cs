using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddQuests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Difficulty = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuestQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    QuestId = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Question = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrectOptionId = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    AcceptedAnswers = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Explaination = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestQuestions_Quests_QuestId",
                        column: x => x.QuestId,
                        principalTable: "Quests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    QuestQuestionId = table.Column<int>(type: "INTEGER", nullable: false),
                    OptionKey = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestOptions_QuestQuestions_QuestQuestionId",
                        column: x => x.QuestQuestionId,
                        principalTable: "QuestQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestOptions_QuestQuestionId",
                table: "QuestOptions",
                column: "QuestQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestQuestions_QuestId",
                table: "QuestQuestions",
                column: "QuestId");

            migrationBuilder.CreateIndex(
                name: "IX_Quests_Title",
                table: "Quests",
                column: "Title",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestOptions");

            migrationBuilder.DropTable(
                name: "QuestQuestions");

            migrationBuilder.DropTable(
                name: "Quests");
        }
    }
}
