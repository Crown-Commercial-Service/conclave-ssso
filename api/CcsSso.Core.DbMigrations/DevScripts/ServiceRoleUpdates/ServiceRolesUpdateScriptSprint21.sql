-- DELETING

DELETE FROM public."CcsAccessRole"
	WHERE  "CcsAccessRoleNameKey"='RMI_ADMIN' AND "CcsAccessRoleName"='RMI Administrator' ;

DELETE FROM public."CcsAccessRole"
	WHERE  "CcsAccessRoleNameKey"='RMI_EXAMPLE' AND "CcsAccessRoleName"='RMI Example' ;

DELETE FROM public."CcsAccessRole"
	WHERE  "CcsAccessRoleNameKey"='UR_DEMO_ROLE' AND "CcsAccessRoleName"='UR Demo Role' ;

DELETE FROM public."CcsAccessRole"
	WHERE  "CcsAccessRoleNameKey"='EL_ADMIN' AND "CcsAccessRoleName"='Evidence Locker Admin' ;

DELETE FROM public."CcsAccessRole"
	WHERE  "CcsAccessRoleNameKey"='EL_User' AND "CcsAccessRoleName"='Evidence Locker User' ;

DELETE FROM public."CcsAccessRole"
	WHERE  "CcsAccessRoleNameKey"='TEST_APP_USER' AND "CcsAccessRoleName"='Demo SSO App User' ;

-- RMI

UPDATE public."CcsAccessRole"
	SET "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=2, "LastUpdatedOnUtc"=now()
	WHERE "CcsAccessRoleNameKey"='ACCESS_RMI_CLIENT' AND "CcsAccessRoleName"='Access RMI' ;

UPDATE public."CcsAccessRole"
	SET "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=2, "LastUpdatedOnUtc"=now()
	WHERE "CcsAccessRoleNameKey"='RMI_USER' AND "CcsAccessRoleName"='RMI User' ;


-- DMP

UPDATE public."CcsAccessRole"
	SET "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=2, "LastUpdatedOnUtc"=now()
	WHERE "CcsAccessRoleNameKey"='DMP_SUPPLIER' AND "CcsAccessRoleName"='DMP Supplier' ;

UPDATE public."CcsAccessRole"
	SET "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=2, "LastUpdatedOnUtc"=now()
	WHERE "CcsAccessRoleNameKey"='ACCESS_DMP' AND "CcsAccessRoleName"='ACCESS_DMP' ;

-- EL

UPDATE public."CcsAccessRole"
	SET "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=1, "LastUpdatedOnUtc"=now()
	WHERE "CcsAccessRoleNameKey"='EL_SNR_BUYER' AND "CcsAccessRoleName"='Snr Buyer' ;

UPDATE public."CcsAccessRole"
	SET "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=1, "LastUpdatedOnUtc"=now()
	WHERE "CcsAccessRoleNameKey"='EL_JNR_BUYER' AND "CcsAccessRoleName"='Jnr Buyer' ;

UPDATE public."CcsAccessRole"
	SET "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=2, "LastUpdatedOnUtc"=now()
	WHERE "CcsAccessRoleNameKey"='EL_SNR_SUPPLIER' AND "CcsAccessRoleName"='Snr Supplier' ;

UPDATE public."CcsAccessRole"
	SET "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=2, "LastUpdatedOnUtc"=now()
	WHERE "CcsAccessRoleNameKey"='EL_JNR_SUPPLIER' AND "CcsAccessRoleName"='Jnr Supplier' ;

UPDATE public."CcsAccessRole"
	SET "OrgTypeEligibility"=0, "SubscriptionTypeEligibility"=1, "TradeEligibility"=2, "LastUpdatedOnUtc"=now()
	WHERE "CcsAccessRoleNameKey"='EL_CCS_SNR_ADMIN' AND "CcsAccessRoleName"='CCS Snr Admin' ;

UPDATE public."CcsAccessRole"
	SET "OrgTypeEligibility"=0, "SubscriptionTypeEligibility"=1, "TradeEligibility"=2, "LastUpdatedOnUtc"=now()
	WHERE "CcsAccessRoleNameKey"='EL_CCS_JNR_ADMIN' AND "CcsAccessRoleName"='CCS Jnr Admin' ;

UPDATE public."CcsAccessRole"
	SET "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=0, "TradeEligibility"=2, "LastUpdatedOnUtc"=now()
	WHERE "CcsAccessRoleNameKey"='ACCESS_EVIDENCE_LOCKER' AND "CcsAccessRoleName"='Access Evidence Locker' ;

-- CAT

UPDATE public."CcsAccessRole"
	SET "OrgTypeEligibility"=0, "SubscriptionTypeEligibility"=1, "TradeEligibility"=2, "LastUpdatedOnUtc"=now()
	WHERE "CcsAccessRoleNameKey"='CAT_ADMINISTRATOR' AND "CcsAccessRoleName"='CaT Admin' ;

UPDATE public."CcsAccessRole"
	SET "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=1, "LastUpdatedOnUtc"=now()
	WHERE "CcsAccessRoleNameKey"='CAT_USER' AND "CcsAccessRoleName"='CaT User' ;

UPDATE public."CcsAccessRole"
	SET "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=1, "LastUpdatedOnUtc"=now()
	WHERE "CcsAccessRoleNameKey"='ACCESS_CAT' AND "CcsAccessRoleName"='Access CaT' ;

-- DIGITS

UPDATE public."CcsAccessRole"
	SET "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=1, "LastUpdatedOnUtc"=now()
	WHERE "CcsAccessRoleNameKey"='DIGITS_DEPARTMENT_ADMIN' AND "CcsAccessRoleName"='Department Admin' ;

UPDATE public."CcsAccessRole"
	SET "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=1, "LastUpdatedOnUtc"=now()
	WHERE "CcsAccessRoleNameKey"='DIGITS_CONTRACT_OWNER' AND "CcsAccessRoleName"='Contract Owner' ;

UPDATE public."CcsAccessRole"
	SET "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=1, "LastUpdatedOnUtc"=now()
	WHERE "CcsAccessRoleNameKey"='DIGITS_MI' AND "CcsAccessRoleName"='MI' ;

UPDATE public."CcsAccessRole"
	SET "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=1, "LastUpdatedOnUtc"=now()
	WHERE "CcsAccessRoleNameKey"='USER' AND "CcsAccessRoleName"='User' ;

UPDATE public."CcsAccessRole"
	SET "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=1, "LastUpdatedOnUtc"=now()
	WHERE "CcsAccessRoleNameKey"='SERVICE_ADMIN' AND "CcsAccessRoleName"='Service Admin' ;

UPDATE public."CcsAccessRole"
	SET "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=1, "LastUpdatedOnUtc"=now()
	WHERE "CcsAccessRoleNameKey"='PROVIDER_APP' AND "CcsAccessRoleName"='API Access Role' ;

UPDATE public."CcsAccessRole"
	SET "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=1, "LastUpdatedOnUtc"=now()
	WHERE "CcsAccessRoleNameKey"='ACCESS_DIGITS_CLIENT' AND "CcsAccessRoleName"='Access DigiTS' ;

-- INSERT ACCESS RMI ROLE if not exists

CREATE OR REPLACE FUNCTION create_rmi_access_role() RETURNS integer AS $$

DECLARE dashboardServiceId int;
DECLARE dbAccesClientPermissionId int;
DECLARE dbAccessClientRoleId int;

  BEGIN	
    SELECT "Id" into dashboardServiceId From public."CcsService" WHERE "ServiceName" = 'Dashboard Service' LIMIT 1;

    IF NOT EXISTS (SELECT "Id" FROM public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ACCESS_RMI_CLIENT') THEN
          INSERT INTO public."ServicePermission"(
	          "ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	          VALUES ('ACCESS_RMI_CLIENT', dashboardServiceId, 0, 0, now(), now(), false);	
          SELECT "Id" into dbAccesClientPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'ACCESS_RMI_CLIENT' LIMIT 1;	
			
          INSERT INTO public."CcsAccessRole"(
	          "CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	          "SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	          "LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	          VALUES ('ACCESS_RMI_CLIENT', 'Access RMI', 'Access RMI', 2, 1, 2, 0, 0, now(), now(), 
			          false, false);			
          SELECT "Id" into dbAccessClientRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ACCESS_RMI_CLIENT' LIMIT 1;

          INSERT INTO public."ServiceRolePermission"(
	          "ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	          VALUES (dbAccesClientPermissionId, dbAccessClientRoleId, 0, 0, now(), now(), false);
    END IF;

	RETURN 1;
	END;
$$ LANGUAGE plpgsql;

SELECT setval('"CcsAccessRole_Id_seq"', max("Id")) FROM "CcsAccessRole";
SELECT setval('"CcsService_Id_seq"', max("Id")) FROM "CcsService";
SELECT setval('"ServicePermission_Id_seq"', max("Id")) FROM "ServicePermission";
SELECT setval('"ServiceRolePermission_Id_seq"', max("Id")) FROM "ServiceRolePermission";
SELECT create_rmi_access_role();
DROP FUNCTION create_rmi_access_role;


