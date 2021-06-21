START TRANSACTION;

CREATE UNIQUE INDEX "IX_AdapterConsumerEntity_Name_AdapterConsumerId" ON "AdapterConsumerEntity" ("Name", "AdapterConsumerId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210614063127_AddUniqueConstrainToAdapterConsumerEntity', '5.0.2');

COMMIT;

