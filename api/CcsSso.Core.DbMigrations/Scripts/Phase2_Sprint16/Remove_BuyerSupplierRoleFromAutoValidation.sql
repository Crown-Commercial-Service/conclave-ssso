CREATE OR REPLACE FUNCTION Remove_BuyerSupplierRoleFromAutoValidation() RETURNS integer AS $$
DECLARE buyerSupplierInfoId int;
BEGIN
	--Buyer Supplier Information - Dashboard Service
	SELECT "Id" into buyerSupplierInfoId From public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'ACCESS_EVIDENCE_LOCKER' LIMIT 1;
	
	DELETE FROM public."AutoValidationRole" WHERE "CcsAccessRoleId" = buyerSupplierInfoId;

RETURN 1;
END;

$$ LANGUAGE plpgsql;
SELECT Remove_BuyerSupplierRoleFromAutoValidation();
DROP FUNCTION Remove_BuyerSupplierRoleFromAutoValidation;
