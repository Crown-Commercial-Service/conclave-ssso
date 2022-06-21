CREATE OR REPLACE FUNCTION create_ld_service() RETURNS integer AS $$

-- Add values to clientId and clientUrl
DECLARE serviceName text = 'Login Director';
DECLARE serviceDescription text = 'Login Director';
DECLARE serviceCode text = 'LOGIN_DIRECTOR_CLIENT';
DECLARE clientUrl text = 'https://localhost:4200/ldnothing';
DECLARE clientId text = '';

DECLARE clientServiceId int;
DECLARE dashboardServiceId int;

DECLARE clientCatUserPermissionId int;
DECLARE clientJSPermissionId int;
DECLARE clientJBPermissionId int;

DECLARE dbAccesClientPermissionId int;

DECLARE clientCatUserRoleId int;
DECLARE clientJSRoleId int;
DECLARE clientJBRoleId int;

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
	VALUES ('CAT_USER_LOGIN_DIRECTOR', clientServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into clientCatUserPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'CAT_USER_LOGIN_DIRECTOR' AND "CcsServiceId" = clientServiceId LIMIT 1;			

INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('JAEGGER_SUPPLIER', clientServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into clientJSPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'JAEGGER_SUPPLIER' AND "CcsServiceId" = clientServiceId LIMIT 1;

INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('JAEGGER_BUYER', clientServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into clientJBPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'JAEGGER_BUYER' AND "CcsServiceId" = clientServiceId LIMIT 1;

INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('ACCESS_LOGIN_DIRECTOR_CLIENT', dashboardServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into dbAccesClientPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'ACCESS_LOGIN_DIRECTOR_CLIENT' LIMIT 1;	
	

INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('CAT_USER_LOGIN_DIRECTOR', 'CAS User', 'CAS User', 2, 1, 2, 0, 0, now(), now(), 
			false, false);
SELECT "Id" into clientCatUserRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'CAT_USER_LOGIN_DIRECTOR' AND "CcsAccessRoleName" = 'CAS User' LIMIT 1;
						
INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('JAEGGER_SUPPLIER', 'Jaggaer Supplier', 'Jaggaer Supplier', 2, 0, 2, 0, 0, now(), now(), 
			false, false);
SELECT "Id" into clientJSRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'JAEGGER_SUPPLIER' AND "CcsAccessRoleName" = 'Jaegger Supplier' LIMIT 1;

INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('JAEGGER_BUYER', 'Jaggaer Buyer', 'Jaegger Buyer', 2, 1, 1, 0, 0, now(), now(), 
			false, false);
SELECT "Id" into clientJBRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'JAEGGER_BUYER' AND "CcsAccessRoleName" = 'Jaggaer Buyer' LIMIT 1;

			
INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('ACCESS_LOGIN_DIRECTOR_CLIENT', 'Access Login Director', 'Access Login Director', 2, 1, 2, 0, 0, now(), now(), 
			true, false);			
SELECT "Id" into dbAccessClientRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ACCESS_LOGIN_DIRECTOR_CLIENT' LIMIT 1;



INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (clientCatUserPermissionId, clientCatUserRoleId, 0, 0, now(), now(), false);

INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (clientJSPermissionId, clientJSRoleId, 0, 0, now(), now(), false);

INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (clientJBPermissionId, clientJBRoleId, 0, 0, now(), now(), false);


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
SELECT create_ld_service();
DROP FUNCTION create_ld_service;
