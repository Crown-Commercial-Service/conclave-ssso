-- Drop tables in the database

START TRANSACTION;

DROP TABLE IF EXISTS "AuditLog";

DROP TABLE IF EXISTS "BulkUploadDetail";

DROP TABLE IF EXISTS "CcsServiceLogin";

DROP TABLE IF EXISTS "IdamUserLoginRole";

DROP TABLE IF EXISTS "OrganisationEnterpriseType";

DROP TABLE IF EXISTS "OrganisationGroupEligibleRole";

DROP TABLE IF EXISTS "Person";

DROP TABLE IF EXISTS "PhysicalAddress";

DROP TABLE IF EXISTS "ProcurementGroup";

DROP TABLE IF EXISTS "ServiceRolePermission";

DROP TABLE IF EXISTS "SiteContact";

DROP TABLE IF EXISTS "TradingOrganisation";

DROP TABLE IF EXISTS "UserAccessRole";

DROP TABLE IF EXISTS "UserGroupMembership";

DROP TABLE IF EXISTS "UserSetting";

DROP TABLE IF EXISTS "VirtualAddress";

DROP TABLE IF EXISTS "IdamUserLogin";

DROP TABLE IF EXISTS "EnterpriseType";

DROP TABLE IF EXISTS "ServicePermission";

DROP TABLE IF EXISTS "ContactPoint";

DROP TABLE IF EXISTS "OrganisationAccessRole";

DROP TABLE IF EXISTS "ExternalServiceRoleMapping";

DROP TABLE IF EXISTS "OrganisationEligibleRole";

DROP TABLE IF EXISTS "OrganisationUserGroup";

DROP TABLE IF EXISTS "UserSettingType";

DROP TABLE IF EXISTS "VirtualAddressType";

DROP TABLE IF EXISTS "UserIdentityProvider";

DROP TABLE IF EXISTS "User";

DROP TABLE IF EXISTS "OrganisationEligibleIdentityProvider";

DROP TABLE IF EXISTS "Organisation";

DROP TABLE IF EXISTS "CcsService";

DROP TABLE IF EXISTS "ContactDetail";

DROP TABLE IF EXISTS "ContactPointReason";

DROP TABLE IF EXISTS "CcsAccessRole";

DROP TABLE IF EXISTS "IdentityProvider";

DROP TABLE IF EXISTS "Party";

DROP TABLE IF EXISTS "PartyType";

DROP TABLE IF EXISTS "__EFMigrationsHistory";

COMMIT;
