CREATE OR REPLACE FUNCTION create_cat_service() RETURNS integer AS $$

-- Add values to clientId and clientUrl
DECLARE serviceName text = 'Contract Award Service';
DECLARE serviceDescription text = 'Find and contact suitable suppliers for your procurement project, and ask them about the services they can provide. Progress to one stage further competition.';
DECLARE serviceCode text = 'CAAAC_CLIENT';
DECLARE clientUrl text = 'https://test.cat.co.uk';
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
	VALUES ('CAT_ADMINISTRATOR', clientServiceId, 0, 0, now(), now(), false);
SELECT "Id" into clientAdminPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'CAT_ADMINISTRATOR' AND "CcsServiceId" = clientServiceId  LIMIT 1;				

INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('CAT_USER', clientServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into clientUserPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'CAT_USER' AND "CcsServiceId" = clientServiceId LIMIT 1;


INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('ACCESS_CAAAC_CLIENT', dashboardServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into dbAccesClientPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'ACCESS_CAAAC_CLIENT' LIMIT 1;	
	

			
INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('CAT_ADMINISTRATOR', 'CaT Admin', 'User for CaT Admin ', 0, 1, 2, 0, 0, now(), now(), 
			false, true);
SELECT "Id" into clientAdminRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'CAT_ADMINISTRATOR' AND "CcsAccessRoleName" = 'CaT Admin' LIMIT 1;
						
INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('CAT_USER', 'Cat User', 'Cat User', 2, 1, 1, 0, 0, now(), now(), 
			false, false);
SELECT "Id" into clientUserRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'CAT_USER' AND "CcsAccessRoleName" = 'Cat User' LIMIT 1;		

			
INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('ACCESS_CAAAC_CLIENT', 'Access Contract Award Service', 'Access Contract Award Service', 2, 1, 1, 0, 0, now(), now(), 
			false, false);			
SELECT "Id" into dbAccessClientRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ACCESS_CAAAC_CLIENT' LIMIT 1;




INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (clientAdminPermissionId, clientAdminRoleId, 0, 0, now(), now(), false);
	
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
SELECT create_cat_service();
DROP FUNCTION create_cat_service;
