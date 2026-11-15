using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathChain.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPracticeExercises : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PracticeExercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentWallet = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FormulaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseIndex = table.Column<int>(type: "integer", nullable: false),
                    Difficulty = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Requirement = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Latex = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Hint = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Form = table.Column<int>(type: "integer", nullable: false),
                    ExpectedAnswer = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeExercises", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeExercises_StudentWallet_FormulaId_ExerciseIndex",
                table: "PracticeExercises",
                columns: new[] { "StudentWallet", "FormulaId", "ExerciseIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PracticeExercises");
        }
    }
}
