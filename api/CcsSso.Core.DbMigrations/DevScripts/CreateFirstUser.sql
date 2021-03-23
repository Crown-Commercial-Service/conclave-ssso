-- Create first user with party and person for the already created organisation
-- Execute this after executing the CreateFirstOrg.sql

CREATE OR REPLACE FUNCTION create_first_user() RETURNS integer AS $$
	
	DECLARE partyType text = 'USER';
	DECLARE partyTypeId int;
	DECLARE partyId int;
	DECLARE organisationId int;
	DECLARE idpId int;
		
    BEGIN
	
		INSERT INTO public."IdentityProvider"(
			"IdpUri", "IdpName", "ExternalIdpFlag", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('TestIDPUrl', 'TestIDP', false, 0, 0, now(), now(), false);
			
		SELECT "Id" into partyTypeId FROM public."PartyType" WHERE "PartyTypeName" = partyType  LIMIT 1;

		INSERT INTO public."Party"(
			"PartyTypeId", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (partyTypeId, 0, 0, now(), now(), false);

		SELECT "Id" into partyId FROM public."Party" ORDER BY "CreatedOnUtc" DESC LIMIT 1;
		SELECT "Id" into organisationId FROM public."Organisation" ORDER BY "CreatedOnUtc" LIMIT 1;
		SELECT "Id" into idpId FROM public."IdentityProvider" ORDER BY "CreatedOnUtc" LIMIT 1;
		
		INSERT INTO public."User"(
			"UserName", "JobTitle", "UserTitle", "PartyId", "IdentityProviderId", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('testuserone@mail.com', 'TestJobTitle', 1, partyId, idpId,  0, 0, now(), now(), false);

		INSERT INTO public."Person"(
			"OrganisationId", "PartyId", "Title", "FirstName", "LastName", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (organisationId, partyId, 1, 'TestUserOneFN', 'TestUserOneLN', 0, 0, now(), now(), false);
		
		RETURN 1;
	END;
$$ LANGUAGE plpgsql;

SELECT create_first_user();

DROP FUNCTION create_first_user;
