START TRANSACTION;

UPDATE public."CcsAccessRole"
	SET "DefaultEligibility" = '000'
	WHERE "CcsAccessRoleNameKey"='FP_USER';

UPDATE public."CcsAccessRole"
	SET "DefaultEligibility" = '000'
	WHERE "CcsAccessRoleNameKey"='ACCESS_FP_CLIENT';

COMMIT;