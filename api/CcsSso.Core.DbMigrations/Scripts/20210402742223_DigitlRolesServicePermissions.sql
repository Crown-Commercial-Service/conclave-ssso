--

CREATE OR REPLACE FUNCTION create_digit_role_service_permissions() RETURNS integer AS $$

  DECLARE serviceName text = 'DigiTS Service';
  DECLARE serviceClientId text = 'tAznfZOoZkJmI7hhEWugDHRr25OKXBfU';
  DECLARE serviceUrl text = 'http://localhost:9090';
  DECLARE dashboardServiceName text = 'Dashboard Service';
	
	DECLARE digitsEnableInDashboardAccessRoleId int;
	DECLARE digitsApiAdminAccessRoleId int;
	DECLARE digitsApiUserAccessRoleId int;
	DECLARE digitsDepartmentAdminAccessRoleId int;
	DECLARE digitsContractOwnerAccessRoleId int;
  DECLARE digitsMIAccessRoleId int;
  DECLARE digitsUserAccessRoleId int;

  DECLARE dashboardServiceId int;
	DECLARE digitServiceId int;

  DECLARE digitsEnableInDashboardPermissionId int;
	DECLARE digitsApiAdminPermissionId int;
	DECLARE digitsApiUserPermissionId int;
	DECLARE digitsDepartmentAdminPermissionId int;
	DECLARE digitsContractOwnerPermissionId int;
	DECLARE digitsMIPermissionId int;
	DECLARE digitsUserPermissionId int;
	
    BEGIN

    INSERT INTO public."CcsAccessRole"(
			"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility")
			VALUES ('ACCESS_DIGITS_CLIENT', 'Access DigiTS', 'Access DigiTS', 0, 0, now(), now(), false, 1, 0, 2);
		INSERT INTO public."CcsAccessRole"(
			"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility")
			VALUES ('SERVICE_ADMIN', 'DigiTS API Administrator', 'DigiTS API Administrator', 0, 0, now(), now(), false, 1, 0, 2);
		INSERT INTO public."CcsAccessRole"(
			"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility")
			VALUES ('PROVIDER_APP', 'DigiTS API User', 'DigiTS API User', 0, 0, now(), now(), false, 1, 0, 2);
		INSERT INTO public."CcsAccessRole"(
			"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility")
			VALUES ('DIGITS_DEPARTMENT_ADMIN', 'DigiTS Department Admin', 'DigiTS Department Admin', 0, 0, now(), now(), false, 1, 0, 2);
		INSERT INTO public."CcsAccessRole"(
			"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility")
			VALUES ('DIGITS_CONTRACT_OWNER', 'DigiTS Super Admin', 'DigiTS Super Admin', 0, 0, now(), now(), false, 1, 0, 2);
    INSERT INTO public."CcsAccessRole"(
			"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility")
			VALUES ('DIGITS_MI', 'DigiTS MI', 'DigiTS MI', 0, 0, now(), now(), false, 1, 0, 2);
      INSERT INTO public."CcsAccessRole"(
			"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility")
			VALUES ('USER', 'DigiTS User', 'DigiTS User', 0, 0, now(), now(), false, 1, 0, 2);

		SELECT "Id" into digitsEnableInDashboardAccessRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ACCESS_DIGITS_CLIENT' LIMIT 1;
		SELECT "Id" into digitsApiAdminAccessRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'SERVICE_ADMIN' LIMIT 1;
		SELECT "Id" into digitsApiUserAccessRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'PROVIDER_APP' LIMIT 1;
		SELECT "Id" into digitsDepartmentAdminAccessRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'DIGITS_DEPARTMENT_ADMIN' LIMIT 1;
		SELECT "Id" into digitsContractOwnerAccessRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'DIGITS_CONTRACT_OWNER' LIMIT 1;
		SELECT "Id" into digitsMIAccessRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'DIGITS_MI' LIMIT 1;
		SELECT "Id" into digitsUserAccessRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'USER' LIMIT 1;
		
		INSERT INTO public."CcsService"(
			"ServiceName", "TimeOutLength", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "ServiceClientId", "ServiceUrl")
			VALUES (serviceName, 0, 0, 0, now(), now(), false, serviceClientId, serviceUrl);
			
		SELECT "Id" into digitServiceId From public."CcsService" WHERE "ServiceName" = serviceName LIMIT 1;
		SELECT "Id" into dashboardServiceId From public."CcsService" WHERE "ServiceName" = dashboardServiceName LIMIT 1;
		
		INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('ACCESS_DIGITS_CLIENT', dashboardServiceId, 0, 0, now(), now(), false);
    INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('SERVICE_ADMIN', digitServiceId, 0, 0, now(), now(), false);
		INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('PROVIDER_APP', digitServiceId, 0, 0, now(), now(), false);
		INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('DIGITS_DEPARTMENT_ADMIN', digitServiceId, 0, 0, now(), now(), false);
		INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('DIGITS_CONTRACT_OWNER', digitServiceId, 0, 0, now(), now(), false);
		INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('DIGITS_MI', digitServiceId, 0, 0, now(), now(), false);
    INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('USER', digitServiceId, 0, 0, now(), now(), false);
			
		SELECT "Id" into digitsEnableInDashboardPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'ACCESS_DIGITS_CLIENT' LIMIT 1;
		SELECT "Id" into digitsApiAdminPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'SERVICE_ADMIN' LIMIT 1;
		SELECT "Id" into digitsApiUserPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'PROVIDER_APP' LIMIT 1;
		SELECT "Id" into digitsDepartmentAdminPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'DIGITS_DEPARTMENT_ADMIN' LIMIT 1;
		SELECT "Id" into digitsContractOwnerPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'DIGITS_CONTRACT_OWNER' LIMIT 1;
		SELECT "Id" into digitsMIPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'DIGITS_MI' LIMIT 1;
		SELECT "Id" into digitsUserPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'USER' LIMIT 1;
		
		INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (digitsEnableInDashboardAccessRoleId, digitsEnableInDashboardPermissionId, 0, 0, now(), now(), false);
    INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (digitsApiAdminAccessRoleId, digitsApiAdminPermissionId, 0, 0, now(), now(), false);
		INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (digitsApiUserAccessRoleId, digitsApiUserPermissionId, 0, 0, now(), now(), false);
		INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (digitsDepartmentAdminAccessRoleId, digitsDepartmentAdminPermissionId, 0, 0, now(), now(), false);
		INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (digitsContractOwnerAccessRoleId, digitsContractOwnerPermissionId, 0, 0, now(), now(), false);
		INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (digitsMIAccessRoleId, digitsMIPermissionId, 0, 0, now(), now(), false);
    INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (digitsUserAccessRoleId, digitsUserPermissionId, 0, 0, now(), now(), false);

		RETURN 1;
	END;
$$ LANGUAGE plpgsql;

SELECT create_digit_role_service_permissions();

DROP FUNCTION create_digit_role_service_permissions;
