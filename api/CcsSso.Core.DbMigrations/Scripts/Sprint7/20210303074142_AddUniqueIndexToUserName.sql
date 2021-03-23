START TRANSACTION;

CREATE UNIQUE INDEX "IX_User_UserName" ON "User" ("UserName");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210303074142_AddUniqueIndexToUserName', '5.0.2');

COMMIT;

