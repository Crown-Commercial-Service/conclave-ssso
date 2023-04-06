
CREATE OR REPLACE FUNCTION AddGroupRoleMapping(
		CcsServiceRoleGroupKey varchar(200),
		CcsAccessRoleNameKey varchar(200),
		CcsAccessRoleName text
	) RETURNS integer AS $$

DECLARE roleId int;
DECLARE roleGroupId int;

begin

select "Id" into roleId from "CcsAccessRole" where "CcsAccessRoleNameKey"=CcsAccessRoleNameKey and "CcsAccessRoleName"=CcsAccessRoleName;
select "Id" into roleGroupId from "CcsServiceRoleGroup" where "Key"=CcsServiceRoleGroupKey;
   
if (roleId is null or roleGroupId is null) then
	raise notice 'No role or group found';
	return 1;
end if; 

	raise notice 'Adding group role mapping';

	  IF NOT EXISTS (SELECT "Id" FROM public."CcsServiceRoleMapping" WHERE "CcsServiceRoleGroupId" = roleGroupId and "CcsAccessRoleId" = roleId LIMIT 1) THEN
			INSERT INTO public."CcsServiceRoleMapping"("CcsServiceRoleGroupId", "CcsAccessRoleId")
			VALUES (roleGroupId,roleId );
   	END IF;
	

	RETURN 1;
	END;
$$ LANGUAGE plpgsql;

SELECT AddGroupRoleMapping('CAS_USER_GROUP','CAT_USER','Contract Award Service (CAS) - add service');
SELECT AddGroupRoleMapping('CAS_USER_GROUP','CAS_USER','Contract Award Service (CAS) - add to dashboard');
SELECT AddGroupRoleMapping('CAS_USER_GROUP','CAS_USER','Contract Award Service role to create buyer in Jagger-LD');
-- run the below line the if you are executing all the script at the first time - (Above TEST env)
--SELECT AddGroupRoleMapping('CAS_USER_GROUP','CAT_USER','Contract Award Service role to merge buyer via Jaggaer');

-- run the below line if the script already execuated. (TEST and lower environment.)
--SELECT AddGroupRoleMapping('CAT_USER','CAT_USER','Contract Award Service role to merge buyer via Jaggaer');


SELECT AddGroupRoleMapping('JAEGGER_BUYER_GROUP','JAEGGER_BUYER','eSourcing Service as a buyer');
SELECT AddGroupRoleMapping('JAEGGER_BUYER_GROUP','JAEGGER_BUYER','eSourcing  buyer role for CAS -Optional');
SELECT AddGroupRoleMapping('JAEGGER_BUYER_GROUP','JAEGGER_BUYER','eSourcing Tile for Buyer User');
SELECT AddGroupRoleMapping('JAEGGER_BUYER_GROUP','JAEGGER_BUYER','eSourcing buyer role to access Jagger');

SELECT AddGroupRoleMapping('JAEGGER_SUPPLIER_GROUP','JAEGGER_SUPPLIER','eSourcing Service as a supplier');
SELECT AddGroupRoleMapping('JAEGGER_SUPPLIER_GROUP','JAEGGER_SUPPLIER','eSourcing  Supplier role for CAS for QA Pages Access');
SELECT AddGroupRoleMapping('JAEGGER_SUPPLIER_GROUP','JAEGGER_SUPPLIER','eSourcing Tile for Supplier User');
SELECT AddGroupRoleMapping('JAEGGER_SUPPLIER_GROUP','JAEGGER_SUPPLIER','eSourcing  Supplier role to access Jagger');

SELECT AddGroupRoleMapping('FP_USER_GROUP','FP_USER','Fleet Portal Tile');
SELECT AddGroupRoleMapping('FP_USER_GROUP','FP_USER','Fleet Portal User');

SELECT AddGroupRoleMapping('ORG_ADMINISTRATOR_GROUP','ORG_ADMINISTRATOR','Organisation Administrator');
SELECT AddGroupRoleMapping('ORG_DEFAULT_USER_GROUP','ORG_DEFAULT_USER','Organisation User');
SELECT AddGroupRoleMapping('ORG_USER_SUPPORT_GROUP','ORG_USER_SUPPORT','Organisation Users Support ');
SELECT AddGroupRoleMapping('MANAGE_SUBSCRIPTIONS_GROUP','MANAGE_SUBSCRIPTIONS','Manage Subscription');



DROP FUNCTION AddGroupRoleMapping;