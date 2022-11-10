-- Add delegate permission

CREATE OR REPLACE FUNCTION add_delegate_permission() RETURNS integer AS $$

		
	DECLARE orgAdminAccessRoleId int;
	
	DECLARE dashboardServiceId int;

	DECLARE delegatedAccessPermissionId int;
		
	BEGIN

	SELECT "Id" into orgAdminAccessRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ORG_ADMINISTRATOR' LIMIT 1;
			
	SELECT "Id" into dashboardServiceId From public."CcsService" WHERE "ServiceName" = 'Dashboard Service' LIMIT 1;
		
	INSERT INTO public."ServicePermission"(
		"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
		VALUES ('DELEGATED_ACCESS', dashboardServiceId, 0, 0, now(), now(), false);

	SELECT "Id" into delegatedAccessPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'DELEGATED_ACCESS' LIMIT 1;
		
	INSERT INTO public."ServiceRolePermission"(
		"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
		VALUES (orgAdminAccessRoleId, delegatedAccessPermissionId, 0, 0, now(), now(), false);
	
	RETURN 1;
	END;

$$ LANGUAGE plpgsql;

SELECT setval('"ServicePermission_Id_seq"', max("Id")) FROM "ServicePermission";
SELECT setval('"ServiceRolePermission_Id_seq"', max("Id")) FROM "ServiceRolePermission";

SELECT add_delegate_permission();

DROP FUNCTION add_delegate_permission;
