START TRANSACTION;

ALTER TABLE "User" ADD "DelegationAccepted" boolean NOT NULL DEFAULT FALSE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20220808132444_AddDelegationAcceptedFieldToUser', '5.0.10');

COMMIT;

