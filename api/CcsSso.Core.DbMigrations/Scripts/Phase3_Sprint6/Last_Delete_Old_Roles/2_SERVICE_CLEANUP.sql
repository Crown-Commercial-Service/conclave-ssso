CREATE OR REPLACE FUNCTION SERVICE_CLEANUP(
	) RETURNS integer AS $$

BEGIN

UPDATE "CcsServiceRoleGroup" SET "IsDeleted" = true, "LastUpdatedOnUtc" = timezone('utc', now()) 
WHERE "IsDeleted" != true AND "Id" NOT IN (SELECT DISTINCT "CcsServiceRoleGroupId" FROM "CcsServiceRoleMapping");

RETURN 1;
END;
$$ LANGUAGE plpgsql;

SELECT SERVICE_CLEANUP();
DROP FUNCTION SERVICE_CLEANUP;