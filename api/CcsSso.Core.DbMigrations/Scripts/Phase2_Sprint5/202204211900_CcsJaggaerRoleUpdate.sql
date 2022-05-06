START TRANSACTION;

UPDATE public."CcsAccessRole"
SET "CcsAccessRoleName"='Jaggaer Supplier',
"CcsAccessRoleDescription"='Jaggaer Supplier',
"LastUpdatedOnUtc"=Now()
WHERE "CcsAccessRoleNameKey"='JAEGGER_SUPPLIER';

COMMIT;