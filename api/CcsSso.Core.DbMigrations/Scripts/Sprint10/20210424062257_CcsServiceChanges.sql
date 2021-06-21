START TRANSACTION;

ALTER TABLE "CcsService" ADD "ServiceClientId" text NULL;

ALTER TABLE "CcsService" ADD "ServiceUrl" text NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210424062257_AddNewColumbnsToCcsServiceEntity', '5.0.2');

COMMIT;

