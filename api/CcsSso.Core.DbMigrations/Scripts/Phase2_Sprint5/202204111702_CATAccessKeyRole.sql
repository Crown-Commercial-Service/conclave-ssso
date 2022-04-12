START TRANSACTION;

UPDATE public."CcsAccessRole" 
SET "CcsAccessRoleName"='Access Contract Award Service',
"CcsAccessRoleDescription"='Access Contract Award Service',
"LastUpdatedOnUtc"=Now()
WHERE "CcsAccessRoleNameKey"='ACCESS_CAAAC_CLIENT';

UPDATE public."CcsAccessRole" 
SET "CcsAccessRoleName"='CAS User',
"CcsAccessRoleDescription"='CAS User',
"LastUpdatedOnUtc"=Now()
WHERE "CcsAccessRoleNameKey"='CAT_USER';

UPDATE public."CcsAccessRole" 
SET "CcsAccessRoleName"='CAS User',
"CcsAccessRoleDescription"='CAS User',
"LastUpdatedOnUtc"=Now()
WHERE "CcsAccessRoleNameKey"='CAT_USER_LOGIN_DIRECTOR';

COMMIT;