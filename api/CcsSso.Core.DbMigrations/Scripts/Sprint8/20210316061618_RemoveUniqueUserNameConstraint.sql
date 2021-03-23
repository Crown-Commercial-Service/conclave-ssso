START TRANSACTION;

DROP INDEX IF EXISTS "IX_User_UserName";

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210316061618_RemoveUniqueUserNameConstraint', '5.0.2');

COMMIT;

