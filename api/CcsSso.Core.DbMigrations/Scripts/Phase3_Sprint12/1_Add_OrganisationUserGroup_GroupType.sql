START TRANSACTION;

ALTER TABLE "OrganisationUserGroup" ADD "GroupType" integer NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230523121502_Add_OrganisationUserGroup_GroupType', '5.0.10');

COMMIT;

