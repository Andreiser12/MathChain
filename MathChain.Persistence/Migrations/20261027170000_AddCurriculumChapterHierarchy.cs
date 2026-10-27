using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathChain.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCurriculumChapterHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "CurriculumChapters",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "CurriculumChapters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "CurriculumChapters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "CurriculumChapters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "IpfsHash",
                table: "CurriculumChapters",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "CurriculumChapters",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumChapters_ParentId",
                table: "CurriculumChapters",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_CurriculumChapters_CurriculumChapters_ParentId",
                table: "CurriculumChapters",
                column: "ParentId",
                principalTable: "CurriculumChapters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CurriculumChapters_CurriculumChapters_ParentId",
                table: "CurriculumChapters");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumChapters_ParentId",
                table: "CurriculumChapters");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "CurriculumChapters");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "CurriculumChapters");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "CurriculumChapters");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "CurriculumChapters");
        }
    }
}
