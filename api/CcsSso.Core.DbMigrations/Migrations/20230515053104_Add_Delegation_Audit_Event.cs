using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class Add_Delegation_Audit_Event : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DelegationLinkExpiryOnUtc",
                table: "User",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DelegationAuditEvent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PreviousDelegationStartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PreviousDelegationEndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NewDelegationStartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NewDelegationEndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Roles = table.Column<string>(type: "text", nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: true),
                    ActionedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ActionedBy = table.Column<string>(type: "text", nullable: true),
                    ActionedByUserName = table.Column<string>(type: "text", nullable: true),
                    ActionedByFirstName = table.Column<string>(type: "text", nullable: true),
                    ActionedByLastName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DelegationAuditEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DelegationAuditEvent_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DelegationAuditEvent_UserId",
                table: "DelegationAuditEvent",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DelegationAuditEvent");

            migrationBuilder.DropColumn(
                name: "DelegationLinkExpiryOnUtc",
                table: "User");
        }
    }
}
