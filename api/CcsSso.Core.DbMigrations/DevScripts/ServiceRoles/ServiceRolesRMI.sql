CREATE OR REPLACE FUNCTION create_rmi_service() RETURNS integer AS $$

-- Add values to clientId and clientUrl
DECLARE serviceName text = 'RMI';
DECLARE serviceDescription text = 'RMI';
DECLARE serviceCode text = 'RMI_CLIENT';
DECLARE clientUrl text = '';
DECLARE clientId text = '';

DECLARE clientServiceId int;
DECLARE dashboardServiceId int;

DECLARE clientAdminPermissionId int;
DECLARE clientCOPermissionId int;
DECLARE clientMIPermissionId int;
DECLARE clientUserPermissionId int;

DECLARE dbAccesClientPermissionId int;

DECLARE clientAdminRoleId int;
DECLARE clientCORoleId int;
DECLARE clientMIRoleId int;
DECLARE clientUserRoleId int;
DECLARE dbAccessClientRoleId int;

BEGIN

INSERT INTO public."CcsService"(
	"ServiceName", "TimeOutLength", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc",
	"IsDeleted", "ServiceClientId", "ServiceUrl", "Description", "ServiceCode", "GlobalLevelOrganisationAccess", "ActivateOrganisations")
	VALUES (serviceName, 0, 0, 0, now(), now(), false, clientId, 
			clientUrl, serviceDescription, serviceCode, false, false);
			
			
SELECT "Id" into clientServiceId From public."CcsService" WHERE "ServiceName" = serviceName LIMIT 1;
SELECT "Id" into dashboardServiceId From public."CcsService" WHERE "ServiceName" = 'Dashboard Service' LIMIT 1;	
			

INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('RMI_USER', clientServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into clientUserPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'RMI_USER' AND "CcsServiceId" = clientServiceId LIMIT 1;


INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('ACCESS_RMI_CLIENT', dashboardServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into dbAccesClientPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'ACCESS_RMI_CLIENT' LIMIT 1;	
	


						
INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('RMI_USER', 'RMI User', 'RMI User', 2, 1, 2, 0, 0, now(), now(), 
			false, false);
SELECT "Id" into clientUserRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'RMI_USER' AND "CcsAccessRoleName" = 'RMI User' LIMIT 1;		

			
INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('ACCESS_RMI_CLIENT', 'Access RMI', 'Access RMI', 2, 1, 2, 0, 0, now(), now(), 
			false, false);			
SELECT "Id" into dbAccessClientRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ACCESS_RMI_CLIENT' LIMIT 1;




INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (clientUserPermissionId, clientUserRoleId, 0, 0, now(), now(), false);


INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (dbAccesClientPermissionId, dbAccessClientRoleId, 0, 0, now(), now(), false);

	RETURN 1;
	END;
$$ LANGUAGE plpgsql;

SELECT setval('"CcsAccessRole_Id_seq"', max("Id")) FROM "CcsAccessRole";
SELECT setval('"CcsService_Id_seq"', max("Id")) FROM "CcsService";
SELECT setval('"ServicePermission_Id_seq"', max("Id")) FROM "ServicePermission";
SELECT setval('"ServiceRolePermission_Id_seq"', max("Id")) FROM "ServiceRolePermission";
SELECT create_rmi_service();
DROP FUNCTION create_rmi_service;
