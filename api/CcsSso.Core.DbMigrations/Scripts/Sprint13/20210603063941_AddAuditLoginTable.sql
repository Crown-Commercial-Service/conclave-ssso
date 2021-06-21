START TRANSACTION;

ALTER TABLE "VirtualAddressType" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "VirtualAddressType" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "VirtualAddress" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "VirtualAddress" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "UserSettingType" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "UserSettingType" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "UserSetting" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "UserSetting" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "UserGroupMembership" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "UserGroupMembership" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "UserAccessRole" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "UserAccessRole" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "User" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "User" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "TradingOrganisation" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "TradingOrganisation" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "SiteContact" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "SiteContact" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "ServiceRolePermission" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "ServiceRolePermission" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "ServicePermission" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "ServicePermission" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "ProcurementGroup" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "ProcurementGroup" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "PhysicalAddress" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "PhysicalAddress" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "Person" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "Person" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "PartyType" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "PartyType" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "Party" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "Party" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "OrganisationUserGroup" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "OrganisationUserGroup" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "OrganisationGroupEligibleRole" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "OrganisationGroupEligibleRole" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "OrganisationEnterpriseType" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "OrganisationEnterpriseType" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "OrganisationEligibleRole" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "OrganisationEligibleRole" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "OrganisationEligibleIdentityProvider" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "OrganisationEligibleIdentityProvider" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "OrganisationAccessRole" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "OrganisationAccessRole" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "Organisation" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "Organisation" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "IdentityProvider" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "IdentityProvider" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "IdamUserLoginRole" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "IdamUserLoginRole" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "IdamUserLogin" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "IdamUserLogin" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "EnterpriseType" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "EnterpriseType" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "ContactPointReason" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "ContactPointReason" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "ContactPoint" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "ContactPoint" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "ContactDetail" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "ContactDetail" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "CcsServiceLogin" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "CcsServiceLogin" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "CcsService" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "CcsService" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

ALTER TABLE "CcsAccessRole" RENAME COLUMN "LastUpdatedPartyId" TO "LastUpdatedUserId";

ALTER TABLE "CcsAccessRole" RENAME COLUMN "CreatedPartyId" TO "CreatedUserId";

CREATE TABLE "AuditLog" (
    "Id" integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
    "Event" text NULL,
    "UserId" integer NOT NULL,
    "Application" text NULL,
    "ReferenceData" text NULL,
    "IpAddress" text NULL,
    "Device" text NULL,
    "CreatedUserId" integer NOT NULL,
    "LastUpdatedUserId" integer NOT NULL,
    "CreatedOnUtc" timestamp without time zone NOT NULL,
    "LastUpdatedOnUtc" timestamp without time zone NOT NULL,
    "IsDeleted" boolean NOT NULL,
    "ConcurrencyKey" bytea NULL,
    CONSTRAINT "PK_AuditLog" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210603063941_AddAuditLoginTable', '5.0.2');

COMMIT;

