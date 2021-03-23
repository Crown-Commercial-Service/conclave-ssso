-- Create OrganisationGroups with Access Role for an organisation
-- Provide the organiosation id, and the new group name, new role name and role description.

CREATE OR REPLACE FUNCTION create_org_group_role() RETURNS integer AS $$

	DECLARE organisationId int = ; -- Provide value ex:- 8
	DECLARE groupName text = ; -- Provide value ex:- 'Admin Group'
	DECLARE roleName text = ; -- Provide value ex:- 'Administrators'
	DECLARE roleDescription text = ; -- Provide value ex:- 'Administrators Role'
	
	DECLARE organisationUserGroupId int;
	DECLARE accessRoleId int;
		
    BEGIN
		INSERT INTO public."OrganisationUserGroup"(
			"OrganisationId", "UserGroupName", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (organisationId, groupName, 0, 0, now(), now(), false);

		SELECT "Id" into organisationUserGroupId FROM public."OrganisationUserGroup" Where "OrganisationId" = @organisationId and "UserGroupName" = groupName LIMIT 1;
		
		INSERT INTO public."CcsAccessRole"(
			"CcsAccessRoleName", "CcsAccessRoleDescription", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (roleName, roleDescription,  0, 0, now(), now(), false);
		
		SELECT "Id" into accessRoleId From public."CcsAccessRole" WHERE "CcsAccessRoleName" = roleName LIMIT 1;
		
		INSERT INTO public."GroupAccess"(
			"OrganisationUserGroupId", "CcsAccessRoleId", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES (organisationUserGroupId, accessRoleId, 0, 0, now(), now(), false);
		
		RETURN 1;
	END;
$$ LANGUAGE plpgsql;

SELECT create_org_group_role();

DROP FUNCTION create_org_group_role;
