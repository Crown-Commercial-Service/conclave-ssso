-- Create first user with party and person for the already created organisation
-- Execute this after executing the CreateFirstOrg.sql

CREATE OR REPLACE FUNCTION create_first_user() RETURNS integer AS $$
	
	DECLARE userName text = 'testuserone@mail.com';
	DECLARE partyType text = 'USER';
	DECLARE ciiOrgId text = 'TestCiiOrgId1';
	DECLARE idpName text = 'User ID and password';
	DECLARE partyTypeId int;
	DECLARE partyId int;
	DECLARE organisationId int;
	DECLARE idpId int;
	DECLARE orgIdpId int;
	DECLARE userId int;
	DECLARE role1Id int;
	DECLARE orgRole1Id int;
		
    BEGIN
		SELECT "Id" into partyTypeId FROM public."PartyType" WHERE "PartyTypeName" = partyType  LIMIT 1;

		INSERT INTO public."Party"(
			"PartyTypeId", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (partyTypeId, 0, 0, now(), now(), false);

		SELECT "Id" into partyId FROM public."Party" ORDER BY "CreatedOnUtc" DESC LIMIT 1;
		SELECT "Id" into organisationId FROM public."Organisation" WHERE "CiiOrganisationId" = ciiOrgId  LIMIT 1;
		SELECT "Id" into idpId FROM public."IdentityProvider" WHERE "IdpName" = idpName  LIMIT 1;
    SELECT "Id" into orgIdpId FROM public."OrganisationEligibleIdentityProvider" WHERE "OrganisationId" = organisationId AND "IdentityProviderId" = idpId  LIMIT 1;

    INSERT INTO public."User"(
	    "UserName", "JobTitle", "UserTitle", "PartyId", "OrganisationEligibleIdentityProviderId", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	    VALUES (userName, 'TestJobTitle', 0, partyId, orgIdpId, 0, 0, now(), now(), false);
		INSERT INTO public."Person"(
			"OrganisationId", "PartyId", "Title", "FirstName", "LastName", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (organisationId, partyId, 0, 'TestUserOneFN', 'TestUserOneLN', 0, 0, now(), now(), false);

    SELECT "Id" into userId FROM public."User" WHERE "UserName" = userName  LIMIT 1;
		SELECT "Id" into role1Id  FROM public."CcsAccessRole"  WHERE "CcsAccessRoleNameKey" = 'ORG_ADMINISTRATOR' LIMIT 1;
    SELECT "Id" into orgRole1Id FROM public."OrganisationEligibleRole" WHERE  "OrganisationId" = organisationId AND  "CcsAccessRoleId" = role1Id;

    INSERT INTO public."UserAccessRole"(
	    "UserId", "OrganisationEligibleRoleId", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	    VALUES (userId, orgRole1Id, 0, 0, now(), now(), false);
		RETURN 1;
	END;
$$ LANGUAGE plpgsql;

SELECT create_first_user();

DROP FUNCTION create_first_user;
