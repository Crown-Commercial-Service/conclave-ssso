using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace CcsSso.Adaptor.DbMigration.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdapterConsumer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    ClientId = table.Column<string>(type: "text", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdapterConsumer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdapterFormat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FomatFileType = table.Column<string>(type: "text", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdapterFormat", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConclaveEntity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConclaveEntity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdapterConsumerEntity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    AdapterConsumerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdapterConsumerEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdapterConsumerEntity_AdapterConsumer_AdapterConsumerId",
                        column: x => x.AdapterConsumerId,
                        principalTable: "AdapterConsumer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdapterSubscription",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubscriptionType = table.Column<string>(type: "text", nullable: true),
                    SubscriptionUrl = table.Column<string>(type: "text", nullable: true),
                    AdapterConsumerId = table.Column<int>(type: "integer", nullable: false),
                    ConclaveEntityId = table.Column<int>(type: "integer", nullable: false),
                    AdapterFormatId = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdapterSubscription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdapterSubscription_AdapterConsumer_AdapterConsumerId",
                        column: x => x.AdapterConsumerId,
                        principalTable: "AdapterConsumer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdapterSubscription_AdapterFormat_AdapterFormatId",
                        column: x => x.AdapterFormatId,
                        principalTable: "AdapterFormat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdapterSubscription_ConclaveEntity_ConclaveEntityId",
                        column: x => x.ConclaveEntityId,
                        principalTable: "ConclaveEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConclaveEntityAttribute",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttributeName = table.Column<string>(type: "text", nullable: true),
                    ConclaveEntityId = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConclaveEntityAttribute", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConclaveEntityAttribute_ConclaveEntity_ConclaveEntityId",
                        column: x => x.ConclaveEntityId,
                        principalTable: "ConclaveEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdapterConsumerEntityAttribute",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttributeName = table.Column<string>(type: "text", nullable: true),
                    AdapterConsumerEntityId = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdapterConsumerEntityAttribute", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdapterConsumerEntityAttribute_AdapterConsumerEntity_Adapte~",
                        column: x => x.AdapterConsumerEntityId,
                        principalTable: "AdapterConsumerEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdapterConclaveAttributeMapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdapterConsumerEntityAttributeId = table.Column<int>(type: "integer", nullable: false),
                    ConclaveEntityAttributeId = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdapterConclaveAttributeMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdapterConclaveAttributeMapping_AdapterConsumerEntityAttrib~",
                        column: x => x.AdapterConsumerEntityAttributeId,
                        principalTable: "AdapterConsumerEntityAttribute",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdapterConclaveAttributeMapping_ConclaveEntityAttribute_Con~",
                        column: x => x.ConclaveEntityAttributeId,
                        principalTable: "ConclaveEntityAttribute",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdapterConclaveAttributeMapping_AdapterConsumerEntityAttrib~",
                table: "AdapterConclaveAttributeMapping",
                column: "AdapterConsumerEntityAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_AdapterConclaveAttributeMapping_ConclaveEntityAttributeId",
                table: "AdapterConclaveAttributeMapping",
                column: "ConclaveEntityAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_AdapterConsumer_ClientId",
                table: "AdapterConsumer",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdapterConsumerEntity_AdapterConsumerId",
                table: "AdapterConsumerEntity",
                column: "AdapterConsumerId");

            migrationBuilder.CreateIndex(
                name: "IX_AdapterConsumerEntity_Name_AdapterConsumerId",
                table: "AdapterConsumerEntity",
                columns: new[] { "Name", "AdapterConsumerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdapterConsumerEntityAttribute_AdapterConsumerEntityId",
                table: "AdapterConsumerEntityAttribute",
                column: "AdapterConsumerEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AdapterSubscription_AdapterConsumerId",
                table: "AdapterSubscription",
                column: "AdapterConsumerId");

            migrationBuilder.CreateIndex(
                name: "IX_AdapterSubscription_AdapterFormatId",
                table: "AdapterSubscription",
                column: "AdapterFormatId");

            migrationBuilder.CreateIndex(
                name: "IX_AdapterSubscription_ConclaveEntityId",
                table: "AdapterSubscription",
                column: "ConclaveEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ConclaveEntityAttribute_ConclaveEntityId",
                table: "ConclaveEntityAttribute",
                column: "ConclaveEntityId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdapterConclaveAttributeMapping");

            migrationBuilder.DropTable(
                name: "AdapterSubscription");

            migrationBuilder.DropTable(
                name: "AdapterConsumerEntityAttribute");

            migrationBuilder.DropTable(
                name: "ConclaveEntityAttribute");

            migrationBuilder.DropTable(
                name: "AdapterFormat");

            migrationBuilder.DropTable(
                name: "AdapterConsumerEntity");

            migrationBuilder.DropTable(
                name: "ConclaveEntity");

            migrationBuilder.DropTable(
                name: "AdapterConsumer");
        }
    }
}
