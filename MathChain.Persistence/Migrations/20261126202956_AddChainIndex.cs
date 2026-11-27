using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathChain.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChainIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChainCertificates",
                columns: table => new
                {
                    TokenId = table.Column<long>(type: "bigint", nullable: false),
                    Holder = table.Column<string>(type: "character varying(42)", maxLength: 42, nullable: false),
                    Issuer = table.Column<string>(type: "character varying(42)", maxLength: 42, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    SubjectId = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: false),
                    DataHash = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: false),
                    ClaimedBySmartAccount = table.Column<bool>(type: "boolean", nullable: false),
                    IssuedBlock = table.Column<long>(type: "bigint", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IssuedTransaction = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: false),
                    Revoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedBy = table.Column<string>(type: "character varying(42)", maxLength: 42, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedTransaction = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChainCertificates", x => x.TokenId);
                });

            migrationBuilder.CreateTable(
                name: "ChainCurriculumPublications",
                columns: table => new
                {
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Cid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Publisher = table.Column<string>(type: "character varying(42)", maxLength: 42, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Block = table.Column<long>(type: "bigint", nullable: false),
                    Transaction = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChainCurriculumPublications", x => x.Version);
                });

            migrationBuilder.CreateTable(
                name: "ChainPriceChanges",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(38,0)", precision: 38, scale: 0, nullable: false),
                    Block = table.Column<long>(type: "bigint", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Transaction = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChainPriceChanges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChainSmartAccounts",
                columns: table => new
                {
                    Account = table.Column<string>(type: "character varying(42)", maxLength: 42, nullable: false),
                    Owner = table.Column<string>(type: "character varying(42)", maxLength: 42, nullable: false),
                    CreatedBlock = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Transaction = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChainSmartAccounts", x => x.Account);
                });

            migrationBuilder.CreateTable(
                name: "ChainSolutionPurchases",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Account = table.Column<string>(type: "character varying(42)", maxLength: 42, nullable: false),
                    SolutionId = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: false),
                    Payer = table.Column<string>(type: "character varying(42)", maxLength: 42, nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(38,0)", precision: 38, scale: 0, nullable: false),
                    Block = table.Column<long>(type: "bigint", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Transaction = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChainSolutionPurchases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IndexerCheckpoints",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChainId = table.Column<long>(type: "bigint", nullable: false),
                    LastBlock = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndexerCheckpoints", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChainCertificates_Holder",
                table: "ChainCertificates",
                column: "Holder");

            migrationBuilder.CreateIndex(
                name: "IX_ChainSmartAccounts_Owner",
                table: "ChainSmartAccounts",
                column: "Owner");

            migrationBuilder.CreateIndex(
                name: "IX_ChainSolutionPurchases_Account",
                table: "ChainSolutionPurchases",
                column: "Account");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChainCertificates");

            migrationBuilder.DropTable(
                name: "ChainCurriculumPublications");

            migrationBuilder.DropTable(
                name: "ChainPriceChanges");

            migrationBuilder.DropTable(
                name: "ChainSmartAccounts");

            migrationBuilder.DropTable(
                name: "ChainSolutionPurchases");

            migrationBuilder.DropTable(
                name: "IndexerCheckpoints");
        }
    }
}
