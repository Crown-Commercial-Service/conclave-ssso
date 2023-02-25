


CREATE OR REPLACE FUNCTION AddAutoValidationRoleMapping(
		CcsAccessRoleNameKey varchar(200),
		CcsAccessRoleName text,
		OldCcsAccessRoleNameKey varchar(200)
	) RETURNS integer AS $$

DECLARE oldRoleId int;
DECLARE newRoleId int;

begin

select "Id" into oldRoleId from "CcsAccessRole" where "CcsAccessRoleNameKey"=OldCcsAccessRoleNameKey limit 1;
select "Id" into newRoleId from "CcsAccessRole" where "CcsAccessRoleNameKey"=CcsAccessRoleNameKey and "CcsAccessRoleName"=CcsAccessRoleName limit 1;

if (oldRoleId is null or newRoleId is null) then
	raise notice 'No Old Role or New Role Id  found';
	return 1;
end if; 


		  IF NOT EXISTS (SELECT "Id" FROM public."AutoValidationRole" WHERE "CcsAccessRoleId" = newRoleId LIMIT 1) THEN
				raise notice 'Adding Auto validation Matrix for Role key %',CcsAccessRoleNameKey;
		
				INSERT INTO public."AutoValidationRole"("CcsAccessRoleId", "IsSupplier", "IsBuyerSuccess", "IsBuyerFailed", "IsBothSuccess", "IsBothFailed", "AssignToOrg", "AssignToAdmin")
					               (select newRoleId, "IsSupplier", "IsBuyerSuccess", "IsBuyerFailed", "IsBothSuccess", "IsBothFailed", "AssignToOrg", "AssignToAdmin" 
									from public."AutoValidationRole" where "CcsAccessRoleId"= oldRoleId);
   		END IF;
	

	RETURN 1;
	END;
$$ LANGUAGE plpgsql;


SELECT AddAutoValidationRoleMapping('JAEGGER_BUYER','eSourcing Tile for Buyer User','ACCESS_JAGGAER');
SELECT AddAutoValidationRoleMapping('JAEGGER_BUYER','eSourcing buyer role to access Jagger','JAGGAER_USER');
SELECT AddAutoValidationRoleMapping('FP_USER','Fleet Portal Tile','ACCESS_FP_CLIENT');
SELECT AddAutoValidationRoleMapping('CAS_USER','Contract Award Service (CAS) - add to dashboard','ACCESS_CAAAC_CLIENT');

DROP FUNCTION AddAutoValidationRoleMapping;
