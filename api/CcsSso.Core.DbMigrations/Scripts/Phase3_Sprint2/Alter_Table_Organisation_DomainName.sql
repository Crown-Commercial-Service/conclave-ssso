START TRANSACTION;

ALTER TABLE "Organisation" ADD "DomainName" text NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20221220110945_Update_OneTime_Org_Domain', '5.0.10');

COMMIT;