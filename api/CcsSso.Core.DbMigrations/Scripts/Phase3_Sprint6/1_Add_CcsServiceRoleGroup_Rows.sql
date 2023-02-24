
CREATE OR REPLACE FUNCTION AddCcsServiceRoleGroup() RETURNS integer AS $$

BEGIN

IF NOT EXISTS (SELECT "Id" FROM "CcsServiceRoleGroup" WHERE "Key" = 'CAS_USER_GROUP') THEN
	INSERT INTO public."CcsServiceRoleGroup"(
		"Key", "Name", "Description", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility","DisplayOrder","MfaEnabled", 
		"DefaultEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc","IsDeleted")
		VALUES ('CAS_USER_GROUP','Contract Award Service', 'Find and contract suitable suppliers for your procurement project, and ask them about the services they can provide. Progress to one stage further competition.', 2, 1, 1,1, false, null,0,0,0,now(),now(),false);
END IF;

IF NOT EXISTS (SELECT "Id" FROM "CcsServiceRoleGroup" WHERE "Key" = 'FP_USER_GROUP') THEN
	INSERT INTO public."CcsServiceRoleGroup"(
		"Key", "Name", "Description", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility","DisplayOrder", "MfaEnabled", 
		"DefaultEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc","IsDeleted")
		VALUES ('FP_USER_GROUP','Fleet Portal', 'A self-service system where customers can obtain live pricing quotes for either purchasing or leasing standard build cars and light commercial vehicles.', 2, 1, 1, 2,false, null,1,0,0,now(),now(),false);
END IF;


IF NOT EXISTS (SELECT "Id" FROM "CcsServiceRoleGroup" WHERE "Key" = 'JAEGGER_BUYER_GROUP') THEN
	INSERT INTO public."CcsServiceRoleGroup"(
		"Key", "Name", "Description", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility","DisplayOrder", "MfaEnabled", 
		"DefaultEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc","IsDeleted")
		VALUES ('JAEGGER_BUYER_GROUP','eSourcing as a Buyer', 'The eSourcing tool will help you supply to, or buy for, the public sector, compliantly.', 2, 1, 1, 3,false, null,0,0,0,now(),now(),false);
END IF;

IF NOT EXISTS (SELECT "Id" FROM "CcsServiceRoleGroup" WHERE "Key" = 'JAEGGER_SUPPLIER_GROUP') THEN
	INSERT INTO public."CcsServiceRoleGroup"(
		"Key", "Name", "Description", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility","DisplayOrder", "MfaEnabled", 
		"DefaultEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc","IsDeleted")
		VALUES ('JAEGGER_SUPPLIER_GROUP','eSourcing as a Supplier', 'The eSourcing tool will help you supply to, or buy for, the public sector, compliantly.', 2, 0, 0, 4,false, null,0,0,0,now(),now(),false);
END IF;

IF NOT EXISTS (SELECT "Id" FROM "CcsServiceRoleGroup" WHERE "Key" = 'ORG_ADMINISTRATOR_GROUP') THEN
	INSERT INTO public."CcsServiceRoleGroup"(
		"Key", "Name", "Description", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility","DisplayOrder", "MfaEnabled", 
		"DefaultEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc","IsDeleted")
		VALUES ('ORG_ADMINISTRATOR_GROUP','Organisation Administrator', 'Administrator of as organisation', 2, 0, 2,5, true, null,0,0,0,now(),now(),false);
END IF;

IF NOT EXISTS (SELECT "Id" FROM "CcsServiceRoleGroup" WHERE "Key" = 'ORG_DEFAULT_USER_GROUP') THEN
	INSERT INTO public."CcsServiceRoleGroup"(
		"Key", "Name", "Description", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility","DisplayOrder", "MfaEnabled", 
		"DefaultEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc","IsDeleted")
		VALUES ('ORG_DEFAULT_USER_GROUP','Organisation User', 'Default user of an organisation', 2, 0, 2, 6,false, null,0,0,0,now(),now(),false);
END IF;


IF NOT EXISTS (SELECT "Id" FROM "CcsServiceRoleGroup" WHERE "Key" = 'ORG_USER_SUPPORT_GROUP') THEN
	INSERT INTO public."CcsServiceRoleGroup"(
		"Key", "Name", "Description", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility","DisplayOrder", "MfaEnabled", 
		"DefaultEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc","IsDeleted")
		VALUES ('ORG_USER_SUPPORT_GROUP','Organisation Users Support', 'Support for Org Users', 0, 0, 2, 7,true, null,0,0,0,now(),now(),false);
END IF;


IF NOT EXISTS (SELECT "Id" FROM "CcsServiceRoleGroup" WHERE "Key" = 'MANAGE_SUBSCRIPTIONS_GROUP') THEN
	INSERT INTO public."CcsServiceRoleGroup"(
		"Key", "Name", "Description", "OrgTypeEligibility", "SubscriptionTypeEligibility", "TradeEligibility","DisplayOrder", "MfaEnabled", 
		"DefaultEligibility", "ApprovalRequired","CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc","IsDeleted")
		VALUES ('MANAGE_SUBSCRIPTIONS_GROUP','Manage Service Eligibility', 'Service Subscriptions for Organisation', 0, 0, 2, 8,true, null,0,0,0,now(),now(),false);
END IF;

	
RETURN 1;
END;

$$ LANGUAGE plpgsql;
SELECT AddCcsServiceRoleGroup();
DROP FUNCTION AddCcsServiceRoleGroup;
