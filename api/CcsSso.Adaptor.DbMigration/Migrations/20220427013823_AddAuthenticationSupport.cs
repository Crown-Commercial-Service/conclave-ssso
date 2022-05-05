using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace CcsSso.Adaptor.DbMigration.Migrations
{
    public partial class AddAuthenticationSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdapterConsumerSubscriptionAuthMethod",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    APIKey = table.Column<string>(type: "text", nullable: true),
                    AdapterConsumerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdapterConsumerSubscriptionAuthMethod", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdapterConsumerSubscriptionAuthMethod_AdapterConsumer_Adapt~",
                        column: x => x.AdapterConsumerId,
                        principalTable: "AdapterConsumer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdapterConsumerSubscriptionAuthMethod_AdapterConsumerId",
                table: "AdapterConsumerSubscriptionAuthMethod",
                column: "AdapterConsumerId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdapterConsumerSubscriptionAuthMethod");
        }
    }
}
