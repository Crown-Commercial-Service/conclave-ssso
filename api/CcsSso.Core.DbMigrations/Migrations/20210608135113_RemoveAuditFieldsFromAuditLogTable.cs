using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class RemoveAuditFieldsFromAuditLogTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConcurrencyKey",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "CreatedOnUtc",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "CreatedUserId",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "LastUpdatedUserId",
                table: "AuditLog");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedOnUtc",
                table: "AuditLog",
                newName: "EventTimeUtc");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EventTimeUtc",
                table: "AuditLog",
                newName: "LastUpdatedOnUtc");

            migrationBuilder.AddColumn<byte[]>(
                name: "ConcurrencyKey",
                table: "AuditLog",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOnUtc",
                table: "AuditLog",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedUserId",
                table: "AuditLog",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AuditLog",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LastUpdatedUserId",
                table: "AuditLog",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
