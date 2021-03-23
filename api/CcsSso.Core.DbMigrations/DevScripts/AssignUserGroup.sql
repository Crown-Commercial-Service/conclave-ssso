-- Assign a user to a Organisation Group
-- Provide the user id, organisation id and the user group name to execute.

CREATE OR REPLACE FUNCTION assign_user_group() RETURNS integer AS $$

	DECLARE organisationId int = ;  -- Provide value ex:- 8
	DECLARE userId int = ; -- Provide value ex:- 8
	DECLARE groupName text = ; -- Provide value ex:- 'Admin Group'
	
	DECLARE organisationUserGroupId int;
		
    BEGIN

		SELECT "Id" into organisationUserGroupId FROM public."OrganisationUserGroup" Where "OrganisationId" = organisationId and "UserGroupName" = groupName LIMIT 1;

		INSERT INTO public."UserGroupMembership"(
			"OrganisationUserGroupId", "UserId", "MembershipStartDate", "MembershipEndDate", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (organisationUserGroupId, userId, now(), now(), 0, 0, now(), now(), false);
		
		RETURN 1;
	END;
$$ LANGUAGE plpgsql;

SELECT assign_user_group();

DROP FUNCTION assign_user_group;
