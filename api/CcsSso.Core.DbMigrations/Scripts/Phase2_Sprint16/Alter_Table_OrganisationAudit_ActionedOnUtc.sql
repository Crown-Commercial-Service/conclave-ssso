START TRANSACTION;

ALTER TABLE "OrganisationAudit" RENAME COLUMN "CreatedOnUtc" TO "ActionedOnUtc";

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20221122054840_ALTER_OrganisationAudit_ActionedOnUtc', '5.0.10');

COMMIT;

