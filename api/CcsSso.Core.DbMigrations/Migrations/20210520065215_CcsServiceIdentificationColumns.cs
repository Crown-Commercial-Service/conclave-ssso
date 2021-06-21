using Microsoft.EntityFrameworkCore.Migrations;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class CcsServiceIdentificationColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "CcsService",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceCode",
                table: "CcsService",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "CcsService");

            migrationBuilder.DropColumn(
                name: "ServiceCode",
                table: "CcsService");
        }
    }
}
