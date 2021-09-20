CREATE OR REPLACE FUNCTION create_new_ccs_service() RETURNS integer AS $$

DECLARE elServiceId int;
DECLARE dashboardServiceId int;
DECLARE elAdminPermissionId int;
DECLARE elUserPermissionId int;
DECLARE dbELAccessermissionId int;
DECLARE elAccesPermissionId int;
DECLARE elAdminRoleId int;
DECLARE elUserRoleId int;
DECLARE dbAccessELRoleId int;
-- repalce clientId and clientUrl
BEGIN	
INSERT INTO public."CcsService"(
	"ServiceName", "TimeOutLength", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc",
	"IsDeleted", "ServiceClientId", "ServiceUrl", "Description", "ServiceCode")
	VALUES ('Evidence Locker', 0, 0, 0, now(), now(), false, clientId, 
			clientUrl, 'Evidence Locker','EVIDENCE_LOCKER');
			
			
SELECT "Id" into elServiceId From public."CcsService" WHERE "ServiceName" = 'Evidence Locker' LIMIT 1;
SELECT "Id" into dashboardServiceId From public."CcsService" WHERE "ServiceName" = 'Dashboard Service' LIMIT 1;	


INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('EL_ADMIN', elServiceId, 0, 0, now(), now(), false);
SELECT "Id" into elAdminPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'EL_ADMIN' LIMIT 1;				

INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('EL_User', elServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into elUserPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'EL_User' LIMIT 1;	

INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('ACCESS_EVIDENCE_LOCKER', dashboardServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into elAccesPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'ACCESS_EVIDENCE_LOCKER' LIMIT 1;	
	
			
INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('EL_ADMIN', 'Evidence Locker Admin', 'Evidence Locker Admin', 1, 0, 2, 0, 0, now(), now(), 
			false, true);
SELECT "Id" into elAdminRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'EL_ADMIN' LIMIT 1;
			
INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('EL_User', 'Evidence Locker User', 'Evidence Locker User', 1, 0, 2, 0, 0, now(), now(), 
			false, false);
SELECT "Id" into elUserRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'EL_User' LIMIT 1;
			
			
INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('ACCESS_EVIDENCE_LOCKER', 'Access Evidence Locker', 'Access Evidence Locker', 1, 0, 2, 0, 0, now(), now(), 
			false, false);			
SELECT "Id" into dbAccessELRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ACCESS_EVIDENCE_LOCKER' LIMIT 1;

INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (elAdminPermissionId, elAdminRoleId, 0, 0, now(), now(), false);
	
INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (elUserPermissionId, elUserRoleId, 0, 0, now(), now(), false);

INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (elAccesPermissionId, dbAccessELRoleId, 0, 0, now(), now(), false);

	RETURN 1;
	END;
$$ LANGUAGE plpgsql;


SELECT create_new_ccs_service();

DROP FUNCTION create_new_ccs_service;
