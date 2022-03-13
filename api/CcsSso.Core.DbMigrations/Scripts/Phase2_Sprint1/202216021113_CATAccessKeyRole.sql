START TRANSACTION;

UPDATE public."CcsService" 
SET "ServiceCode"='CAAAC_CLIENT',
"LastUpdatedOnUtc"=Now()
WHERE "ServiceName"='Create and award a contract';

UPDATE public."CcsAccessRole" 
SET "CcsAccessRoleNameKey" ='ACCESS_CAAAC_CLIENT',
"CcsAccessRoleName"='Access CAAAC',
"CcsAccessRoleDescription"='Access CAAAC',
"LastUpdatedOnUtc"=Now()
WHERE "CcsAccessRoleNameKey"='ACCESS_CAT_CLIENT';

UPDATE public."ServicePermission" 
SET "ServicePermissionName" ='ACCESS_CAAAC_CLIENT',
"LastUpdatedOnUtc"=Now()
WHERE "ServicePermissionName"='ACCESS_CAT_CLIENT';

COMMIT;

