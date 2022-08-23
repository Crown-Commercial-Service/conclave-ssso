START TRANSACTION;

ALTER TABLE "User" ADD "DelegationEndDate" timestamp without time zone NULL;

ALTER TABLE "User" ADD "DelegationStartDate" timestamp without time zone NULL;

ALTER TABLE "User" ADD "UserType" integer NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20220801092551_UserDelegation', '5.0.10');

COMMIT;

