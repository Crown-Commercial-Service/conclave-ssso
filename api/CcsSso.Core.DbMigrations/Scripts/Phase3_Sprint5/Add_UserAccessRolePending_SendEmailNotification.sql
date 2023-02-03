START TRANSACTION;

ALTER TABLE "UserAccessRolePending" ADD "SendEmailNotification" boolean NOT NULL DEFAULT TRUE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230203101249_Add_UserAccessRolePending_SendEmailNotification', '5.0.10');

COMMIT;

