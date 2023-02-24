
START TRANSACTION;
UPDATE "CcsAccessRole" set "CcsAccessRoleNameKey"= 'CAT_USER' WHERE "CcsAccessRoleNameKey" = 'CAS_USER';
COMMIT;
