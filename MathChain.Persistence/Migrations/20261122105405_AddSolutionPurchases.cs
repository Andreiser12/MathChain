using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathChain.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSolutionPurchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VariableValues",
                table: "PracticeExercises",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SolutionPurchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountWallet = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FormulaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseIndex = table.Column<int>(type: "integer", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    PriceUnits = table.Column<long>(type: "bigint", nullable: false),
                    PayerAddress = table.Column<string>(type: "character varying(42)", maxLength: 42, nullable: false),
                    SolutionId = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: false),
                    Salt = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: false),
                    Nonce = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: false),
                    ValidBefore = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TransactionHash = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolutionPurchases", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolutionPurchases_Account",
                table: "SolutionPurchases",
                columns: new[] { "AccountWallet", "FormulaId", "ExerciseIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_SolutionPurchases_Completed",
                table: "SolutionPurchases",
                columns: new[] { "AccountWallet", "FormulaId", "ExerciseIndex" },
                unique: true,
                filter: "\"Status\" = 3");

            migrationBuilder.CreateIndex(
                name: "IX_SolutionPurchases_TransactionHash",
                table: "SolutionPurchases",
                column: "TransactionHash",
                unique: true,
                filter: "\"TransactionHash\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolutionPurchases");

            migrationBuilder.DropColumn(
                name: "VariableValues",
                table: "PracticeExercises");
        }
    }
}
