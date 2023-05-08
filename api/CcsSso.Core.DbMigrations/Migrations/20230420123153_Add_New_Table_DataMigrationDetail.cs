using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class Add_New_Table_DataMigrationDetail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataMigrationDetail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileKey = table.Column<string>(type: "text", nullable: true),
                    FileKeyId = table.Column<string>(type: "text", nullable: true),
                    DocUploadId = table.Column<string>(type: "text", nullable: true),
                    DataMigrationStatus = table.Column<int>(type: "integer", nullable: false),
                    ValidationErrorDetails = table.Column<string>(type: "text", nullable: true),
                    MigrationStartedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    MigrationEndedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    MigrationStringContent = table.Column<string>(type: "text", nullable: true),
                    CreatedUserId = table.Column<int>(type: "integer", nullable: false),
                    LastUpdatedUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ConcurrencyKey = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataMigrationDetail", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataMigrationDetail");
        }
    }
}
