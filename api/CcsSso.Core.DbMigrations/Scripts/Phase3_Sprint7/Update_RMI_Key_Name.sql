Update "CcsServiceRoleGroup" 
SET "Name" = 'Report Management Information', "Key" = 'RMI_USER' 
WHERE "Key" = 'RMI' AND "IsDeleted" = false;

UPDATE "CcsService" 
SET "ServiceName" = 'Report Management Information' 
WHERE "ServiceCode" = 'RMI_USER_DS' AND "IsDeleted" = false;