START TRANSACTION;

UPDATE public."CcsAccessRole" 
SET "CcsAccessRoleName"='Jaggaer Supplier',
"CcsAccessRoleDescription"='Jaggaer Supplier',
"LastUpdatedOnUtc"=Now()
where "CcsAccessRoleNameKey"='JAEGGER_SUPPLIER'

COMMIT;

