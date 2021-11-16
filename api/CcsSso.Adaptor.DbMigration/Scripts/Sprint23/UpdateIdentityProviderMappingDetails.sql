UPDATE public."ConclaveEntityAttribute"
	SET "AttributeName"='Detail.IdentityProviders.IdentityProviderId', "LastUpdatedOnUtc"=now()
	WHERE "Id"=29;

UPDATE public."ConclaveEntityAttribute"
	SET "AttributeName"='Detail.IdentityProviders.IdentityProviderDisplayName', "LastUpdatedOnUtc"=now()
	WHERE "Id"=30;

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (87, 'Detail.IdentityProviders', 2, now(), now(), false);
