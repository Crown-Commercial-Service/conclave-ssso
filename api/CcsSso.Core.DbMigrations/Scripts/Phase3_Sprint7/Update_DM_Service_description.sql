Update "CcsServiceRoleGroup" 
SET "Description" = 'Migrate Users and Organisations', "LastUpdatedOnUtc" = now()
WHERE "Key" = 'DATA_MIGRATION' AND "IsDeleted" = false;