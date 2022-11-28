CREATE OR REPLACE FUNCTION Update_AutoValidationRoleAssignment() RETURNS integer AS $$

DECLARE snrSupplierId int;
DECLARE jnrSupplierId int;
DECLARE buyerSupplierInfoId int;

BEGIN
	--Snr Supplier - Buyer/Supplier Information
	SELECT "Id" into snrSupplierId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'EL_SNR_SUPPLIER' LIMIT 1;

	UPDATE public."AutoValidationRole"
	SET "IsSupplier" = true, 
	"IsBuyerSuccess" = false, 
	"IsBuyerFailed" = false, 
	"IsBothSuccess" = true, 
	"IsBothFailed" = false, 
	"AssignToOrg" = false, 
	"AssignToAdmin" = false
	WHERE "CcsAccessRoleId" = snrSupplierId;

	--Jnr Supplier - Buyer/Supplier Information
	SELECT "Id" into jnrSupplierId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'EL_JNR_SUPPLIER' LIMIT 1;

	UPDATE public."AutoValidationRole"
	SET "IsSupplier" = true, 
	"IsBuyerSuccess" = false, 
	"IsBuyerFailed" = false, 
	"IsBothSuccess" = true, 
	"IsBothFailed" = false, 
	"AssignToOrg" = false, 
	"AssignToAdmin" = false
	WHERE "CcsAccessRoleId" = jnrSupplierId;

	--Buyer Supplier Information - Dashboard Service
	SELECT "Id" into buyerSupplierInfoId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ACCESS_EVIDENCE_LOCKER' LIMIT 1;

	UPDATE public."AutoValidationRole"
	SET "IsSupplier" = true, 
	"IsBuyerSuccess" = true, 
	"IsBuyerFailed" = false, 
	"IsBothSuccess" = true, 
	"IsBothFailed" = false, 
	"AssignToOrg" = false, 
	"AssignToAdmin" = false
	WHERE "CcsAccessRoleId" = buyerSupplierInfoId;

RETURN 1;
END;

$$ LANGUAGE plpgsql;
SELECT Update_AutoValidationRoleAssignment();
DROP FUNCTION Update_AutoValidationRoleAssignment;
