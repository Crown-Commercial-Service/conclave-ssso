using Microsoft.EntityFrameworkCore.Migrations;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class AddNewColumbnsToCcsServiceEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ServiceClientId",
                table: "CcsService",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceUrl",
                table: "CcsService",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServiceClientId",
                table: "CcsService");

            migrationBuilder.DropColumn(
                name: "ServiceUrl",
                table: "CcsService");
        }
    }
}
