START TRANSACTION;

ALTER TABLE "User" ADD "MfaEnabled" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "OrganisationUserGroup" ADD "MfaEnabled" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "OrganisationEligibleRole" ADD "MfaEnabled" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "CcsAccessRole" ADD "MfaEnabled" boolean NOT NULL DEFAULT FALSE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210628060230_AddMfaflag', '5.0.2');

COMMIT;

