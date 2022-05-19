CREATE OR REPLACE FUNCTION create_demo_service() RETURNS integer AS $$

-- Add values to clientId and clientUrl
DECLARE serviceName text = 'Test SAML Client';
DECLARE serviceDescription text = 'TEST SAML client';
DECLARE serviceCode text = 'TEST_SAML_CLIENT';
DECLARE clientUrl text = 'http://localhost:50575';
DECLARE clientId text = '';

DECLARE clientServiceId int;
DECLARE dashboardServiceId int;

DECLARE clientUserPermissionId int;
DECLARE dbAccesClientPermissionId int;

DECLARE clientUserRoleId int;
DECLARE dbAccessClientRoleId int;

BEGIN

INSERT INTO public."CcsService"(
	"ServiceName", "TimeOutLength", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc",
	"IsDeleted", "ServiceClientId", "ServiceUrl", "Description", "ServiceCode", "GlobalLevelOrganisationAccess", "ActivateOrganisations")
	VALUES (serviceName, 0, 0, 0, now(), now(), false, clientId, 
			clientUrl, serviceDescription, serviceCode, true, false);
			
			
SELECT "Id" into clientServiceId From public."CcsService" WHERE "ServiceName" = serviceName LIMIT 1;
SELECT "Id" into dashboardServiceId From public."CcsService" WHERE "ServiceName" = 'Dashboard Service' LIMIT 1;	
			

INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('TEST_SAML_CLIENT_USER', clientServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into clientUserPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'TEST_SAML_CLIENT_USER' AND "CcsServiceId" = clientServiceId LIMIT 1;


INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('ACCESS_TEST_SAML_CLIENT', dashboardServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into dbAccesClientPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'ACCESS_TEST_SAML_CLIENT' LIMIT 1;	
	


						
INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('TEST_SAML_CLIENT_USER', 'Test SAML Client User', 'Test SSO Client User', 2, 1, 2, 0, 0, now(), now(), 
			false, false);
SELECT "Id" into clientUserRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'TEST_SAML_CLIENT_USER' AND "CcsAccessRoleName" = 'Test SAML Client User' LIMIT 1;		

			
INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('ACCESS_TEST_SAML_CLIENT', 'Access Test SAML Client', 'Access Test Client', 2, 1, 2, 0, 0, now(), now(), 
			false, false);			
SELECT "Id" into dbAccessClientRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ACCESS_TEST_SAML_CLIENT' LIMIT 1;




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
SELECT create_demo_service();
DROP FUNCTION create_demo_service;
