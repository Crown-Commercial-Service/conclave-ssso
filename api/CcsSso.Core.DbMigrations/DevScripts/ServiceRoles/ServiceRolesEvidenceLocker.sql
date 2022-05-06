CREATE OR REPLACE FUNCTION create_el_service() RETURNS integer AS $$

-- Add values to clientId and clientUrl
DECLARE serviceName text = 'Buyer/Supplier Information';
DECLARE serviceDescription text = 'Store procurement information from previous bids so you don’t need to provide the same evidence over and over';
DECLARE serviceCode text = 'EVIDENCE_LOCKER';
DECLARE clientUrl text = '';
DECLARE clientId text = '';

DECLARE elServiceId int;
DECLARE dashboardServiceId int;

DECLARE snrBuyerPermissionId int;
DECLARE jnrBuyerPermissionId int;
DECLARE snrSupplierPermissionId int;
DECLARE jnrSupplierPermissionId int;
DECLARE snrAdminPermissionId int;
DECLARE jnrAdminPermissionId int;
DECLARE dbElAccessPermissionId int;

DECLARE snrBuyerRoleId int;
DECLARE jnrBuyerRoleId int;
DECLARE snrSupplierRoleId int;
DECLARE jnrSupplierRoleId int;
DECLARE snrAdminRoleId int;
DECLARE jnrAdminRoleId int;
DECLARE dbAccessELRoleId int;

BEGIN

DELETE FROM public."CcsService"
	WHERE "ServiceName" = serviceName;

-- If required to delete uncomment following
--DELETE FROM public."CcsAccessRole"
	--WHERE "CcsAccessRoleNameKey"='EL_ADMIN' or "CcsAccessRoleNameKey"='EL_User' or "CcsAccessRoleNameKey"='ACCESS_EVIDENCE_LOCKER';

INSERT INTO public."CcsService"(
	"ServiceName", "TimeOutLength", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc",
	"IsDeleted", "ServiceClientId", "ServiceUrl", "Description", "ServiceCode", "GlobalLevelOrganisationAccess", "ActivateOrganisations")
	VALUES (serviceName, 0, 0, 0, now(), now(), false, clientId, 
			clientUrl, serviceDescription, serviceCode, false, false);
			
			
SELECT "Id" into elServiceId From public."CcsService" WHERE "ServiceName" = serviceName LIMIT 1;
SELECT "Id" into dashboardServiceId From public."CcsService" WHERE "ServiceName" = 'Dashboard Service' LIMIT 1;	


INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('EL_SNR_BUYER', elServiceId, 0, 0, now(), now(), false);
SELECT "Id" into snrBuyerPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'EL_SNR_BUYER' AND "CcsServiceId" = elServiceId LIMIT 1;				

INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('EL_JNR_BUYER', elServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into jnrBuyerPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'EL_JNR_BUYER' AND "CcsServiceId" = elServiceId LIMIT 1;

INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('EL_SNR_SUPPLIER', elServiceId, 0, 0, now(), now(), false);
SELECT "Id" into snrSupplierPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'EL_SNR_SUPPLIER' AND "CcsServiceId" = elServiceId LIMIT 1;				

INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('EL_JNR_SUPPLIER', elServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into jnrSupplierPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'EL_JNR_SUPPLIER' AND "CcsServiceId" = elServiceId LIMIT 1;

INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('EL_CCS_SNR_ADMIN', elServiceId, 0, 0, now(), now(), false);
SELECT "Id" into snrAdminPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'EL_CCS_SNR_ADMIN' AND "CcsServiceId" = elServiceId LIMIT 1;				

INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('EL_CCS_JNR_ADMIN', elServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into jnrAdminPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'EL_CCS_JNR_ADMIN' AND "CcsServiceId" = elServiceId LIMIT 1;


INSERT INTO public."ServicePermission"(
	"ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
	VALUES ('ACCESS_EVIDENCE_LOCKER', dashboardServiceId, 0, 0, now(), now(), false);	
SELECT "Id" into dbElAccessPermissionId From public."ServicePermission" WHERE "ServicePermissionName" = 'ACCESS_EVIDENCE_LOCKER' LIMIT 1;



INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('EL_SNR_BUYER', 'Snr Buyer', 'Snr Buyer', 2, 1, 1, 0, 0, now(), now(), 
			false, false);
SELECT "Id" into snrBuyerRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'EL_SNR_BUYER' AND "CcsAccessRoleName" = 'Snr Buyer' LIMIT 1;

INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('EL_JNR_BUYER', 'Jnr Buyer', 'Jnr Buyer', 2, 1, 1, 0, 0, now(), now(), 
			false, false);
SELECT "Id" into jnrBuyerRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'EL_JNR_BUYER' AND "CcsAccessRoleName" = 'Jnr Buyer' LIMIT 1;

INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('EL_SNR_SUPPLIER', 'Snr Supplier', 'Snr Supplier', 2, 0, 2, 0, 0, now(), now(), 
			false, false);
SELECT "Id" into snrSupplierRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'EL_SNR_SUPPLIER' AND "CcsAccessRoleName" = 'Snr Supplier' LIMIT 1;

INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('EL_JNR_SUPPLIER', 'Jnr Supplier', 'Jnr Supplier', 2, 0, 2, 0, 0, now(), now(), 
			false, false);
SELECT "Id" into jnrSupplierRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'EL_JNR_SUPPLIER' AND "CcsAccessRoleName" = 'Jnr Supplier' LIMIT 1;

INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('EL_CCS_SNR_ADMIN', 'CCS Snr Admin', 'CCS Snr Admin', 0, 1, 2, 0, 0, now(), now(), 
			false, false);
SELECT "Id" into snrAdminRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'EL_CCS_SNR_ADMIN' AND "CcsAccessRoleName" = 'CCS Snr Admin' LIMIT 1;

INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('EL_CCS_JNR_ADMIN', 'CCS Jnr Admin', 'CCS Jnr Admin', 0, 1, 2, 0, 0, now(), now(), 
			false, false);
SELECT "Id" into jnrAdminRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'EL_CCS_JNR_ADMIN' AND "CcsAccessRoleName" = 'CCS Jnr Admin' LIMIT 1;

			
INSERT INTO public."CcsAccessRole"(
	"CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", 
	"SubscriptionTypeEligibility", "TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", 
	"LastUpdatedOnUtc", "IsDeleted", "MfaEnabled")
	VALUES ('ACCESS_EVIDENCE_LOCKER', 'Access Evidence Locker', 'Access Evidence Locker', 2, 0, 2, 0, 0, now(), now(), 
			false, false);			
SELECT "Id" into dbAccessELRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ACCESS_EVIDENCE_LOCKER' LIMIT 1;



INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (snrBuyerPermissionId, snrBuyerRoleId, 0, 0, now(), now(), false);

INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (jnrBuyerPermissionId, jnrBuyerRoleId, 0, 0, now(), now(), false);

INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (snrSupplierPermissionId, snrSupplierRoleId, 0, 0, now(), now(), false);
	
INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (jnrSupplierPermissionId, jnrSupplierRoleId, 0, 0, now(), now(), false);

INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (snrAdminPermissionId, snrAdminRoleId, 0, 0, now(), now(), false);
	
INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (jnrAdminPermissionId, jnrAdminRoleId, 0, 0, now(), now(), false);

INSERT INTO public."ServiceRolePermission"(
	"ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (dbElAccessPermissionId, dbAccessELRoleId, 0, 0, now(), now(), false);

	RETURN 1;
	END;
$$ LANGUAGE plpgsql;

SELECT setval('"CcsAccessRole_Id_seq"', max("Id")) FROM "CcsAccessRole";
SELECT setval('"CcsService_Id_seq"', max("Id")) FROM "CcsService";
SELECT setval('"ServicePermission_Id_seq"', max("Id")) FROM "ServicePermission";
SELECT setval('"ServiceRolePermission_Id_seq"', max("Id")) FROM "ServiceRolePermission";
SELECT create_el_service();
DROP FUNCTION create_el_service;
