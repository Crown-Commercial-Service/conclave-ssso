using Microsoft.EntityFrameworkCore.Migrations;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class ALTER_OrganisationAudit_ActionedOnUtc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedOnUtc",
                table: "OrganisationAudit",
                newName: "ActionedOnUtc");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ActionedOnUtc",
                table: "OrganisationAudit",
                newName: "CreatedOnUtc");
        }
    }
}
