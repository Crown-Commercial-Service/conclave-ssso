CREATE OR REPLACE FUNCTION create_el_service() RETURNS integer AS $$

-- Add values to clientId and clientUrl
DECLARE serviceName text = 'Data Migration';
DECLARE serviceDescription text = 'Migrate Users and Organisations';
DECLARE serviceCode text = 'DM_CLIENT';
DECLARE clientUrl text = '';
DECLARE clientId text = '';

DECLARE dmServiceId int;
DECLARE dashboardServiceId int;

DECLARE dmAdminPermissionId int;
DECLARE dbDMAccessPermissionId int;

DECLARE dmAdminRoleId int;
DECLARE dbAccessDMRoleId int;

BEGIN

DELETE FROM public."CcsService"
	WHERE "ServiceName" = serviceName;

INSERT INTO public."CcsService"(
	"ServiceName", "TimeOutLength", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc",
	"IsDeleted", "ServiceClientId", "ServiceUrl", "Description", "ServiceCode", "GlobalLevelOrganisationAccess", "ActivateOrganisations")
	VALUES (serviceName, 0, 0, 0, now(), now(), false, clientId, 
			clientUrl, serviceDescription, serviceCode, true, true);
			
			
SELECT "Id" into dmServiceId From public."CcsService" WHERE "ServiceName" = serviceName LIMIT 1;
SELECT "Id" into dashboardServiceId From public."CcsService" WHERE "ServiceName" = 'Dashboard Service' LIMIT 1;	

INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('DATA_MIGRATION_PERMISSION', dmServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into dmAdminPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'DATA_MIGRATION_PERMISSION' AND "CcsServiceId" = dmServiceId LIMIT 1;


INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('ACCESS_DM_CLIENT', dashboardServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into dbDMAccessPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'ACCESS_DM_CLIENT' LIMIT 1;



INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('DATA_MIGRATION', 'Data Migration', 'Data Migration', 0, 0, 2, 0, 0, now(), now(), 
			false, false);
SELECT "Id" into dmAdminRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'DATA_MIGRATION' LIMIT 1;
			
INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('ACCESS_DM_CLIENT', 'Access Data Migration', 'Access Data Migration', 0, 0, 2, 0, 0, now(), now(), 
			false, false);			
SELECT "Id" into dbAccessDMRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ACCESS_DM_CLIENT' LIMIT 1;


INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (dmAdminPermissionId, dmAdminRoleId, 0, 0, now(), now(), false);

INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (dbDMAccessPermissionId, dbAccessDMRoleId, 0, 0, now(), now(), false);

	RETURN 1;
	END;
$$ LANGUAGE plpgsql;

SELECT setval('"CcsAccessRole_Id_seq"', max("Id")) FROM "CcsAccessRole";
SELECT setval('"CcsService_Id_seq"', max("Id")) FROM "CcsService";
SELECT setval('"ServicePermission_Id_seq"', max("Id")) FROM "ServicePermission";
SELECT setval('"ServiceRolePermission_Id_seq"', max("Id")) FROM "ServiceRolePermission";
SELECT create_el_service();
DROP FUNCTION create_el_service;
