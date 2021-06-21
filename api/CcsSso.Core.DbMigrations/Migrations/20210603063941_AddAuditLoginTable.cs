using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace CcsSso.Core.DbMigrations.Migrations
{
    public partial class AddAuditLoginTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "VirtualAddressType",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "VirtualAddressType",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "VirtualAddress",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "VirtualAddress",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "UserSettingType",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "UserSettingType",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "UserSetting",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "UserSetting",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "UserGroupMembership",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "UserGroupMembership",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "UserAccessRole",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "UserAccessRole",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "User",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "User",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "TradingOrganisation",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "TradingOrganisation",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "SiteContact",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "SiteContact",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "ServiceRolePermission",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "ServiceRolePermission",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "ServicePermission",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "ServicePermission",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "ProcurementGroup",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "ProcurementGroup",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "PhysicalAddress",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "PhysicalAddress",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "Person",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "Person",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "PartyType",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "PartyType",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "Party",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "Party",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "OrganisationUserGroup",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "OrganisationUserGroup",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "OrganisationGroupEligibleRole",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "OrganisationGroupEligibleRole",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "OrganisationEnterpriseType",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "OrganisationEnterpriseType",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "OrganisationEligibleRole",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "OrganisationEligibleRole",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "OrganisationEligibleIdentityProvider",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "OrganisationEligibleIdentityProvider",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "OrganisationAccessRole",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "OrganisationAccessRole",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "Organisation",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "Organisation",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "IdentityProvider",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "IdentityProvider",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "IdamUserLoginRole",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "IdamUserLoginRole",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "IdamUserLogin",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "IdamUserLogin",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "EnterpriseType",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "EnterpriseType",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "ContactPointReason",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "ContactPointReason",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "ContactPoint",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "ContactPoint",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "ContactDetail",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "ContactDetail",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "CcsServiceLogin",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "CcsServiceLogin",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "CcsService",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "CcsService",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedPartyId",
                table: "CcsAccessRole",
                newName: "LastUpdatedUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedPartyId",
                table: "CcsAccessRole",
                newName: "CreatedUserId");

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Event = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Application = table.Column<string>(type: "text", nullable: true),
                    ReferenceData = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    Device = table.Column<string>(type: "text", nullable: true),
                    CreatedUserId = table.Column<int>(type: "integer", nullable: false),
                    LastUpdatedUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdatedOnUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ConcurrencyKey = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "VirtualAddressType",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "VirtualAddressType",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "VirtualAddress",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "VirtualAddress",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "UserSettingType",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "UserSettingType",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "UserSetting",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "UserSetting",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "UserGroupMembership",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "UserGroupMembership",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "UserAccessRole",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "UserAccessRole",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "User",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "User",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "TradingOrganisation",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "TradingOrganisation",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "SiteContact",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "SiteContact",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "ServiceRolePermission",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "ServiceRolePermission",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "ServicePermission",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "ServicePermission",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "ProcurementGroup",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "ProcurementGroup",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "PhysicalAddress",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "PhysicalAddress",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "Person",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "Person",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "PartyType",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "PartyType",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "Party",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "Party",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "OrganisationUserGroup",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "OrganisationUserGroup",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "OrganisationGroupEligibleRole",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "OrganisationGroupEligibleRole",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "OrganisationEnterpriseType",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "OrganisationEnterpriseType",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "OrganisationEligibleRole",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "OrganisationEligibleRole",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "OrganisationEligibleIdentityProvider",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "OrganisationEligibleIdentityProvider",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "OrganisationAccessRole",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "OrganisationAccessRole",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "Organisation",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "Organisation",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "IdentityProvider",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "IdentityProvider",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "IdamUserLoginRole",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "IdamUserLoginRole",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "IdamUserLogin",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "IdamUserLogin",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "EnterpriseType",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "EnterpriseType",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "ContactPointReason",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "ContactPointReason",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "ContactPoint",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "ContactPoint",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "ContactDetail",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "ContactDetail",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "CcsServiceLogin",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "CcsServiceLogin",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "CcsService",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "CcsService",
                newName: "CreatedPartyId");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedUserId",
                table: "CcsAccessRole",
                newName: "LastUpdatedPartyId");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "CcsAccessRole",
                newName: "CreatedPartyId");
        }
    }
}
