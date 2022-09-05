START TRANSACTION;

ALTER TABLE "User" ADD "OriginOrganizationId" integer NULL;

CREATE INDEX "IX_User_OriginOrganizationId" ON "User" ("OriginOrganizationId");

ALTER TABLE "User" ADD CONSTRAINT "FK_User_Organisation_OriginOrganizationId" FOREIGN KEY ("OriginOrganizationId") REFERENCES "Organisation" ("Id") ON DELETE RESTRICT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20220826070759_Add_User_OriginOrganizationId', '5.0.10');

COMMIT;

