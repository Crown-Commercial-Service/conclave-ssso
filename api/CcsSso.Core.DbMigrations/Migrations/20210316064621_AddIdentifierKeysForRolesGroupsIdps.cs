using Microsoft.EntityFrameworkCore.Migrations;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class AddIdentifierKeysForRolesGroupsIdps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserGroupNameKey",
                table: "OrganisationUserGroup",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdpConnectionName",
                table: "IdentityProvider",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CcsAccessRoleNameKey",
                table: "CcsAccessRole",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserGroupNameKey",
                table: "OrganisationUserGroup");

            migrationBuilder.DropColumn(
                name: "IdpConnectionName",
                table: "IdentityProvider");

            migrationBuilder.DropColumn(
                name: "CcsAccessRoleNameKey",
                table: "CcsAccessRole");
        }
    }
}
