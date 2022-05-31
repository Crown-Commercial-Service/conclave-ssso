
CREATE OR REPLACE FUNCTION jaggaer_role_update() RETURNS integer AS $$

DECLARE CURRENT_ID int;

BEGIN


SELECT "Id" INTO CURRENT_ID FROM public."CcsAccessRole" where "CcsAccessRoleNameKey"='JAGGAER_TMP';

UPDATE public."CcsAccessRole" SET "CcsAccessRoleName"='Jaggaer User',
"CcsAccessRoleDescription"='Jaggaer User',
"CcsAccessRoleNameKey"='JAGGAER_USER',
"LastUpdatedOnUtc"=Now()
WHERE "Id"=CURRENT_ID;

	RETURN 1;
	END;
$$ LANGUAGE plpgsql;

SELECT jaggaer_role_update();

DROP FUNCTION jaggaer_role_update;