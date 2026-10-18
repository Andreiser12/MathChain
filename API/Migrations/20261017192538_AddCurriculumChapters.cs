using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathChain.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCurriculumChapters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CurriculumChapters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChapterSlug = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    IpfsHash = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumChapters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumChapters_ChapterSlug",
                table: "CurriculumChapters",
                column: "ChapterSlug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurriculumChapters");
        }
    }
}
