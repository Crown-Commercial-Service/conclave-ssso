CREATE OR REPLACE FUNCTION Rename_Roles() RETURNS integer AS $$
BEGIN

	----eSourcing service as a supplier
	UPDATE public."OrganisationAuditEvent" e 
		SET "Roles" = REPLACE("Roles",r."CcsAccessRoleName",'eSourcing Service as a supplier')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'JAEGGER_SUPPLIER' AND e."Roles" like '' || r."CcsAccessRoleName" || '';
            
	UPDATE public."OrganisationAuditEvent" e
		SET "Roles" = REPLACE("Roles",concat(r."CcsAccessRoleName",','),'eSourcing Service as a supplier,')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'JAEGGER_SUPPLIER' AND 
	e."Roles" like '' || r."CcsAccessRoleName" || ',%';
            
	UPDATE public."OrganisationAuditEvent" e
			SET "Roles" = REPLACE("Roles", concat(',',r."CcsAccessRoleName",','),',eSourcing Service as a supplier,')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'JAEGGER_SUPPLIER' AND e."Roles" like '%,' || r."CcsAccessRoleName" || ',%';
            
	UPDATE public."OrganisationAuditEvent" e
		SET "Roles" = REPLACE("Roles",concat(',',r."CcsAccessRoleName"),',eSourcing Service as a supplier')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'JAEGGER_SUPPLIER' AND e."Roles" like '%,' || r."CcsAccessRoleName" || '';

	----eSourcing service as buyer
	UPDATE public."OrganisationAuditEvent" e 
		SET "Roles" = REPLACE("Roles",r."CcsAccessRoleName",'eSourcing Service as a buyer')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'JAEGGER_BUYER' AND e."Roles" like '' || r."CcsAccessRoleName" || '';
            
	UPDATE public."OrganisationAuditEvent" e
		SET "Roles" = REPLACE("Roles",concat(r."CcsAccessRoleName",','),'eSourcing Service as a buyer,')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'JAEGGER_BUYER' AND 
	e."Roles" like '' || r."CcsAccessRoleName" || ',%';
            
	UPDATE public."OrganisationAuditEvent" e
			SET "Roles" = REPLACE("Roles", concat(',',r."CcsAccessRoleName",','),',eSourcing Service as a buyer,')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'JAEGGER_BUYER' AND e."Roles" like '%,' || r."CcsAccessRoleName" || ',%';
            
	UPDATE public."OrganisationAuditEvent" e
		SET "Roles" = REPLACE("Roles",concat(',',r."CcsAccessRoleName"),',eSourcing Service as a buyer')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'JAEGGER_BUYER' AND e."Roles" like '%,' || r."CcsAccessRoleName" || '';

	----eSourcing service - add to dashboard
	UPDATE public."OrganisationAuditEvent" e 
		SET "Roles" = REPLACE("Roles",r."CcsAccessRoleName",'eSourcing Service - add to dashboard')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'ACCESS_JAGGAER' AND e."Roles" like '' || r."CcsAccessRoleName" || '';
            
	UPDATE public."OrganisationAuditEvent" e
		SET "Roles" = REPLACE("Roles",concat(r."CcsAccessRoleName",','),'eSourcing Service - add to dashboard,')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'ACCESS_JAGGAER' AND 
	e."Roles" like '' || r."CcsAccessRoleName" || ',%';
            
	UPDATE public."OrganisationAuditEvent" e
			SET "Roles" = REPLACE("Roles", concat(',',r."CcsAccessRoleName",','),',eSourcing Service - add to dashboard,')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'ACCESS_JAGGAER' AND e."Roles" like '%,' || r."CcsAccessRoleName" || ',%';
            
	UPDATE public."OrganisationAuditEvent" e
		SET "Roles" = REPLACE("Roles",concat(',',r."CcsAccessRoleName"),',eSourcing Service - add to dashboard')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'ACCESS_JAGGAER' AND e."Roles" like '%,' || r."CcsAccessRoleName" || '';

	----eSourcing Service - add service
	UPDATE public."OrganisationAuditEvent" e 
		SET "Roles" = REPLACE("Roles",r."CcsAccessRoleName",'eSourcing Service - add service')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'JAGGAER_USER' AND e."Roles" like '' || r."CcsAccessRoleName" || '';
            
	UPDATE public."OrganisationAuditEvent" e
		SET "Roles" = REPLACE("Roles",concat(r."CcsAccessRoleName",','),'eSourcing Service - add service,')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'JAGGAER_USER' AND 
	e."Roles" like '' || r."CcsAccessRoleName" || ',%';
            
	UPDATE public."OrganisationAuditEvent" e
			SET "Roles" = REPLACE("Roles", concat(',',r."CcsAccessRoleName",','),',eSourcing Service - add service,')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'JAGGAER_USER' AND e."Roles" like '%,' || r."CcsAccessRoleName" || ',%';
            
	UPDATE public."OrganisationAuditEvent" e
		SET "Roles" = REPLACE("Roles",concat(',',r."CcsAccessRoleName"),',eSourcing Service - add service')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'JAGGAER_USER' AND e."Roles" like '%,' || r."CcsAccessRoleName" || '';

	----Contract Award Service (CAS) - add service
	UPDATE public."OrganisationAuditEvent" e 
		SET "Roles" = REPLACE("Roles",r."CcsAccessRoleName",'Contract Award Service (CAS) - add service')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'CAT_USER' AND e."Roles" like '' || r."CcsAccessRoleName" || '';
            
	UPDATE public."OrganisationAuditEvent" e
		SET "Roles" = REPLACE("Roles",concat(r."CcsAccessRoleName",','),'Contract Award Service (CAS) - add service,')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'CAT_USER' AND 
	e."Roles" like '' || r."CcsAccessRoleName" || ',%';
            
	UPDATE public."OrganisationAuditEvent" e
			SET "Roles" = REPLACE("Roles", concat(',',r."CcsAccessRoleName",','),',Contract Award Service (CAS) - add service,')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'CAT_USER' AND e."Roles" like '%,' || r."CcsAccessRoleName" || ',%';
            
	UPDATE public."OrganisationAuditEvent" e
		SET "Roles" = REPLACE("Roles",concat(',',r."CcsAccessRoleName"),',Contract Award Service (CAS) - add service')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'CAT_USER' AND e."Roles" like '%,' || r."CcsAccessRoleName" || '';

	----Contract Award Service (CAS) - add to dashboard
	UPDATE public."OrganisationAuditEvent" e 
		SET "Roles" = REPLACE("Roles",r."CcsAccessRoleName",'Contract Award Service (CAS) - add to dashboard')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'ACCESS_CAAAC_CLIENT' AND e."Roles" like '' || r."CcsAccessRoleName" || '';
            
	UPDATE public."OrganisationAuditEvent" e
		SET "Roles" = REPLACE("Roles",concat(r."CcsAccessRoleName",','),'Contract Award Service (CAS) - add to dashboard,')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'ACCESS_CAAAC_CLIENT' AND 
	e."Roles" like '' || r."CcsAccessRoleName" || ',%';
            
	UPDATE public."OrganisationAuditEvent" e
			SET "Roles" = REPLACE("Roles", concat(',',r."CcsAccessRoleName",','),',Contract Award Service (CAS) - add to dashboard,')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'ACCESS_CAAAC_CLIENT' AND e."Roles" like '%,' || r."CcsAccessRoleName" || ',%';
            
	UPDATE public."OrganisationAuditEvent" e
		SET "Roles" = REPLACE("Roles",concat(',',r."CcsAccessRoleName"),',Contract Award Service (CAS) - add to dashboard')
	FROM public."CcsAccessRole" AS r
	WHERE r."CcsAccessRoleNameKey" = 'ACCESS_CAAAC_CLIENT' AND e."Roles" like '%,' || r."CcsAccessRoleName" || '';

------------Role Name Update-------------

	--eSourcing service as a supplier
	UPDATE public."CcsAccessRole" SET "CcsAccessRoleName" = 'eSourcing Service as a supplier',	"CcsAccessRoleDescription" = 'eSourcing Service as a supplier' WHERE "CcsAccessRoleNameKey" = 'JAEGGER_SUPPLIER';
	
	--eSourcing service as buyer
	UPDATE public."CcsAccessRole" SET "CcsAccessRoleName" = 'eSourcing Service as a buyer',	"CcsAccessRoleDescription" = 'eSourcing Service as a buyer' WHERE "CcsAccessRoleNameKey" = 'JAEGGER_BUYER';
	
	--eSourcing service - add to dashboard
	UPDATE public."CcsAccessRole" SET "CcsAccessRoleName" = 'eSourcing Service - add to dashboard',	"CcsAccessRoleDescription" = 'eSourcing Service - add to dashboard' WHERE "CcsAccessRoleNameKey" = 'ACCESS_JAGGAER';
	
	--eSourcing Service - add service
	UPDATE public."CcsAccessRole" SET "CcsAccessRoleName" = 'eSourcing Service - add service',	"CcsAccessRoleDescription" = 'eSourcing Service - add service' WHERE "CcsAccessRoleNameKey" = 'JAGGAER_USER';

	--Contract Award Service (CAS) - add service
	UPDATE public."CcsAccessRole" SET "CcsAccessRoleName" = 'Contract Award Service (CAS) - add service',	"CcsAccessRoleDescription" = 'Contract Award Service (CAS) - add service' WHERE "CcsAccessRoleNameKey" = 'CAT_USER';
	
	--Contract Award Service (CAS) - add to dashboard
	UPDATE public."CcsAccessRole" SET "CcsAccessRoleName" = 'Contract Award Service (CAS) - add to dashboard',	"CcsAccessRoleDescription" = 'Contract Award Service (CAS) - add to dashboard' WHERE "CcsAccessRoleNameKey" = 'ACCESS_CAAAC_CLIENT';
	
RETURN 1;
END;

$$ LANGUAGE plpgsql;
SELECT Rename_Roles();
DROP FUNCTION Rename_Roles;
