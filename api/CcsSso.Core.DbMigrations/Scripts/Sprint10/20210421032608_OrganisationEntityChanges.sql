START TRANSACTION;

ALTER TABLE "Organisation" ALTER COLUMN "RightToBuy" DROP NOT NULL;

ALTER TABLE "Organisation" ADD "BusinessType" text NULL;

ALTER TABLE "Organisation" ADD "SupplierBuyerType" integer NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210421032608_OrganisationEntityChanges', '5.0.2');

COMMIT;

