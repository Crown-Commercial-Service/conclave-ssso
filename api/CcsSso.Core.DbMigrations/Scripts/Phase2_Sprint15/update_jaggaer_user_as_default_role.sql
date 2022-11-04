CREATE OR REPLACE FUNCTION update_jaggaer_user_as_default_role() RETURNS integer AS $$
DECLARE serviceName text = 'eSourcing';
DECLARE serviceDescription text = 'The eSourcing tool will help you supply to, or buy for, the public sector, compliantly';
DECLARE serviceCode text = 'JAGGAER';
DECLARE clientUrl text = 'https://crowncommercialservice-prep.bravosolution.co.uk/esop/guest/ssoRequest.do';
DECLARE clientId text = '';
DECLARE roleNameKey text = 'JAGGAER_USER';
DECLARE clientServiceId int;
DECLARE clientUserRoleId int;
DECLARE clientuserpermissionid int;
BEGIN

IF NOT EXISTS (SELECT "Id" FROM public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = roleNameKey LIMIT 1) THEN
    
    IF NOT EXISTS (SELECT "Id" From public."CcsService" WHERE "ServiceName" = serviceName LIMIT 1) THEN
        INSERT INTO public."CcsService"( "ServiceName", "TimeOutLength", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc",
										 "IsDeleted", "ServiceClientId", "ServiceUrl", "Description", "ServiceCode", "GlobalLevelOrganisationAccess", "ActivateOrganisations")
        VALUES (serviceName, 0, 0, 0, now(), now(), false, clientId, clientUrl, serviceDescription, serviceCode, false, false);
    END IF;
    
    SELECT "Id" into clientServiceId From public."CcsService" WHERE "ServiceName" = serviceName LIMIT 1;    
        
	IF NOT EXISTS (SELECT "Id" FROM public."ServicePermission" WHERE "ServicePermissionName" = roleNameKey AND "CcsServiceId" = clientServiceId LIMIT 1) THEN
		INSERT INTO public."ServicePermission"("ServicePermissionName", "CcsServiceId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc","LastUpdatedOnUtc", "IsDeleted")
		VALUES (roleNameKey, clientServiceId, 0, 0, now(), now(), false);	
	END IF;
	
	SELECT "Id" into clientUserPermissionId FROM public."ServicePermission" WHERE "ServicePermissionName" = roleNameKey AND "CcsServiceId" = clientServiceId LIMIT 1;

	IF NOT EXISTS (SELECT "Id" FROM public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = roleNameKey LIMIT 1) THEN
		INSERT INTO public."CcsAccessRole"("CcsAccessRoleNameKey", "CcsAccessRoleName", "CcsAccessRoleDescription", "OrgTypeEligibility", "SubscriptionTypeEligibility", 
							"TradeEligibility", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "MfaEnabled", "DefaultEligibility")
		VALUES (roleNameKey, 'Jaggaer User', 'Jaggaer User', 2, 0, 2, 0, 0, now(), now(), false, false, '000');
	END IF;
	
	SELECT "Id" into clientUserRoleId FROM public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = roleNameKey LIMIT 1;

    IF NOT EXISTS (SELECT "Id" From public."ServiceRolePermission" WHERE "ServicePermissionId" = clientUserPermissionId AND "CcsAccessRoleId" = clientUserRoleId  LIMIT 1) THEN
        INSERT INTO public."ServiceRolePermission"("ServicePermissionId", "CcsAccessRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
        VALUES (clientUserPermissionId, clientUserRoleId, 0, 0, now(), now(), false);
    END IF;

RETURN 1;
ELSE
	UPDATE public."CcsAccessRole" SET "OrgTypeEligibility" = 2, "SubscriptionTypeEligibility" = 0, "TradeEligibility" = 2 
	WHERE "CcsAccessRoleNameKey" = roleNameKey;
RETURN 2;
END IF;

END;
$$ LANGUAGE plpgsql;
SELECT setval('"CcsService_Id_seq"', max("Id")) FROM "CcsService";
SELECT setval('"ServicePermission_Id_seq"', max("Id")) FROM "ServicePermission";
SELECT setval('"ServiceRolePermission_Id_seq"', max("Id")) FROM "ServiceRolePermission";
SELECT update_jaggaer_user_as_default_role();
DROP FUNCTION update_jaggaer_user_as_default_role;