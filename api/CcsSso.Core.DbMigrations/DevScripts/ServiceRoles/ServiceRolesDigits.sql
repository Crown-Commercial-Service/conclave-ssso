CREATE OR REPLACE FUNCTION create_digits_service() RETURNS integer AS $$

-- Add values to clientId and clientUrl
DECLARE serviceName text = 'DigiTS';
DECLARE serviceDescription text = 'Book rail, accommodation, air travel and more';
DECLARE serviceCode text = 'DIGITS_CLIENT';
DECLARE clientUrl text = '';
DECLARE clientId text = '';

DECLARE clientServiceId int;
DECLARE dashboardServiceId int;

DECLARE clientAdminPermissionId int;
DECLARE clientCOPermissionId int;
DECLARE clientMIPermissionId int;
DECLARE clientUserPermissionId int;
DECLARE clientSAPermissionId int;
DECLARE clientPAPermissionId int;

DECLARE dbAccesClientPermissionId int;

DECLARE clientAdminRoleId int;
DECLARE clientCORoleId int;
DECLARE clientMIRoleId int;
DECLARE clientUserRoleId int;
DECLARE clientSARoleId int;
DECLARE clientPARoleId int;

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
	VALUES ('DIGITS_DEPARTMENT_ADMIN', clientServiceId, 0, 0, now(), now(), false);
SELECT "Id" into clientAdminPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'DIGITS_DEPARTMENT_ADMIN' AND "CcsServiceId" = clientServiceId  LIMIT 1;				

INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('DIGITS_CONTRACT_OWNER', clientServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into clientCOPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'DIGITS_CONTRACT_OWNER' AND "CcsServiceId" = clientServiceId LIMIT 1;

INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('DIGITS_MI', clientServiceId, 0, 0, now(), now(), false);
SELECT "Id" into clientMIPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'DIGITS_MI' AND "CcsServiceId" = clientServiceId LIMIT 1;				

INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('USER', clientServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into clientUserPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'USER' AND "CcsServiceId" = clientServiceId LIMIT 1;

INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('SERVICE_ADMIN', clientServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into clientSAPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'SERVICE_ADMIN' AND "CcsServiceId" = clientServiceId LIMIT 1;

INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('PROVIDER_APP', clientServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into clientPAPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'PROVIDER_APP' AND "CcsServiceId" = clientServiceId LIMIT 1;


INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('ACCESS_DIGITS_CLIENT', dashboardServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into dbAccesClientPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'ACCESS_DIGITS_CLIENT' LIMIT 1;	
	

			
INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('DIGITS_DEPARTMENT_ADMIN', 'Department Admin', 'Department Admin', 2, 1, 1, 0, 0, now(), now(), 
			false, true);
SELECT "Id" into clientAdminRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'DIGITS_DEPARTMENT_ADMIN' AND "CcsAccessRoleName" = 'Department Admin' LIMIT 1;
			
INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('DIGITS_CONTRACT_OWNER', 'Contract Owner', 'Contract Owner', 2, 1, 1, 0, 0, now(), now(), 
			false, false);
SELECT "Id" into clientCORoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'DIGITS_CONTRACT_OWNER' AND "CcsAccessRoleName" = 'Contract Owner' LIMIT 1;

INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('DIGITS_MI', 'MI', 'MI', 2, 1, 1, 0, 0, now(), now(), 
			false, true);
SELECT "Id" into clientMIRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'DIGITS_MI' AND "CcsAccessRoleName" = 'MI' LIMIT 1;
			
INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('USER', 'User', 'User', 2, 1, 1, 0, 0, now(), now(), 
			false, false);
SELECT "Id" into clientUserRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'USER' AND "CcsAccessRoleName" = 'User' LIMIT 1;

INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('SERVICE_ADMIN', 'Service Admin', 'Service Admin', 2, 1, 1, 0, 0, now(), now(), 
			false, false);
SELECT "Id" into clientSARoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'SERVICE_ADMIN' AND "CcsAccessRoleName" = 'Service Admin' LIMIT 1;

INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('PROVIDER_APP', 'API Access Role', 'API Access Role', 2, 1, 1, 0, 0, now(), now(), 
			false, false);
SELECT "Id" into clientPARoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'PROVIDER_APP' AND "CcsAccessRoleName" = 'API Access Role' LIMIT 1;		

			
INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('ACCESS_DIGITS_CLIENT', 'Access DigiTS', 'Access DigiTS', 2, 1, 1, 0, 0, now(), now(), 
			false, false);			
SELECT "Id" into dbAccessClientRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ACCESS_DIGITS_CLIENT' LIMIT 1;




INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (clientAdminPermissionId, clientAdminRoleId, 0, 0, now(), now(), false);

INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (clientCOPermissionId, clientCORoleId, 0, 0, now(), now(), false);

INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (clientMIPermissionId, clientMIRoleId, 0, 0, now(), now(), false);
	
INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (clientUserPermissionId, clientUserRoleId, 0, 0, now(), now(), false);

 INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (clientSAPermissionId, clientSARoleId, 0, 0, now(), now(), false);

INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (clientPAPermissionId, clientPARoleId, 0, 0, now(), now(), false);

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
SELECT create_digits_service();
DROP FUNCTION create_digits_service;
