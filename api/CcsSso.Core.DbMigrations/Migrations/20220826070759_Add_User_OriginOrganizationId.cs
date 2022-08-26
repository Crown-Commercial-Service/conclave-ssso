using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class Add_User_OriginOrganizationId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OriginOrganizationId",
                table: "User",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_OriginOrganizationId",
                table: "User",
                column: "OriginOrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_User_Organisation_OriginOrganizationId",
                table: "User",
                column: "OriginOrganizationId",
                principalTable: "Organisation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_Organisation_OriginOrganizationId",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_User_OriginOrganizationId",
                table: "User");

            migrationBuilder.DropColumn(
                name: "OriginOrganizationId",
                table: "User");
        }
    }
}
