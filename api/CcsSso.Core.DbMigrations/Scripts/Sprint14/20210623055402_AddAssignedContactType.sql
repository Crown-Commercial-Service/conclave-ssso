START TRANSACTION;

ALTER TABLE "SiteContact" ADD "AssignedContactType" integer NOT NULL DEFAULT 0;

ALTER TABLE "ContactPoint" ADD "AssignedContactType" integer NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210623055402_AddAssignedContactType', '5.0.2');

COMMIT;

