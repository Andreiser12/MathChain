using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathChain.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificateHolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HolderAddress",
                table: "Certificates",
                type: "character varying(42)",
                maxLength: 42,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HolderAddress",
                table: "Certificates");
        }
    }
}
