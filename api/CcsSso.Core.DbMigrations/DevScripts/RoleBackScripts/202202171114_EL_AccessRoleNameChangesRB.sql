START TRANSACTION;

UPDATE public."CcsAccessRole"
	SET "CcsAccessRoleName"= 'Access Evidence Locker', "CcsAccessRoleDescription"='Access Evidence Locker'
	WHERE "CcsAccessRoleNameKey"='ACCESS_EVIDENCE_LOCKER';

COMMIT;

