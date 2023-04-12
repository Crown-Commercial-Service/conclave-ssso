CREATE OR REPLACE FUNCTION AddAutoValidationRoleMapping(
		CcsAccessRoleNameKey varchar(200),
		CcsAccessRoleName text,
		CurrentCcsAccessRoleNameKey varchar(200),
		CurrentCcsAccessRoleName varchar(200)
	) RETURNS integer AS $$

DECLARE currentRoleId int;
DECLARE newRoleId int;

begin

select "Id" into currentRoleId from "CcsAccessRole" where "CcsAccessRoleNameKey"=CurrentCcsAccessRoleNameKey and "CcsAccessRoleName"=CurrentCcsAccessRoleName limit 1;
select "Id" into newRoleId from "CcsAccessRole" where "CcsAccessRoleNameKey"=CcsAccessRoleNameKey and "CcsAccessRoleName"=CcsAccessRoleName limit 1;

if (currentRoleId is null or newRoleId is null) then
	raise notice 'New Role Key %',CcsAccessRoleNameKey;
	raise notice 'New Role Key Name %',CcsAccessRoleName;
	raise notice 'Current Role Key %',CurrentCcsAccessRoleNameKey;
	raise notice 'Current Role Key Name %',CurrentCcsAccessRoleName;
	raise notice 'No Old Role or New Role Id  found';
	return 1;
end if; 


		  IF NOT EXISTS (SELECT "Id" FROM public."AutoValidationRole" WHERE "CcsAccessRoleId" = newRoleId LIMIT 1) THEN

				raise notice 'New Role Key %',CcsAccessRoleNameKey;
				raise notice 'New Role Key Name %',CcsAccessRoleName;
				raise notice 'Adding to Auto validation Matrix';

		INSERT INTO public."AutoValidationRole"("CcsAccessRoleId", "IsSupplier", "IsBuyerSuccess", "IsBuyerFailed", "IsBothSuccess", "IsBothFailed", "AssignToOrg", "AssignToAdmin")
					               (select newRoleId, "IsSupplier", "IsBuyerSuccess", "IsBuyerFailed", "IsBothSuccess", "IsBothFailed", "AssignToOrg", "AssignToAdmin" 
									from public."AutoValidationRole" where "CcsAccessRoleId"= currentRoleId);
   		END IF;


	RETURN 1;
	END;
$$ LANGUAGE plpgsql;



SELECT AddAutoValidationRoleMapping('CAT_USER','Contract Award Service (CAS) - add to dashboard','CAT_USER','Contract Award Service (CAS) - add service');
SELECT AddAutoValidationRoleMapping('CAT_USER','Contract Award Service role to create buyer in Jagger','CAT_USER','Contract Award Service (CAS) - add service');
SELECT AddAutoValidationRoleMapping('CAT_USER','Contract Award Service role to merge buyer via Jaggaer','CAT_USER','Contract Award Service (CAS) - add service');

SELECT AddAutoValidationRoleMapping('JAEGGER_BUYER','eSourcing Tile for Buyer User','JAEGGER_BUYER','eSourcing Service as a buyer');
-- No need to add it to the auto validation.
--SELECT AddAutoValidationRoleMapping('JAEGGER_BUYER','eSourcing  buyer role for CAS -Optional','JAEGGER_BUYER','eSourcing Service as a buyer');
SELECT AddAutoValidationRoleMapping('JAEGGER_BUYER','eSourcing buyer role to access Jagger','JAEGGER_BUYER','eSourcing Service as a buyer');

SELECT AddAutoValidationRoleMapping('JAEGGER_SUPPLIER','eSourcing Tile for Supplier User','JAEGGER_SUPPLIER','eSourcing Service as a supplier');
SELECT AddAutoValidationRoleMapping('JAEGGER_SUPPLIER','eSourcing  Supplier role for CAS for QA Pages Access','JAEGGER_SUPPLIER','eSourcing Service as a supplier');
SELECT AddAutoValidationRoleMapping('JAEGGER_SUPPLIER','eSourcing  Supplier role to access Jagger','JAEGGER_SUPPLIER','eSourcing Service as a supplier');

SELECT AddAutoValidationRoleMapping('FP_USER','Fleet Portal Tile','FP_USER','Fleet Portal User');



DROP FUNCTION AddAutoValidationRoleMapping;