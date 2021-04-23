using Microsoft.EntityFrameworkCore.Migrations;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class OrganisationEntityChanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "RightToBuy",
                table: "Organisation",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddColumn<string>(
                name: "BusinessType",
                table: "Organisation",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplierBuyerType",
                table: "Organisation",
                type: "integer",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessType",
                table: "Organisation");

            migrationBuilder.DropColumn(
                name: "SupplierBuyerType",
                table: "Organisation");

            migrationBuilder.AlterColumn<bool>(
                name: "RightToBuy",
                table: "Organisation",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);
        }
    }
}
