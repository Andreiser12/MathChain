using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathChain.API.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeworkAndSubmissionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Assignmentss",
                table: "Assignmentss");

            migrationBuilder.RenameTable(
                name: "Assignmentss",
                newName: "Assignments");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpfsHash",
                table: "Submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Assignments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpfsHash",
                table: "Assignments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartTime",
                table: "Assignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Assignments",
                table: "Assignments",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ClassMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    IpfsHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassMaterials", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassMaterials");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Assignments",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "IpfsHash",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "IpfsHash",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "Assignments");

            migrationBuilder.RenameTable(
                name: "Assignments",
                newName: "Assignmentss");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Assignmentss",
                table: "Assignmentss",
                column: "Id");
        }
    }
}
