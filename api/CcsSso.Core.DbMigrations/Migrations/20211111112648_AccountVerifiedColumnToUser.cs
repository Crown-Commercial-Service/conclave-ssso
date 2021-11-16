using Microsoft.EntityFrameworkCore.Migrations;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class AccountVerifiedColumnToUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AccountVerified",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountVerified",
                table: "User");
        }
    }
}
