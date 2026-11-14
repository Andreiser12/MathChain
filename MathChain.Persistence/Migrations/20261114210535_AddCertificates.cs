using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathChain.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Certificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StudentWallet = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TeacherWallet = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Grade = table.Column<int>(type: "integer", nullable: false),
                    Salt = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: false),
                    DataHash = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: false),
                    TokenId = table.Column<long>(type: "bigint", nullable: true),
                    TransactionHash = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Kind_SubjectId_StudentWallet",
                table: "Certificates",
                columns: new[] { "Kind", "SubjectId", "StudentWallet" },
                unique: true,
                filter: "\"Status\" <> 2");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_StudentWallet",
                table: "Certificates",
                column: "StudentWallet");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_TokenId",
                table: "Certificates",
                column: "TokenId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Certificates");
        }
    }
}
