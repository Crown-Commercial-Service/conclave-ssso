START TRANSACTION;

ALTER TABLE "CcsAccessRole" ADD "DefaultEligibility" text NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20220829054002_Add_CcsAccessRole_DefaultEligibility', '5.0.10');

COMMIT;

START TRANSACTION;

UPDATE public."CcsAccessRole"
	SET "DefaultEligibility"= '000';

COMMIT;

START TRANSACTION;

UPDATE public."CcsAccessRole"
	SET "DefaultEligibility" = '011'
	WHERE "CcsAccessRoleNameKey"='FP_USER';

UPDATE public."CcsAccessRole"
	SET "DefaultEligibility" = '011'
	WHERE "CcsAccessRoleNameKey"='ACCESS_FP_CLIENT';

COMMIT;

