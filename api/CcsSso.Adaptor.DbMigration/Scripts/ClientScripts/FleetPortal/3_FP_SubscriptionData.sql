-- Create subscription infromation for Fleet Portal
	
CREATE OR REPLACE FUNCTION create_fp_subscription_data() RETURNS integer AS $$
	
	DECLARE consumerName text = 'Fleet Portal';
	DECLARE consumerClientId text = 'CLIENT_ID';
	DECLARE subscriptionAPIKey text = '';
	DECLARE userSubscriptionEndPoint text = 'USER_URL';
	DECLARE organisationSubscriptionEndPoint text = 'ORGANISATION_URL';
	DECLARE userSubscriptionEndPointDataType text = 'application/json';
	DECLARE organisationSubscriptionEndPointDataType text = 'application/json';

	DECLARE consumerId int;
   BEGIN
	
	  SELECT "Id" into consumerId FROM public."AdapterConsumer" WHERE "Name" = consumerName and "ClientId" = consumerClientId LIMIT 1;

    
	END;
$$ LANGUAGE plpgsql;


SELECT setval('"AdapterConsumer_Id_seq"', max("Id")) FROM "AdapterConsumer";
SELECT setval('"AdapterConsumerEntity_Id_seq"', max("Id")) FROM "AdapterConsumerEntity";
SELECT setval('"AdapterConsumerEntityAttribute_Id_seq"', max("Id")) FROM "AdapterConsumerEntityAttribute";
SELECT setval('"AdapterConclaveAttributeMapping_Id_seq"', max("Id")) FROM "AdapterConclaveAttributeMapping";
SELECT create_fp_subscription_data();

DROP FUNCTION create_fp_subscription_data;

DROP TYPE IF EXISTS type_consumer_conclave_mapping;
