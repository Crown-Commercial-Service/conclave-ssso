using Microsoft.EntityFrameworkCore.Migrations;

namespace CcsSso.Adaptor.DbMigration.Migrations
{
    public partial class AddSubscriptionFormat : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsumerKey",
                table: "AdapterConsumer");

            migrationBuilder.AddColumn<int>(
                name: "AdapterFormatId",
                table: "AdapterSubscription",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AdapterSubscription_AdapterFormatId",
                table: "AdapterSubscription",
                column: "AdapterFormatId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdapterSubscription_AdapterFormat_AdapterFormatId",
                table: "AdapterSubscription",
                column: "AdapterFormatId",
                principalTable: "AdapterFormat",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdapterSubscription_AdapterFormat_AdapterFormatId",
                table: "AdapterSubscription");

            migrationBuilder.DropIndex(
                name: "IX_AdapterSubscription_AdapterFormatId",
                table: "AdapterSubscription");

            migrationBuilder.DropColumn(
                name: "AdapterFormatId",
                table: "AdapterSubscription");

            migrationBuilder.AddColumn<string>(
                name: "ConsumerKey",
                table: "AdapterConsumer",
                type: "text",
                nullable: true);
        }
    }
}
