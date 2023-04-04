using Microsoft.EntityFrameworkCore.Migrations;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class Add_UserAccessRolePending_OrganisationUserGroupId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrganisationUserGroupId",
                table: "UserAccessRolePending",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessRolePending_OrganisationUserGroupId",
                table: "UserAccessRolePending",
                column: "OrganisationUserGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAccessRolePending_OrganisationUserGroup_OrganisationUse~",
                table: "UserAccessRolePending",
                column: "OrganisationUserGroupId",
                principalTable: "OrganisationUserGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAccessRolePending_OrganisationUserGroup_OrganisationUse~",
                table: "UserAccessRolePending");

            migrationBuilder.DropIndex(
                name: "IX_UserAccessRolePending_OrganisationUserGroupId",
                table: "UserAccessRolePending");

            migrationBuilder.DropColumn(
                name: "OrganisationUserGroupId",
                table: "UserAccessRolePending");
        }
    }
}
