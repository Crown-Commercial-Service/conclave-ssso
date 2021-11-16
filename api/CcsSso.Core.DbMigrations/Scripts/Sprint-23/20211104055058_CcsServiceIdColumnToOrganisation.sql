START TRANSACTION;

ALTER TABLE "Organisation" ADD "CcsServiceId" integer NULL;

CREATE INDEX "IX_Organisation_CcsServiceId" ON "Organisation" ("CcsServiceId");

ALTER TABLE "Organisation" ADD CONSTRAINT "FK_Organisation_CcsService_CcsServiceId" FOREIGN KEY ("CcsServiceId") REFERENCES "CcsService" ("Id") ON DELETE RESTRICT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20211104055058_CcsServiceIdColumnToOrganisation', '5.0.10');

COMMIT;

