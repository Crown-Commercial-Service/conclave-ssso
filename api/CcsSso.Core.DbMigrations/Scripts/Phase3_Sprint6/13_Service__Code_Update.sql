DO $$
DECLARE	
	jaeggerBuyerId int;
	jaeggerSupplierId int;
BEGIN

-- Update existing service code which helps in the UI to show the service tiles. These codes are used in the service permission table. 	
UPDATE "CcsService" SET "ServiceCode"= 'TEST_SSO_CLIENT_USER_DS' WHERE "ServiceCode" ='TEST_SSO_CLIENT';
UPDATE "CcsService" SET "ServiceCode"= 'TEST_SAML_CLIENT_USER_DS' WHERE "ServiceCode" ='TEST_SAML_CLIENT';
UPDATE "CcsService" SET "ServiceCode"= 'CAT_USER_DS' WHERE "ServiceCode" ='CAAAC_CLIENT';
UPDATE "CcsService" SET "ServiceCode"= 'DATA_MIGRATION_DS' WHERE "ServiceCode" ='DM_CLIENT';
UPDATE "CcsService" SET "ServiceCode"= 'DigiTS_USER_DS' WHERE "ServiceCode" ='DIGITS_CLIENT';
UPDATE "CcsService" SET "ServiceCode"= 'EL_USER_DS' WHERE "ServiceCode" ='EVIDENCE_LOCKER';
UPDATE "CcsService" SET "ServiceCode"= 'FP_USER_DS' WHERE "ServiceCode" ='FP_CLIENT';
UPDATE "CcsService" SET "ServiceCode"= 'RMI_USER_DS' WHERE "ServiceCode" ='RMI_CLIENT';
UPDATE "CcsService" SET "ServiceCode"= null WHERE "ServiceCode" ='LOGIN_DIRECTOR_CLIENT';


	-- New service entries are added to support jaegger buyer and supplier. Which is the replacement of deleted 'JAGGAER' service
	IF NOT EXISTS (SELECT "Id" FROM public."CcsService" WHERE "ServiceCode" = 'JAEGGER_SUPPLIER_DS' LIMIT 1) THEN
		INSERT INTO public."CcsService"("ServiceName", "Description", "ServiceCode", "ServiceUrl", "ServiceClientId", "TimeOutLength", "GlobalLevelOrganisationAccess", "ActivateOrganisations", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "ConcurrencyKey")
	
		   (select 
			"ServiceName", "Description", 'JAEGGER_SUPPLIER_DS', "ServiceUrl", "ServiceClientId", "TimeOutLength", "GlobalLevelOrganisationAccess", "ActivateOrganisations", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "ConcurrencyKey"
			from public."CcsService" where "ServiceCode"= 'JAGGAER' AND "ServiceName"='eSourcing');
   	END IF;

	IF NOT EXISTS (SELECT "Id" FROM public."CcsService" WHERE "ServiceCode" = 'JAEGGER_BUYER_DS' LIMIT 1) THEN
		INSERT INTO public."CcsService"("ServiceName", "Description", "ServiceCode", "ServiceUrl", "ServiceClientId", "TimeOutLength", "GlobalLevelOrganisationAccess", "ActivateOrganisations", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "ConcurrencyKey")
	
		   (select 
			"ServiceName", "Description", 'JAEGGER_BUYER_DS', "ServiceUrl", "ServiceClientId", "TimeOutLength", "GlobalLevelOrganisationAccess", "ActivateOrganisations", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "ConcurrencyKey"
			from public."CcsService" where "ServiceCode"= 'JAGGAER' AND "ServiceName"='eSourcing');
   	END IF;

	-- This is not going to be used so it has been deleted
	UPDATE "CcsService" SET "IsDeleted"= true WHERE "ServiceCode" ='JAGGAER';

	-- this to update existing roles has the IsDeleted flag true.
	UPDATE "CcsService" SET "IsDeleted"= false WHERE "ServiceCode" ='JAEGGER_BUYER_DS';
	UPDATE "CcsService" SET "IsDeleted"= false WHERE "ServiceCode" ='JAEGGER_SUPPLIER_DS';

	-- Since the 'JAGGAER' is deleted, its references are updated in the service permission table using new entries (JAEGGER_BUYER_ES,JAEGGER_SUPPLIER_ES)
	
	SELECT "Id" into jaeggerBuyerId From public."CcsService" WHERE "ServiceCode" ='JAEGGER_BUYER_DS' AND "ServiceName"='eSourcing' LIMIT 1;
	SELECT "Id" into jaeggerSupplierId From public."CcsService" WHERE "ServiceCode" ='JAEGGER_SUPPLIER_DS' AND "ServiceName"='eSourcing' LIMIT 1;
	
	UPDATE "ServicePermission" SET "CcsServiceId"= jaeggerBuyerId WHERE "ServicePermissionName"='JAEGGER_BUYER_ES';
	UPDATE "ServicePermission" SET "CcsServiceId"= jaeggerSupplierId WHERE "ServicePermissionName"='JAEGGER_SUPPLIER_ES';

	update "ServicePermission" set "ServicePermissionName" = 'CAT_USER_DS' where "ServicePermissionName"='CAS_USER_DS';
	update "ServicePermission" set "ServicePermissionName" = 'CAT_USER_LD' where "ServicePermissionName"='CAS_USER_LD';

	UPDATE "CcsService" SET "Description"='A self-service system where customers can obtain live pricing quotes for either purchasing or leasing standard build cars and light commercial vehicles.' 
	WHERE "ServiceCode" ='FP_USER_DS';

	UPDATE "CcsService" SET "Description"='The eSourcing tool will help you supply to, or buy for, the public sector, compliantly.'
	WHERE "ServiceCode" ='JAEGGER_SUPPLIER_DS';

	UPDATE "CcsService" SET "Description"='The eSourcing tool will help you supply to, or buy for, the public sector, compliantly.'
	WHERE "ServiceCode" ='JAEGGER_BUYER_DS';

	UPDATE "CcsService" SET "Description"='Find and contract suitable suppliers for your procurement project, and ask them about the services they can provide. Progress to one stage further competition.' 
	WHERE "ServiceCode" ='CAT_USER_DS';


END $$
