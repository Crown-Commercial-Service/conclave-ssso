using Microsoft.EntityFrameworkCore.Migrations;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class DropOrganisationEligibleIdpIdOnUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_OrganisationEligibleIdentityProvider_OrganisationEligi~",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_User_OrganisationEligibleIdentityProviderId",
                table: "User");

            migrationBuilder.DropColumn(
                name: "OrganisationEligibleIdentityProviderId",
                table: "User");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrganisationEligibleIdentityProviderId",
                table: "User",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_User_OrganisationEligibleIdentityProviderId",
                table: "User",
                column: "OrganisationEligibleIdentityProviderId");

            migrationBuilder.AddForeignKey(
                name: "FK_User_OrganisationEligibleIdentityProvider_OrganisationEligi~",
                table: "User",
                column: "OrganisationEligibleIdentityProviderId",
                principalTable: "OrganisationEligibleIdentityProvider",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
