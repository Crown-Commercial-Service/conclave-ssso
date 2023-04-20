Update "CcsServiceRoleGroup" 
SET "Name" = 'Report Management Information', "Key" = 'RMI_USER', "LastUpdatedOnUtc" = now()
WHERE "Key" = 'RMI' AND "IsDeleted" = false;

UPDATE "CcsService" 
SET "ServiceName" = 'Report Management Information', "LastUpdatedOnUtc" = now()
WHERE "ServiceCode" = 'RMI_USER_DS' AND "IsDeleted" = false;

Update "CcsServiceRoleGroup" 
SET "Description" = 'Use this service to obtain agreement templates, submit management information to CCS or report no business to CCS.', "LastUpdatedOnUtc" = now()
WHERE "Key" = 'RMI_USER' AND "IsDeleted" = false;

UPDATE "CcsService" 
SET "Description" = 'Use this service to obtain agreement templates, submit management information to CCS or report no business to CCS.', "LastUpdatedOnUtc" = now()
WHERE "ServiceCode" = 'RMI_USER_DS' AND "IsDeleted" = false;
