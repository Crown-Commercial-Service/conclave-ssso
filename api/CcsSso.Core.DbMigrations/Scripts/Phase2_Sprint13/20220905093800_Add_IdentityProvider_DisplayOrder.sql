START TRANSACTION;

ALTER TABLE "IdentityProvider" ADD "DisplayOrder" integer NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20220905093800_Add_IdentityProvider_DisplayOrder', '5.0.10');

COMMIT;

START TRANSACTION;

UPDATE public."IdentityProvider"
	SET "DisplayOrder" = 0
	WHERE "IdpName" ='None';

UPDATE public."IdentityProvider"
	SET "DisplayOrder" = 1
	WHERE "IdpName" ='User ID and password';

UPDATE public."IdentityProvider"
	SET "DisplayOrder" = 2
	WHERE "IdpName" ='Google';

UPDATE public."IdentityProvider"
	SET "DisplayOrder" = 3
	WHERE "IdpName" ='Microsoft 365';

UPDATE public."IdentityProvider"
	SET "DisplayOrder" = 4
	WHERE "IdpName" ='LinkedIn';

UPDATE public."IdentityProvider"
	SET "DisplayOrder" = 5
	WHERE "IdpName" ='Facebook';

COMMIT;