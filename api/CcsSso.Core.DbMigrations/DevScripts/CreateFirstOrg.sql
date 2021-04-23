-- Create first organisation and and its party
-- Execute this first

CREATE OR REPLACE FUNCTION create_first_org() RETURNS integer AS $$
	
	DECLARE partyType text = 'EXTERNAL_ORGANISATION';
  DECLARE ciiOrgId text = 'TestCiiOrgId1';
	DECLARE partyTypeId int;
	DECLARE partyId int;
	DECLARE orgIdId int;
	DECLARE role1Id int;
	DECLARE role2Id int;
	DECLARE idp1Id int;
	DECLARE idp2Id int;
	DECLARE idp3Id int;
	DECLARE idp4Id int;
		
    BEGIN
		SELECT "Id" into partyTypeId FROM public."PartyType" WHERE "PartyTypeName" = partyType  LIMIT 1;

		INSERT INTO public."Party"(
			"PartyTypeId", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (partyTypeId, 0, 0, now(), now(), false);

		SELECT "Id" into partyId FROM public."Party" ORDER BY "CreatedOnUtc" DESC LIMIT 1;
		
		INSERT INTO public."Organisation"(
	    "CiiOrganisationId", "LegalName", "OrganisationUri", "RightToBuy", "PartyId", "IsActivated", "IsSme", "IsVcse", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	    VALUES (ciiOrgId, 'TestCiiOrg', 'TestCiiOrg.com', true, partyId, true, true, true, 0, 0, now(), now(), false);


    SELECT "Id" into orgIdId FROM public."Organisation" ORDER BY "CreatedOnUtc" DESC LIMIT 1;

    SELECT "Id" into role1Id  FROM public."CcsAccessRole"  WHERE "CcsAccessRoleNameKey" = 'ORG_ADMINISTRATOR' LIMIT 1;
    INSERT INTO public."OrganisationEligibleRole"(
	    "OrganisationId", "CcsAccessRoleId", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	    VALUES (orgIdId, role1Id, 0, 0, now(), now(), false);

    SELECT "Id" into role2Id  FROM public."CcsAccessRole"  WHERE "CcsAccessRoleNameKey" = 'ORG_DEFAULT_USER' LIMIT 1;
    INSERT INTO public."OrganisationEligibleRole"(
	    "OrganisationId", "CcsAccessRoleId", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	    VALUES (orgIdId, role2Id, 0, 0, now(), now(), false);


    SELECT "Id" into idp1Id  FROM public."IdentityProvider"  WHERE "IdpName" = 'User ID and password' LIMIT 1;
    INSERT INTO public."OrganisationEligibleIdentityProvider"(
	    "OrganisationId", "IdentityProviderId", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	    VALUES (orgIdId, idp1Id, 0, 0, now(), now(), false);

    SELECT "Id" into idp2Id  FROM public."IdentityProvider"  WHERE "IdpName" = 'Facebook' LIMIT 1;
    INSERT INTO public."OrganisationEligibleIdentityProvider"(
	    "OrganisationId", "IdentityProviderId", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	    VALUES (orgIdId, idp2Id, 0, 0, now(), now(), false);

    SELECT "Id" into idp3Id  FROM public."IdentityProvider"  WHERE "IdpName" = 'Google' LIMIT 1;
    INSERT INTO public."OrganisationEligibleIdentityProvider"(
	    "OrganisationId", "IdentityProviderId", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	    VALUES (orgIdId, idp3Id, 0, 0, now(), now(), false);

    SELECT "Id" into idp4Id  FROM public."IdentityProvider"  WHERE "IdpName" = 'Microsoft 365' LIMIT 1;
    INSERT INTO public."OrganisationEligibleIdentityProvider"(
	    "OrganisationId", "IdentityProviderId", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	    VALUES (orgIdId, idp4Id, 0, 0, now(), now(), false);
		
		RETURN 1;
	END;
$$ LANGUAGE plpgsql;

SELECT create_first_org();

DROP FUNCTION create_first_org;
