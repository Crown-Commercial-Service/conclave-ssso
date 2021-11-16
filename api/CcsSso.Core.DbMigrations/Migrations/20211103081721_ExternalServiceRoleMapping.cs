using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class ExternalServiceRoleMapping : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GlobalLevelOrganisationAccess",
                table: "CcsService",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ExternalServiceRoleMapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CcsServiceId = table.Column<int>(type: "integer", nullable: false),
                    OrganisationEligibleRoleId = table.Column<int>(type: "integer", nullable: false),
                    CreatedUserId = table.Column<int>(type: "integer", nullable: false),
                    LastUpdatedUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ConcurrencyKey = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalServiceRoleMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalServiceRoleMapping_CcsService_CcsServiceId",
                        column: x => x.CcsServiceId,
                        principalTable: "CcsService",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExternalServiceRoleMapping_OrganisationEligibleRole_Organis~",
                        column: x => x.OrganisationEligibleRoleId,
                        principalTable: "OrganisationEligibleRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalServiceRoleMapping_CcsServiceId",
                table: "ExternalServiceRoleMapping",
                column: "CcsServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalServiceRoleMapping_OrganisationEligibleRoleId",
                table: "ExternalServiceRoleMapping",
                column: "OrganisationEligibleRoleId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalServiceRoleMapping");

            migrationBuilder.DropColumn(
                name: "GlobalLevelOrganisationAccess",
                table: "CcsService");
        }
    }
}
