START TRANSACTION;

UPDATE public."CcsAccessRole" 
SET "CcsAccessRoleNameKey" ='ACCESS_CAAAC_CLIENT',
"CcsAccessRoleName"='Access Contract Award Service',
"LastUpdatedOnUtc"=Now()
WHERE "CcsAccessRoleNameKey"='ACCESS_CAT_CLIENT';

UPDATE public."CcsAccessRole" 
SET "CcsAccessRoleNameKey" ='CAT_USER',
"CcsAccessRoleName"='CAS User',
"CcsAccessRoleDescription"='CAS User',
"LastUpdatedOnUtc"=Now()
WHERE "CcsAccessRoleNameKey"='CAT_USER';

COMMIT;

