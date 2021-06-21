START TRANSACTION;

ALTER TABLE "CcsService" ADD "Description" text NULL;

ALTER TABLE "CcsService" ADD "ServiceCode" text NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210520065215_CcsServiceIdentificationColumns', '5.0.2');

COMMIT;

