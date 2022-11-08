using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class Add_Table_AutoValidationRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutoValidationRole",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CcsAccessRoleId = table.Column<int>(type: "integer", nullable: false),
                    IsSupplier = table.Column<bool>(type: "boolean", nullable: false),
                    IsBuyerSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    IsBuyerFailed = table.Column<bool>(type: "boolean", nullable: false),
                    IsBothSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    IsBothFailed = table.Column<bool>(type: "boolean", nullable: false),
                    AssignToOrg = table.Column<bool>(type: "boolean", nullable: false),
                    AssignToAdmin = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoValidationRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutoValidationRole_CcsAccessRole_CcsAccessRoleId",
                        column: x => x.CcsAccessRoleId,
                        principalTable: "CcsAccessRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutoValidationRole_CcsAccessRoleId",
                table: "AutoValidationRole",
                column: "CcsAccessRoleId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoValidationRole");
        }
    }
}
