START TRANSACTION;

ALTER TABLE "BulkUploadDetail" ADD "FailedUserCount" integer NOT NULL DEFAULT 0;

ALTER TABLE "BulkUploadDetail" ADD "MigrationEndedOnUtc" timestamp without time zone NOT NULL DEFAULT TIMESTAMP '0001-01-01 00:00:00';

ALTER TABLE "BulkUploadDetail" ADD "MigrationStartedOnUtc" timestamp without time zone NOT NULL DEFAULT TIMESTAMP '0001-01-01 00:00:00';

ALTER TABLE "BulkUploadDetail" ADD "MigrationStringContent" text NULL;

ALTER TABLE "BulkUploadDetail" ADD "ProcessedUserCount" integer NOT NULL DEFAULT 0;

ALTER TABLE "BulkUploadDetail" ADD "TotalOrganisationCount" integer NOT NULL DEFAULT 0;

ALTER TABLE "BulkUploadDetail" ADD "TotalUserCount" integer NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20220112054931_BulkUploadMugrationDetails', '5.0.10');

COMMIT;

