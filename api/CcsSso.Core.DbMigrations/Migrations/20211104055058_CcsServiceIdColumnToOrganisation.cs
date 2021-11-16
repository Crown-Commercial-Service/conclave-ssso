using Microsoft.EntityFrameworkCore.Migrations;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class CcsServiceIdColumnToOrganisation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CcsServiceId",
                table: "Organisation",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organisation_CcsServiceId",
                table: "Organisation",
                column: "CcsServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Organisation_CcsService_CcsServiceId",
                table: "Organisation",
                column: "CcsServiceId",
                principalTable: "CcsService",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organisation_CcsService_CcsServiceId",
                table: "Organisation");

            migrationBuilder.DropIndex(
                name: "IX_Organisation_CcsServiceId",
                table: "Organisation");

            migrationBuilder.DropColumn(
                name: "CcsServiceId",
                table: "Organisation");
        }
    }
}
