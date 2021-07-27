-- Create Roles 'CCS Administrator', 'Organisation Administrator', 'Organisation User'
-- Create Service 'Dashboard Service'
-- Create Service Permissions 'MANAGE_USERS', 'MANAGE_ORG', 'MANAGE_GROUPS', 'MANAGE_MY_ACCOUNT', 'MANAGE_SIGN_IN_PROVIDERS'
-- Map Role with Service Permissions
-- 'CCS Administrator' :- 'MANAGE_USERS', 'MANAGE_ORG', 'MANAGE_GROUPS', 'MANAGE_MY_ACCOUNT', 'MANAGE_SIGN_IN_PROVIDERS'
-- 'Organisation Administrator' :- 'MANAGE_USERS', 'MANAGE_ORG', 'MANAGE_GROUPS', 'MANAGE_MY_ACCOUNT'
-- 'Organisation User' :- 'MANAGE_MY_ACCOUNT'

CREATE OR REPLACE FUNCTION create_initial_role_service_permissions() RETURNS integer AS $$
	
	DECLARE ccsAdminAccessRoleId int;
	DECLARE orgAdminAccessRoleId int;
	DECLARE orgUserAccessRoleId int;
	DECLARE testSsoAccessRoleId int;
	
	DECLARE dashboardServiceId int;
	
	DECLARE manageUsersPermissionId int;
	DECLARE manageOrgPermissionId int;
	DECLARE manageGroupsPermissionId int;
	DECLARE manageMyAccountPermissionId int;
	DECLARE manageSignInProvidersPermissionId int;
	DECLARE testClientPermissionId int;
		
    BEGIN
				
		INSERT INTO public."CcsAccessRole"(
			"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility")
			VALUES ('CCS_ADMINISTRATOR', 'CCS Administrator', 'Administrator of the CCS', 0, 0, now(), now(), false, 0, 0, 0);
		INSERT INTO public."CcsAccessRole"(
			"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility")
			VALUES ('ORG_ADMINISTRATOR', 'Organisation Administrator', 'Administrator of as organisation', 0, 0, now(), now(), false, 0, 0, 0);
		INSERT INTO public."CcsAccessRole"(
			"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility")
			VALUES ('ORG_DEFAULT_USER', 'Organisation User', 'Default user of an organisation', 0, 0, now(), now(), false, 0, 0, 0);
		INSERT INTO public."CcsAccessRole"(
			"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility")
			VALUES ('TEST_SSO_CLIENT_USER', 'Test Client User', 'Test Client User', 0, 0, now(), now(), false, 0, 0, 0);

		SELECT "Id" into ccsAdminAccessRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'CCS_ADMINISTRATOR' LIMIT 1;
		SELECT "Id" into orgAdminAccessRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ORG_ADMINISTRATOR' LIMIT 1;
		SELECT "Id" into orgUserAccessRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ORG_DEFAULT_USER' LIMIT 1;
		SELECT "Id" into testSsoAccessRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'TEST_SSO_CLIENT_USER' LIMIT 1;
		
		INSERT INTO public."CcsService"(
			"ServiceName", "TimeOutLength", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('Dashboard Service', 0, 0, 0, now(), now(), false);
			
		SELECT "Id" into dashboardServiceId From public."CcsService" WHERE "ServiceName" = 'Dashboard Service' LIMIT 1;
		
		INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('MANAGE_USERS', dashboardServiceId, 0, 0, now(), now(), false);
		INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('MANAGE_ORG', dashboardServiceId, 0, 0, now(), now(), false);
		INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('MANAGE_GROUPS', dashboardServiceId, 0, 0, now(), now(), false);
		INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('MANAGE_MY_ACCOUNT', dashboardServiceId, 0, 0, now(), now(), false);
		INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('MANAGE_SIGN_IN_PROVIDERS', dashboardServiceId, 0, 0, now(), now(), false);
    INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('TEST_CLIENT', dashboardServiceId, 0, 0, now(), now(), false);
			
		SELECT "Id" into manageUsersPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'MANAGE_USERS' LIMIT 1;
		SELECT "Id" into manageOrgPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'MANAGE_ORG' LIMIT 1;
		SELECT "Id" into manageGroupsPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'MANAGE_GROUPS' LIMIT 1;
		SELECT "Id" into manageMyAccountPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'MANAGE_MY_ACCOUNT' LIMIT 1;
		SELECT "Id" into manageSignInProvidersPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'MANAGE_SIGN_IN_PROVIDERS' LIMIT 1;
		SELECT "Id" into testClientPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'TEST_CLIENT' LIMIT 1;
		
		INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (ccsAdminAccessRoleId, manageUsersPermissionId, 0, 0, now(), now(), false);
		INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (ccsAdminAccessRoleId, manageOrgPermissionId, 0, 0, now(), now(), false);
		INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (ccsAdminAccessRoleId, manageGroupsPermissionId, 0, 0, now(), now(), false);
		INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (ccsAdminAccessRoleId, manageMyAccountPermissionId, 0, 0, now(), now(), false);
		INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (ccsAdminAccessRoleId, manageSignInProvidersPermissionId, 0, 0, now(), now(), false);
			
		INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (orgAdminAccessRoleId, manageUsersPermissionId, 0, 0, now(), now(), false);
		INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (orgAdminAccessRoleId, manageOrgPermissionId, 0, 0, now(), now(), false);
		INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (orgAdminAccessRoleId, manageGroupsPermissionId, 0, 0, now(), now(), false);
		INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (orgAdminAccessRoleId, manageMyAccountPermissionId, 0, 0, now(), now(), false);
			
		INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (orgUserAccessRoleId, manageMyAccountPermissionId, 0, 0, now(), now(), false);

    INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (testSsoAccessRoleId, testClientPermissionId, 0, 0, now(), now(), false);
		RETURN 1;
	END;
$$ LANGUAGE plpgsql;

SELECT create_initial_role_service_permissions();

DROP FUNCTION create_initial_role_service_permissions;
