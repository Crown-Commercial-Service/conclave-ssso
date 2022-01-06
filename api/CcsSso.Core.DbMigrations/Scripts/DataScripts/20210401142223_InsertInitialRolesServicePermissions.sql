-- Create Roles 'CCS Administrator', 'Organisation Administrator', 'Organisation User'
-- Create Service 'Dashboard Service'
-- Create Service Permissions 'MANAGE_USERS', 'MANAGE_ORG', 'MANAGE_GROUPS', 'MANAGE_MY_ACCOUNT', 'MANAGE_SIGN_IN_PROVIDERS'
-- Map Role with Service Permissions
-- 'CCS Administrator' :- 'MANAGE_USERS', 'MANAGE_ORG', 'MANAGE_GROUPS', 'MANAGE_MY_ACCOUNT', 'MANAGE_SIGN_IN_PROVIDERS'
-- 'Organisation Administrator' :- 'MANAGE_USERS', 'MANAGE_ORG', 'MANAGE_GROUPS', 'MANAGE_MY_ACCOUNT'
-- 'Organisation User' :- 'MANAGE_MY_ACCOUNT'

CREATE OR REPLACE FUNCTION create_initial_role_service_permissions() RETURNS integer AS $$
	
	DECLARE manageSubscriptionAccessRoleId int;
	DECLARE orgUserSupportAccessRoleId int;
	DECLARE orgAdminAccessRoleId int;
	DECLARE orgUserAccessRoleId int;
	
	DECLARE dashboardServiceId int;

  DECLARE manageSubscriptionPermissionId int;
	DECLARE orgUserSupportPermissionId int;
	DECLARE manageUsersPermissionId int;
	DECLARE manageOrgPermissionId int;
	DECLARE manageGroupsPermissionId int;
	DECLARE manageMyAccountPermissionId int;
	DECLARE manageSignInProvidersPermissionId int;
		
    BEGIN
				
		INSERT INTO public."CcsAccessRole"(
			"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility", "MfaEnabled")
			VALUES ('MANAGE_SUBSCRIPTIONS', 'Manage Subscription', 'Service Subscriptions for Organisation', 0, 0, now(), now(), false, 0, 0, 2, true);
    INSERT INTO public."CcsAccessRole"(
			"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility", "MfaEnabled")
			VALUES ('ORG_USER_SUPPORT', 'Organisation Users Support ', 'Support for Org Users', 0, 0, now(), now(), false, 0, 0, 2, true);
		INSERT INTO public."CcsAccessRole"(
			"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility", "MfaEnabled")
			VALUES ('ORG_ADMINISTRATOR', 'Organisation Administrator', 'Administrator of as organisation', 0, 0, now(), now(), false, 2, 0, 2, true);
		INSERT INTO public."CcsAccessRole"(
			"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility", "MfaEnabled")
			VALUES ('ORG_DEFAULT_USER', 'Organisation User', 'Default user of an organisation', 0, 0, now(), now(), false, 2, 0, 2, false);

		SELECT "Id" into manageSubscriptionAccessRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'MANAGE_SUBSCRIPTIONS' LIMIT 1;
		SELECT "Id" into orgUserSupportAccessRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ORG_USER_SUPPORT' LIMIT 1;
		SELECT "Id" into orgAdminAccessRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ORG_ADMINISTRATOR' LIMIT 1;
		SELECT "Id" into orgUserAccessRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ORG_DEFAULT_USER' LIMIT 1;
		
		INSERT INTO public."CcsService"(
			"ServiceName", "TimeOutLength", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "GlobalLevelOrganisationAccess", "ActivateOrganisations")
			VALUES ('Dashboard Service', 0, 0, 0, now(), now(), false, true, false);
			
		SELECT "Id" into dashboardServiceId From public."CcsService" WHERE "ServiceName" = 'Dashboard Service' LIMIT 1;
		
		INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('MANAGE_SUBSCRIPTIONS', dashboardServiceId, 0, 0, now(), now(), false);
    INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('ORG_USER_SUPPORT', dashboardServiceId, 0, 0, now(), now(), false);

    INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('MANAGE_USERS', dashboardServiceId, 0, 0, now(), now(), false);
		INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('MANAGE_ORGS', dashboardServiceId, 0, 0, now(), now(), false);
		INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('MANAGE_GROUPS', dashboardServiceId, 0, 0, now(), now(), false);
		INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('MANAGE_MY_ACCOUNT', dashboardServiceId, 0, 0, now(), now(), false);
		INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('MANAGE_SIGN_IN_PROVIDERS', dashboardServiceId, 0, 0, now(), now(), false);
			
		SELECT "Id" into manageSubscriptionPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'MANAGE_SUBSCRIPTIONS' LIMIT 1;
		SELECT "Id" into orgUserSupportPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'ORG_USER_SUPPORT' LIMIT 1;

		SELECT "Id" into manageUsersPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'MANAGE_USERS' LIMIT 1;
		SELECT "Id" into manageOrgPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'MANAGE_ORGS' LIMIT 1;
		SELECT "Id" into manageGroupsPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'MANAGE_GROUPS' LIMIT 1;
		SELECT "Id" into manageMyAccountPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'MANAGE_MY_ACCOUNT' LIMIT 1;
		SELECT "Id" into manageSignInProvidersPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'MANAGE_SIGN_IN_PROVIDERS' LIMIT 1;
		
		INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (manageSubscriptionAccessRoleId, manageSubscriptionPermissionId, 0, 0, now(), now(), false);
		INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (orgUserSupportAccessRoleId, orgUserSupportPermissionId, 0, 0, now(), now(), false);
			
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
    RETURN 1;
	END;
$$ LANGUAGE plpgsql;

SELECT setval('"CcsAccessRole_Id_seq"', max("Id")) FROM "CcsAccessRole";
SELECT setval('"CcsService_Id_seq"', max("Id")) FROM "CcsService";
SELECT setval('"ServicePermission_Id_seq"', max("Id")) FROM "ServicePermission";
SELECT setval('"ServiceRolePermission_Id_seq"', max("Id")) FROM "ServiceRolePermission";
SELECT create_initial_role_service_permissions();
DROP FUNCTION create_initial_role_service_permissions;
