-- Create User research demo services

CREATE OR REPLACE FUNCTION create_user_research_demo_role_service_permissions() RETURNS integer AS $$

  DECLARE digitsClientId text = '';
	DECLARE demoRoleId int;
	
	DECLARE dashboardServiceId int;
	
	DECLARE dashboardPurchasePermissionId int;
	DECLARE dashboardEvidencePermissionId int;
	DECLARE dashboardAgreementPermissionId int;
	DECLARE dashboardDigitsPermissionId int;
		
    BEGIN
				
		INSERT INTO public."CcsAccessRole"(
			"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility")
			VALUES ('UR_DEMO_ROLE', 'UR Demo Role', 'Demo role for user research. This is for dummy tiles visibility in dashboard.', 0, 0, now(), now(), false, 2, 0, 2);

		SELECT "Id" into demoRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'UR_DEMO_ROLE' LIMIT 1;

    INSERT INTO public."CcsService"(
	    "ServiceName", "TimeOutLength", "ServiceClientId", "ServiceUrl", "Description", "ServiceCode", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	    VALUES ('Purchasing Platform', 0, 'UR_DEMO1', 'https://localhost:4300', 'Buy everyday products using online catalogues', 'PURCHASING_PLATFORM_CLIENT', 0, 0, now(), now(), false);
    INSERT INTO public."CcsService"(
	    "ServiceName", "TimeOutLength", "ServiceClientId", "ServiceUrl", "Description", "ServiceCode", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	    VALUES ('Evidence Locker', 0, 'UR_DEMO2', 'https://localhost:4300', 'Store procurement information from previous bids so you don’t need to provide the same evidence over and over', 'EVIDENCE_LOCKER_CLIENT', 0, 0, now(), now(), false);
    INSERT INTO public."CcsService"(
	    "ServiceName", "TimeOutLength", "ServiceClientId", "ServiceUrl", "Description", "ServiceCode", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	    VALUES ('Agreement Service', 0, 'UR_DEMO3', 'https://localhost:4300', 'Helps you choose the right commercial agreements for your organisation', 'AGREEMENT_SERVICE_CLIENT', 0, 0, now(), now(), false);

    IF NOT EXISTS (SELECT "Id" From public."CcsService" WHERE "ServiceClientId" = digitsClientId and "ServiceCode"='DIGITS_CLIENT' LIMIT 1) THEN
      INSERT INTO public."CcsService"(
	    "ServiceName", "TimeOutLength", "ServiceClientId", "ServiceUrl", "Description", "ServiceCode", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	    VALUES ('DigiTs', 0, digitsClientId, 'https://localhost:4300', 'Book rail, accommodation, air travel and more', 'DIGITS_CLIENT', 0, 0, now(), now(), false);
   	END IF;

    SELECT "Id" into dashboardServiceId From public."CcsService" WHERE "ServiceName" = 'Dashboard Service' LIMIT 1;
		
		INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('ACCESS_PURCHASING_PLATFORM_CLIENT', dashboardServiceId, 0, 0, now(), now(), false);
		INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('ACCESS_EVIDENCE_LOCKER_CLIENT', dashboardServiceId, 0, 0, now(), now(), false);
		INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('ACCESS_AGREEMENT_SERVICE_CLIENT', dashboardServiceId, 0, 0, now(), now(), false);
		INSERT INTO public."ServicePermission"(
			"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('ACCESS_DIGITS_CLIENT', dashboardServiceId, 0, 0, now(), now(), false);
			
		SELECT "Id" into dashboardPurchasePermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'ACCESS_PURCHASING_PLATFORM_CLIENT' and "CcsServiceId" = dashboardServiceId LIMIT 1;
		SELECT "Id" into dashboardEvidencePermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'ACCESS_EVIDENCE_LOCKER_CLIENT' and "CcsServiceId" = dashboardServiceId LIMIT 1;
		SELECT "Id" into dashboardAgreementPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'ACCESS_AGREEMENT_SERVICE_CLIENT' and "CcsServiceId" = dashboardServiceId LIMIT 1;
		SELECT "Id" into dashboardDigitsPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'ACCESS_DIGITS_CLIENT' and "CcsServiceId" = dashboardServiceId LIMIT 1;
		
		INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (demoRoleId, dashboardPurchasePermissionId, 0, 0, now(), now(), false);
		INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (demoRoleId, dashboardEvidencePermissionId, 0, 0, now(), now(), false);
		INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (demoRoleId, dashboardAgreementPermissionId, 0, 0, now(), now(), false);
		INSERT INTO public."ServiceRolePermission"(
			"CcsAccessRoleId", "ServicePermissionId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (demoRoleId, dashboardDigitsPermissionId, 0, 0, now(), now(), false);
		RETURN 1;
	END;
$$ LANGUAGE plpgsql;

SELECT setval('"CcsAccessRole_Id_seq"', max("Id")) FROM "CcsAccessRole";
SELECT setval('"CcsService_Id_seq"', max("Id")) FROM "CcsService";
SELECT setval('"ServicePermission_Id_seq"', max("Id")) FROM "ServicePermission";
SELECT setval('"ServiceRolePermission_Id_seq"', max("Id")) FROM "ServiceRolePermission";
SELECT create_user_research_demo_role_service_permissions();

DROP FUNCTION create_user_research_demo_role_service_permissions;
