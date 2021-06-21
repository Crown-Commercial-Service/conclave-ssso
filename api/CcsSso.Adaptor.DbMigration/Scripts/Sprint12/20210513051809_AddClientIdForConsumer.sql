START TRANSACTION;

ALTER TABLE "AdapterConsumer" ADD "ClientId" text NULL;

CREATE UNIQUE INDEX "IX_AdapterConsumer_ClientId" ON "AdapterConsumer" ("ClientId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210513051809_AddClientIdForConsumer', '5.0.2');

COMMIT;

