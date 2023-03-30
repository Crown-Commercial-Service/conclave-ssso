using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class Add_CcsServiceRoleGroup_CcsServiceRoleMapping : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CcsServiceRoleGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    OrgTypeEligibility = table.Column<int>(type: "integer", nullable: false),
                    SubscriptionTypeEligibility = table.Column<int>(type: "integer", nullable: false),
                    TradeEligibility = table.Column<int>(type: "integer", nullable: false),
                    MfaEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultEligibility = table.Column<string>(type: "text", nullable: true),
                    ApprovalRequired = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedUserId = table.Column<int>(type: "integer", nullable: false),
                    LastUpdatedUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ConcurrencyKey = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CcsServiceRoleGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CcsServiceRoleMapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CcsServiceRoleGroupId = table.Column<int>(type: "integer", nullable: false),
                    CcsAccessRoleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CcsServiceRoleMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CcsServiceRoleMapping_CcsAccessRole_CcsAccessRoleId",
                        column: x => x.CcsAccessRoleId,
                        principalTable: "CcsAccessRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CcsServiceRoleMapping_CcsServiceRoleGroup_CcsServiceRoleGro~",
                        column: x => x.CcsServiceRoleGroupId,
                        principalTable: "CcsServiceRoleGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CcsServiceRoleMapping_CcsAccessRoleId",
                table: "CcsServiceRoleMapping",
                column: "CcsAccessRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_CcsServiceRoleMapping_CcsServiceRoleGroupId",
                table: "CcsServiceRoleMapping",
                column: "CcsServiceRoleGroupId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CcsServiceRoleMapping");

            migrationBuilder.DropTable(
                name: "CcsServiceRoleGroup");
        }
    }
}
