using Microsoft.EntityFrameworkCore.Migrations;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class Add_IdentityProvider_DisplayOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "IdentityProvider",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "IdentityProvider");
        }
    }
}
