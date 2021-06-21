using Microsoft.EntityFrameworkCore.Migrations;

namespace CcsSso.Adaptor.DbMigration.Migrations
{
    public partial class AddClientIdForConsumer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "AdapterConsumer",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdapterConsumer_ClientId",
                table: "AdapterConsumer",
                column: "ClientId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdapterConsumer_ClientId",
                table: "AdapterConsumer");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "AdapterConsumer");
        }
    }
}
