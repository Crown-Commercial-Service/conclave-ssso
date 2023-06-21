
CREATE OR REPLACE FUNCTION AddRole() RETURNS integer AS $$

DECLARE serviceName text = 'Dashboard Service';

DECLARE clientServiceId int;
DECLARE dashboardServiceId int;

declare ServicePermissionId int;
declare RoleId int;

begin

SELECT "Id" into clientServiceId From public."CcsService" WHERE "ServiceName" = serviceName LIMIT 1;

if (clientServiceId is null) then
	raise notice 'No service found';
	return 1;
end if; 

INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('RMI_USER_DS', clientServiceId, 0, 0, now(), now(), false);

SELECT "Id" into ServicePermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'RMI_USER_DS' AND "CcsServiceId" = clientServiceId  LIMIT 1;

INSERT INTO public."CcsAccessRole"(
 	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('RMI_USER', 'RMI Tile', 'RMI Tile', 2, 1, 2,0, 0, 0, now(), now(), 
			false, false);
SELECT "Id" into RoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'RMI_USER' AND "CcsAccessRoleName" = 'RMI Tile' LIMIT 1;

INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (ServicePermissionId, RoleId, 0, 0, now(), now(), false);
	

	RETURN 1;
	END;
$$ LANGUAGE plpgsql;

SELECT setval('"CcsAccessRole_Id_seq"', max("Id")) FROM "CcsAccessRole";
SELECT setval('"ServicePermission_Id_seq"', max("Id")) FROM "ServicePermission";
SELECT setval('"ServiceRolePermission_Id_seq"', max("Id")) FROM "ServiceRolePermission";
SELECT AddRole();
DROP FUNCTION AddRole;

