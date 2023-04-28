START TRANSACTION;

ALTER TABLE "DataMigrationDetail" ADD "FileName" text NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230424091530_Add_FileName_DataMigrationDetail', '5.0.10');

COMMIT;

