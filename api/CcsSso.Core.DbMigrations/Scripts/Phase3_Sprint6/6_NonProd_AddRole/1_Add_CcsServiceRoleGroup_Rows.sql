
CREATE OR REPLACE FUNCTION AddCcsServiceRoleGroup() RETURNS integer AS $$

BEGIN

IF NOT EXISTS (SELECT "Id" FROM "CcsServiceRoleGroup" WHERE "Key" = 'EL_USER') THEN
	INSERT INTO public."CcsServiceRoleGroup"(
		"Key", "Name", "Description", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility","DisplayOrder","MfaEnabled", 
		"DefaultEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc","IsDeleted")
		VALUES ('EL_USER','Buyer Supplier Information', 'Buyer Supplier Information', 2, 1, 2,9, false, null,0,0,0,now(),now(),false);
END IF;

IF NOT EXISTS (SELECT "Id" FROM "CcsServiceRoleGroup" WHERE "Key" = 'EL_SNR_BUYER') THEN
	INSERT INTO public."CcsServiceRoleGroup"(
		"Key", "Name", "Description", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility","DisplayOrder", "MfaEnabled", 
		"DefaultEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc","IsDeleted")
		VALUES ('EL_SNR_BUYER','Snr Buyer', 'Snr Buyer', 2, 1, 1, 10,false, null,1,0,0,now(),now(),false);
END IF;


IF NOT EXISTS (SELECT "Id" FROM "CcsServiceRoleGroup" WHERE "Key" = 'EL_JNR_BUYER') THEN
	INSERT INTO public."CcsServiceRoleGroup"(
		"Key", "Name", "Description", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility","DisplayOrder", "MfaEnabled", 
		"DefaultEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc","IsDeleted")
		VALUES ('EL_JNR_BUYER','Jnr Buyer', 'Jnr Buyer', 2, 1, 1, 11,false, null,0,0,0,now(),now(),false);
END IF;

IF NOT EXISTS (SELECT "Id" FROM "CcsServiceRoleGroup" WHERE "Key" = 'EL_CCS_SNR_ADMIN') THEN
	INSERT INTO public."CcsServiceRoleGroup"(
		"Key", "Name", "Description", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility","DisplayOrder", "MfaEnabled", 
		"DefaultEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc","IsDeleted")
		VALUES ('EL_CCS_SNR_ADMIN','CCS Snr Admin', 'CCS Snr Admin', 0, 1, 2, 12,false, null,0,0,0,now(),now(),false);
END IF;

IF NOT EXISTS (SELECT "Id" FROM "CcsServiceRoleGroup" WHERE "Key" = 'EL_CCS_JNR_ADMIN') THEN
	INSERT INTO public."CcsServiceRoleGroup"(
		"Key", "Name", "Description", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility","DisplayOrder", "MfaEnabled", 
		"DefaultEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc","IsDeleted")
		VALUES ('EL_CCS_JNR_ADMIN','CCS Jnr Admin', 'CCS Jnr Admin', 0, 1, 2,13, false, null,0,0,0,now(),now(),false);
END IF;

IF NOT EXISTS (SELECT "Id" FROM "CcsServiceRoleGroup" WHERE "Key" = 'EL_JNR_SUPPLIER') THEN
	INSERT INTO public."CcsServiceRoleGroup"(
		"Key", "Name", "Description", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility","DisplayOrder", "MfaEnabled", 
		"DefaultEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc","IsDeleted")
		VALUES ('EL_JNR_SUPPLIER','Jnr Supplier', 'Jnr Supplier', 2, 1, 0, 14,false, null,0,0,0,now(),now(),false);
END IF;


IF NOT EXISTS (SELECT "Id" FROM "CcsServiceRoleGroup" WHERE "Key" = 'EL_SNR_SUPPLIER') THEN
	INSERT INTO public."CcsServiceRoleGroup"(
		"Key", "Name", "Description", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility","DisplayOrder", "MfaEnabled", 
		"DefaultEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc","IsDeleted")
		VALUES ('EL_SNR_SUPPLIER','Snr Supplier', 'Snr Supplier', 2, 1, 0, 15,false, null,0,0,0,now(),now(),false);
END IF;



IF NOT EXISTS (SELECT "Id" FROM "CcsServiceRoleGroup" WHERE "Key" = 'TEST_SAML_CLIENT_USER') THEN
	INSERT INTO public."CcsServiceRoleGroup"(
		"Key", "Name", "Description", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility","DisplayOrder", "MfaEnabled", 
		"DefaultEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc","IsDeleted")
		VALUES ('TEST_SAML_CLIENT_USER','SAML Client', 'SAML Client', 2, 1, 2, 16,false, null,0,0,0,now(),now(),false);
END IF;


IF NOT EXISTS (SELECT "Id" FROM "CcsServiceRoleGroup" WHERE "Key" = 'TEST_SSO_CLIENT_USER') THEN
	INSERT INTO public."CcsServiceRoleGroup"(
		"Key", "Name", "Description", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility","DisplayOrder", "MfaEnabled", 
		"DefaultEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc","IsDeleted")
		VALUES ('TEST_SSO_CLIENT_USER','SSO Client', 'SSO Client', 2, 1, 2, 17,false, null,0,0,0,now(),now(),false);
END IF;


IF NOT EXISTS (SELECT "Id" FROM "CcsServiceRoleGroup" WHERE "Key" = 'DMP_SUPPLIER') THEN
	INSERT INTO public."CcsServiceRoleGroup"(
		"Key", "Name", "Description", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility","DisplayOrder", "MfaEnabled", 
		"DefaultEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc","IsDeleted")
		VALUES ('DMP_SUPPLIER','DMP', 'DMP', 2, 1, 2, 18,false, null,0,0,0,now(),now(),false);
END IF;



IF NOT EXISTS (SELECT "Id" FROM "CcsServiceRoleGroup" WHERE "Key" = 'DATA_MIGRATION') THEN
	INSERT INTO public."CcsServiceRoleGroup"(
		"Key", "Name", "Description", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility","DisplayOrder", "MfaEnabled", 
		"DefaultEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc","IsDeleted")
		VALUES ('DATA_MIGRATION','Data Migration', 'Data Migration', 0, 0, 2, 19,false, null,0,0,0,now(),now(),false);
END IF;


IF NOT EXISTS (SELECT "Id" FROM "CcsServiceRoleGroup" WHERE "Key" = 'DigiTS_GROUP') THEN
	INSERT INTO public."CcsServiceRoleGroup"(
		"Key", "Name", "Description", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility","DisplayOrder", "MfaEnabled", 
		"DefaultEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc","IsDeleted")
		VALUES ('DigiTS_GROUP','DigiTS', 'DigiTS', 2, 1, 1, 20,false, null,0,0,0,now(),now(),false);
END IF;

IF NOT EXISTS (SELECT "Id" FROM "CcsServiceRoleGroup" WHERE "Key" = 'RMI') THEN
	INSERT INTO public."CcsServiceRoleGroup"(
		"Key", "Name", "Description", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility","DisplayOrder", "MfaEnabled", 
		"DefaultEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc","IsDeleted")
		VALUES ('RMI','RMI', 'RMI', 2, 1, 2, 21,true, null,0,0,0,now(),now(),false);
END IF;

	
RETURN 1;
END;

$$ LANGUAGE plpgsql;
SELECT AddCcsServiceRoleGroup();
DROP FUNCTION AddCcsServiceRoleGroup;
