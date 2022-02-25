START TRANSACTION;

UPDATE public."CcsAccessRole"
	SET "CcsAccessRoleName"= 'Buyer Supplier Information', "CcsAccessRoleDescription"='Access Evidence Locker (Buyer/Supplier Information)'
	WHERE "CcsAccessRoleNameKey"='ACCESS_EVIDENCE_LOCKER';

COMMIT;

