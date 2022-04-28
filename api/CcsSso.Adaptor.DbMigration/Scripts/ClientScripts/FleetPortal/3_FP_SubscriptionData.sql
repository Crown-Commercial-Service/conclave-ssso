-- Create subscription infromation for Fleet Portal
-- Includes api key configuration, user request subscription and organisation request subscription
	
CREATE OR REPLACE FUNCTION create_fp_subscription_data() RETURNS integer AS $$
	
	DECLARE consumerName text = 'Fleet Portal';
	DECLARE consumerClientId text = 'CLIENT_ID';
	DECLARE subscriptionAPIKey text = 'APIKEY';
	DECLARE subscriptionEndPointsDataType text = 'application/json';
	DECLARE userSubscriptionEndPointUrl text = 'USER_URL';
	DECLARE organisationSubscriptionEndPointUrl text = 'ORGANISATION_URL';

	DECLARE consumerId int;
	DECLARE formatId int;
   BEGIN
	
	  SELECT "Id" into consumerId FROM public."AdapterConsumer" WHERE "Name" = consumerName and "ClientId" = consumerClientId LIMIT 1;
	  SELECT "Id" into formatId FROM public."AdapterFormat"WHERE "FomatFileType" = subscriptionEndPointsDataType  LIMIT 1;


	  -- User endpoint subscription
	  INSERT INTO public."AdapterSubscription"("SubscriptionType", "SubscriptionUrl", "AdapterConsumerId", "ConclaveEntityId", "AdapterFormatId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
		VALUES ('PUSH', userSubscriptionEndPointUrl, consumerId, 2, formatId, now(), now(), false);

	  -- Organisation endpoint subscription
	  INSERT INTO public."AdapterSubscription"("SubscriptionType", "SubscriptionUrl", "AdapterConsumerId", "ConclaveEntityId", "AdapterFormatId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
		VALUES ('PUSH', organisationSubscriptionEndPointUrl, consumerId, 1, formatId, now(), now(), false);

	  -- Set subscription authentication information
	  INSERT INTO public."AdapterConsumerSubscriptionAuthMethod"(
		"APIKey", "AdapterConsumerId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
		VALUES (subscriptionAPIKey, consumerId, now(), now(), false);
    
	  RETURN 1;
	END;
	
$$ LANGUAGE plpgsql;


SELECT setval('"AdapterSubscription_Id_seq"', max("Id")) FROM "AdapterSubscription";
SELECT setval('"AdapterConsumerSubscriptionAuthMethod_Id_seq"', max("Id")) FROM "AdapterConsumerSubscriptionAuthMethod";
SELECT create_fp_subscription_data();

DROP FUNCTION create_fp_subscription_data;
