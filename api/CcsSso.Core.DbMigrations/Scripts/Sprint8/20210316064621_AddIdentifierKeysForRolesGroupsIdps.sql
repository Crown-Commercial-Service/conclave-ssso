START TRANSACTION;

ALTER TABLE "OrganisationUserGroup" ADD "UserGroupNameKey" text NULL;

ALTER TABLE "IdentityProvider" ADD "IdpConnectionName" text NULL;

ALTER TABLE "CcsAccessRole" ADD "CcsAccessRoleNameKey" text NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210316064621_AddIdentifierKeysForRolesGroupsIdps', '5.0.2');

COMMIT;

