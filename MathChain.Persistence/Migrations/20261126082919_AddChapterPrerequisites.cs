using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathChain.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChapterPrerequisites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChapterPrerequisites",
                columns: table => new
                {
                    ChapterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrerequisiteId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterPrerequisites", x => new { x.ChapterId, x.PrerequisiteId });
                    table.CheckConstraint("CK_ChapterPrerequisites_NotSelf", "\"ChapterId\" <> \"PrerequisiteId\"");
                    table.ForeignKey(
                        name: "FK_ChapterPrerequisites_CurriculumChapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "CurriculumChapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChapterPrerequisites_CurriculumChapters_PrerequisiteId",
                        column: x => x.PrerequisiteId,
                        principalTable: "CurriculumChapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChapterPrerequisites_PrerequisiteId",
                table: "ChapterPrerequisites",
                column: "PrerequisiteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChapterPrerequisites");
        }
    }
}
