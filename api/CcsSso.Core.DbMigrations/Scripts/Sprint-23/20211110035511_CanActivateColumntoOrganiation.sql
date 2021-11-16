START TRANSACTION;

ALTER TABLE "CcsService" ADD "ActivateOrganisations" boolean NOT NULL DEFAULT FALSE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20211110035511_CanActivateColumntoOrganiation', '5.0.10');

COMMIT;

