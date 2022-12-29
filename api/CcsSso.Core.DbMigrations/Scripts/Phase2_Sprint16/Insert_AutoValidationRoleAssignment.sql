CREATE OR REPLACE FUNCTION Insert_AutoValidationRole() RETURNS integer AS $$
DECLARE defaultUserId int;
DECLARE orgAdminId int;
-- eSourcing
DECLARE eSourcingSupplierId int;
DECLARE eSourcingBuyerId int;
DECLARE eSourcingDashboardId int;
DECLARE eSourcingServiceId int;

DECLARE snrSupplierId int;
DECLARE jnrSupplierId int;
DECLARE buyerSupplierInfoId int;
DECLARE casDashboardId int;
DECLARE casServiceId int;
DECLARE fleetDashboardId int;
DECLARE fleetUserId int;

BEGIN
	--Organisation User
	SELECT "Id" into defaultUserId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ORG_DEFAULT_USER' AND "IsDeleted" = false LIMIT 1;
	
	INSERT INTO public."AutoValidationRole"("CcsAccessRoleId", "IsSupplier", "IsBuyerSuccess", "IsBuyerFailed", "IsBothSuccess", "IsBothFailed", "AssignToOrg", "AssignToAdmin")
	VALUES (defaultUserId, true, true, true, true, true, true, true);

	--Organisation Administrator
	SELECT "Id" into orgAdminId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ORG_ADMINISTRATOR' AND "IsDeleted" = false LIMIT 1;
	
	INSERT INTO public."AutoValidationRole"("CcsAccessRoleId", "IsSupplier", "IsBuyerSuccess", "IsBuyerFailed", "IsBothSuccess", "IsBothFailed", "AssignToOrg", "AssignToAdmin")
	VALUES (orgAdminId, true, true, true, true, true, true, true);

	--esorucing service as a supplier
	SELECT "Id" into eSourcingSupplierId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'JAEGGER_SUPPLIER' AND "IsDeleted" = false LIMIT 1;
	
	INSERT INTO public."AutoValidationRole"("CcsAccessRoleId", "IsSupplier", "IsBuyerSuccess", "IsBuyerFailed", "IsBothSuccess", "IsBothFailed", "AssignToOrg", "AssignToAdmin")
	VALUES (eSourcingSupplierId,true, false, false, true, true, true, false);

	--esorucing service as buyer
	SELECT "Id" into eSourcingBuyerId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'JAEGGER_BUYER' AND "IsDeleted" = false LIMIT 1;
	
	INSERT INTO public."AutoValidationRole"("CcsAccessRoleId", "IsSupplier", "IsBuyerSuccess", "IsBuyerFailed", "IsBothSuccess", "IsBothFailed", "AssignToOrg", "AssignToAdmin")
	VALUES (eSourcingBuyerId, false, true, false, true, false, true, false);

	--esorucing service - add to dasboard
	SELECT "Id" into eSourcingDashboardId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ACCESS_JAGGAER' AND "IsDeleted" = false LIMIT 1;
	
	INSERT INTO public."AutoValidationRole"("CcsAccessRoleId", "IsSupplier", "IsBuyerSuccess", "IsBuyerFailed", "IsBothSuccess", "IsBothFailed", "AssignToOrg", "AssignToAdmin")
	VALUES (eSourcingDashboardId, true, true, false, true, true, true, false);

	--eSourcing Service - add service
	SELECT "Id" into eSourcingServiceId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'JAGGAER_USER' AND "IsDeleted" = false LIMIT 1;
	
	INSERT INTO public."AutoValidationRole"("CcsAccessRoleId", "IsSupplier", "IsBuyerSuccess", "IsBuyerFailed", "IsBothSuccess", "IsBothFailed", "AssignToOrg", "AssignToAdmin")
	VALUES (eSourcingServiceId, true, true, false, true, true, true, false);

	--Snr Supplier - Buyer/Supplier Information
	SELECT "Id" into snrSupplierId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'EL_SNR_SUPPLIER' AND "IsDeleted" = false LIMIT 1;
	
	INSERT INTO public."AutoValidationRole"("CcsAccessRoleId", "IsSupplier", "IsBuyerSuccess", "IsBuyerFailed", "IsBothSuccess", "IsBothFailed", "AssignToOrg", "AssignToAdmin")
	VALUES (snrSupplierId, false, false, false, false, false, true, false);

	--Jnr Supplier - Buyer/Supplier Information
	SELECT "Id" into jnrSupplierId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'EL_JNR_SUPPLIER' AND "IsDeleted" = false LIMIT 1;
	
	INSERT INTO public."AutoValidationRole"("CcsAccessRoleId", "IsSupplier", "IsBuyerSuccess", "IsBuyerFailed", "IsBothSuccess", "IsBothFailed", "AssignToOrg", "AssignToAdmin")
	VALUES (jnrSupplierId, false, false, false, false, false, true, false);

	--Buyer Supplier Information - Dashboard Service
	SELECT "Id" into buyerSupplierInfoId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ACCESS_EVIDENCE_LOCKER' AND "IsDeleted" = false LIMIT 1;
	
	INSERT INTO public."AutoValidationRole"("CcsAccessRoleId", "IsSupplier", "IsBuyerSuccess", "IsBuyerFailed", "IsBothSuccess", "IsBothFailed", "AssignToOrg", "AssignToAdmin")
	VALUES (buyerSupplierInfoId, false, false, false, false, false, true, false);

	--Contract Award Service (CAS) - add to dashboard
	SELECT "Id" into casDashboardId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ACCESS_CAAAC_CLIENT' AND "IsDeleted" = false LIMIT 1;
	
	INSERT INTO public."AutoValidationRole"("CcsAccessRoleId", "IsSupplier", "IsBuyerSuccess", "IsBuyerFailed", "IsBothSuccess", "IsBothFailed", "AssignToOrg", "AssignToAdmin")
	VALUES (casDashboardId, false, true, false, true, false, true, true);
	
	--Contract Award Service (CAS) - add service
	SELECT "Id" into casServiceId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'CAT_USER' AND "IsDeleted" = false LIMIT 1;
	
	INSERT INTO public."AutoValidationRole"("CcsAccessRoleId", "IsSupplier", "IsBuyerSuccess", "IsBuyerFailed", "IsBothSuccess", "IsBothFailed", "AssignToOrg", "AssignToAdmin")
	VALUES (casServiceId, false, true, false, true, false, true, true);

	--Access Fleet Portal - Dashboard Service
	SELECT "Id" into fleetDashboardId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ACCESS_FP_CLIENT' AND "IsDeleted" = false LIMIT 1;
	
	INSERT INTO public."AutoValidationRole"("CcsAccessRoleId", "IsSupplier", "IsBuyerSuccess", "IsBuyerFailed", "IsBothSuccess", "IsBothFailed", "AssignToOrg", "AssignToAdmin")
	VALUES (fleetDashboardId, false, true, false, true, false, true, true);

	--Fleet Portal User - Fleet Portal
	SELECT "Id" into fleetUserId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'FP_USER' AND "IsDeleted" = false LIMIT 1;
	
	INSERT INTO public."AutoValidationRole"("CcsAccessRoleId", "IsSupplier", "IsBuyerSuccess", "IsBuyerFailed", "IsBothSuccess", "IsBothFailed", "AssignToOrg", "AssignToAdmin")
	VALUES (fleetUserId, false, true, false, true, false, true, true);


RETURN 1;
END;

$$ LANGUAGE plpgsql;
SELECT Insert_AutoValidationRole();
DROP FUNCTION Insert_AutoValidationRole;
