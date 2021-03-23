using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class AddSiteRelatedEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSite",
                table: "ContactPoint",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SiteName",
                table: "ContactPoint",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SiteContact",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContactPointId = table.Column<int>(type: "integer", nullable: false),
                    ContactId = table.Column<int>(type: "integer", nullable: false),
                    CreatedPartyId = table.Column<int>(type: "integer", nullable: false),
                    LastUpdatedPartyId = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ConcurrencyKey = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteContact", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteContact_ContactPoint_ContactPointId",
                        column: x => x.ContactPointId,
                        principalTable: "ContactPoint",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteContact_ContactPointId",
                table: "SiteContact",
                column: "ContactPointId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteContact");

            migrationBuilder.DropColumn(
                name: "IsSite",
                table: "ContactPoint");

            migrationBuilder.DropColumn(
                name: "SiteName",
                table: "ContactPoint");
        }
    }
}
