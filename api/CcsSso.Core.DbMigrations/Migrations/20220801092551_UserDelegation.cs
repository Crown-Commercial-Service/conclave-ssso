using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class UserDelegation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DelegationEndDate",
                table: "User",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DelegationStartDate",
                table: "User",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserType",
                table: "User",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DelegationEndDate",
                table: "User");

            migrationBuilder.DropColumn(
                name: "DelegationStartDate",
                table: "User");

            migrationBuilder.DropColumn(
                name: "UserType",
                table: "User");
        }
    }
}
