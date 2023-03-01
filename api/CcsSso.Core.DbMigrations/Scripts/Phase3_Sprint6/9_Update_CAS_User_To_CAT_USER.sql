START TRANSACTION;

UPDATE "CcsAccessRole" set "CcsAccessRoleNameKey"='CAT_USER' WHERE "CcsAccessRoleNameKey" in ('CAS_USER');


UPDATE "CcsAccessRole" 
	set "CcsAccessRoleName"='Contract Award Service role to create buyer in Jagger',
	"CcsAccessRoleDescription"='Contract Award Service role to create buyer in Jagger'
	
	WHERE "CcsAccessRoleNameKey" ='CAT_USER' AND "CcsAccessRoleName"='Contract Award Service role to create buyer in Jagger-LD';

COMMIT;
