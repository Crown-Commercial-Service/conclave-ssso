START TRANSACTION;

ALTER TABLE "User" DROP CONSTRAINT "FK_User_OrganisationEligibleIdentityProvider_OrganisationEligi~";

DROP INDEX "IX_User_OrganisationEligibleIdentityProviderId";

ALTER TABLE "User" DROP COLUMN "OrganisationEligibleIdentityProviderId";

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20211109132208_DropOrganisationEligibleIdpIdOnUser', '5.0.10');

COMMIT;

