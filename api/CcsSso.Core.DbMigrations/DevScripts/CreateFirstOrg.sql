-- Create first organisation and and its party
-- Execute this first

CREATE OR REPLACE FUNCTION create_first_org() RETURNS integer AS $$
	
	DECLARE partyType text = 'INTERNAL_ORGANISATION';
	DECLARE partyTypeId int;
	DECLARE partyId int;
		
    BEGIN
		SELECT "Id" into partyTypeId FROM public."PartyType" WHERE "PartyTypeName" = partyType  LIMIT 1;

		INSERT INTO public."Party"(
			"PartyTypeId", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (partyTypeId, 0, 0, now(), now(), false);

		SELECT "Id" into partyId FROM public."Party" ORDER BY "CreatedOnUtc" DESC LIMIT 1;
		
		INSERT INTO public."Organisation"(
			"CiiOrganisationId", "LegalName", "OrganisationUri", "RightToBuy", "PartyId", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "IsActivated", "IsSme", "IsVcse")
			VALUES ('CcsOrgTest1','Test Organisation', 'testorg.com', true, partyId, 0, 0, now(), now(), false, true, false, false);
		
		RETURN 1;
	END;
$$ LANGUAGE plpgsql;

SELECT create_first_org();

DROP FUNCTION create_first_org;
