using Microsoft.EntityFrameworkCore.Migrations;

namespace CcsSso.Adaptor.DbMigration.Migrations
{
    public partial class AddUniqueConstrainToAdapterConsumerEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AdapterConsumerEntity_Name_AdapterConsumerId",
                table: "AdapterConsumerEntity",
                columns: new[] { "Name", "AdapterConsumerId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdapterConsumerEntity_Name_AdapterConsumerId",
                table: "AdapterConsumerEntity");
        }
    }
}
