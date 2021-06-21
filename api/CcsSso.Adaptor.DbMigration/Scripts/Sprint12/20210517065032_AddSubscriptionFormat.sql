START TRANSACTION;

ALTER TABLE "AdapterConsumer" DROP COLUMN "ConsumerKey";

ALTER TABLE "AdapterSubscription" ADD "AdapterFormatId" integer NOT NULL DEFAULT 0;

CREATE INDEX "IX_AdapterSubscription_AdapterFormatId" ON "AdapterSubscription" ("AdapterFormatId");

ALTER TABLE "AdapterSubscription" ADD CONSTRAINT "FK_AdapterSubscription_AdapterFormat_AdapterFormatId" FOREIGN KEY ("AdapterFormatId") REFERENCES "AdapterFormat" ("Id") ON DELETE CASCADE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210517065032_AddSubscriptionFormat', '5.0.2');

COMMIT;

