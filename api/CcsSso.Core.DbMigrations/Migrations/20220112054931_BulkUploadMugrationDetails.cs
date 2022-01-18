using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class BulkUploadMugrationDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedUserCount",
                table: "BulkUploadDetail",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "MigrationEndedOnUtc",
                table: "BulkUploadDetail",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "MigrationStartedOnUtc",
                table: "BulkUploadDetail",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "MigrationStringContent",
                table: "BulkUploadDetail",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessedUserCount",
                table: "BulkUploadDetail",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalOrganisationCount",
                table: "BulkUploadDetail",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalUserCount",
                table: "BulkUploadDetail",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedUserCount",
                table: "BulkUploadDetail");

            migrationBuilder.DropColumn(
                name: "MigrationEndedOnUtc",
                table: "BulkUploadDetail");

            migrationBuilder.DropColumn(
                name: "MigrationStartedOnUtc",
                table: "BulkUploadDetail");

            migrationBuilder.DropColumn(
                name: "MigrationStringContent",
                table: "BulkUploadDetail");

            migrationBuilder.DropColumn(
                name: "ProcessedUserCount",
                table: "BulkUploadDetail");

            migrationBuilder.DropColumn(
                name: "TotalOrganisationCount",
                table: "BulkUploadDetail");

            migrationBuilder.DropColumn(
                name: "TotalUserCount",
                table: "BulkUploadDetail");
        }
    }
}
