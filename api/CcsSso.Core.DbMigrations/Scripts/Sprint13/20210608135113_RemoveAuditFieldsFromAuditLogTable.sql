START TRANSACTION;

ALTER TABLE "AuditLog" DROP COLUMN "ConcurrencyKey";

ALTER TABLE "AuditLog" DROP COLUMN "CreatedOnUtc";

ALTER TABLE "AuditLog" DROP COLUMN "CreatedUserId";

ALTER TABLE "AuditLog" DROP COLUMN "IsDeleted";

ALTER TABLE "AuditLog" DROP COLUMN "LastUpdatedUserId";

ALTER TABLE "AuditLog" RENAME COLUMN "LastUpdatedOnUtc" TO "EventTimeUtc";

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210608135113_RemoveAuditFieldsFromAuditLogTable', '5.0.2');

COMMIT;

