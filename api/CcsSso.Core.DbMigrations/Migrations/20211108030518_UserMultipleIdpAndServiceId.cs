using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class UserMultipleIdpAndServiceId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CcsServiceId",
                table: "User",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserIdentityProvider",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganisationEligibleIdentityProviderId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedUserId = table.Column<int>(type: "integer", nullable: false),
                    LastUpdatedUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ConcurrencyKey = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserIdentityProvider", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserIdentityProvider_OrganisationEligibleIdentityProvider_O~",
                        column: x => x.OrganisationEligibleIdentityProviderId,
                        principalTable: "OrganisationEligibleIdentityProvider",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserIdentityProvider_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_User_CcsServiceId",
                table: "User",
                column: "CcsServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserIdentityProvider_OrganisationEligibleIdentityProviderId",
                table: "UserIdentityProvider",
                column: "OrganisationEligibleIdentityProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_UserIdentityProvider_UserId",
                table: "UserIdentityProvider",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_User_CcsService_CcsServiceId",
                table: "User",
                column: "CcsServiceId",
                principalTable: "CcsService",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_CcsService_CcsServiceId",
                table: "User");

            migrationBuilder.DropTable(
                name: "UserIdentityProvider");

            migrationBuilder.DropIndex(
                name: "IX_User_CcsServiceId",
                table: "User");

            migrationBuilder.DropColumn(
                name: "CcsServiceId",
                table: "User");
        }
    }
}
