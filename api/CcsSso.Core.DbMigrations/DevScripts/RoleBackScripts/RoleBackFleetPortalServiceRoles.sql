CREATE OR REPLACE FUNCTION drop_flp_service() RETURNS integer AS $$

-- Add values to clientId and clientUrl
DECLARE serviceName text = 'Fleet Portal';
DECLARE dashboardAccessRoleKey text = 'ACCESS_FP_CLIENT';
DECLARE serviceRoleKey1 text = 'FP_USER';

BEGIN

	DELETE FROM public."CcsService"
	WHERE "ServiceName" = serviceName;

	DELETE FROM public."CcsAccessRole"
	WHERE "CcsAccessRoleNameKey" = dashboardAccessRoleKey;

	DELETE FROM public."CcsAccessRole"
	WHERE "CcsAccessRoleNameKey" = serviceRoleKey1;

	RETURN 1;
	END;
$$ LANGUAGE plpgsql;


SELECT drop_flp_service();
DROP FUNCTION drop_flp_service;
SELECT setval('"CcsAccessRole_Id_seq"', max("Id")) FROM "CcsAccessRole";
SELECT setval('"CcsService_Id_seq"', max("Id")) FROM "CcsService";
SELECT setval('"ServicePermission_Id_seq"', max("Id")) FROM "ServicePermission";
SELECT setval('"ServiceRolePermission_Id_seq"', max("Id")) FROM "ServiceRolePermission";
