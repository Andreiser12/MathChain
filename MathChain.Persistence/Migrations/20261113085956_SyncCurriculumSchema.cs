using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathChain.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncCurriculumSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FinalGrade",
                table: "Enrollments",
                type: "integer",
                nullable: true);

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

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "CurriculumChapters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FormulasCount",
                table: "CurriculumChapters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "CurriculumChapters",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClassAnnouncements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    AuthorWallet = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassAnnouncements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClassMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    IpfsHash = table.Column<string>(type: "text", nullable: false),
                    UploaderWallet = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassMedia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumFormulas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChapterId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormulaCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Latex = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Requirement = table.Column<string>(type: "text", nullable: true),
                    Model3D = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExercisesJson = table.Column<string>(type: "text", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumFormulas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumFormulas_CurriculumChapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "CurriculumChapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessagesComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorWallet = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagesComments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumChapters_ParentId",
                table: "CurriculumChapters",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumFormulas_ChapterId",
                table: "CurriculumFormulas",
                column: "ChapterId");

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

            migrationBuilder.DropTable(
                name: "ClassAnnouncements");

            migrationBuilder.DropTable(
                name: "ClassMedia");

            migrationBuilder.DropTable(
                name: "CurriculumFormulas");

            migrationBuilder.DropTable(
                name: "MessagesComments");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumChapters_ParentId",
                table: "CurriculumChapters");

            migrationBuilder.DropColumn(
                name: "FinalGrade",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "CurriculumChapters");

            migrationBuilder.DropColumn(
                name: "FormulasCount",
                table: "CurriculumChapters");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "CurriculumChapters");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "CurriculumChapters");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "CurriculumChapters");

            migrationBuilder.AlterColumn<string>(
                name: "IpfsHash",
                table: "CurriculumChapters",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "CurriculumChapters",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
