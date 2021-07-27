START TRANSACTION;

ALTER TABLE "SiteContact" ADD "OriginalContactId" integer NOT NULL DEFAULT 0;

ALTER TABLE "ContactPoint" ADD "OriginalContactPointId" integer NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210621134357_AddOriginalContactId', '5.0.2');

COMMIT;

