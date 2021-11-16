START TRANSACTION;

ALTER TABLE "User" ADD "AccountVerified" boolean NOT NULL DEFAULT FALSE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20211111112648_AccountVerifiedColumnToUser', '5.0.10');

COMMIT;

