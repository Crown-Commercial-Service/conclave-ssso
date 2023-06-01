
CREATE OR REPLACE FUNCTION GET_FLEET_BY_GROUP_ONLY() RETURNS integer AS $$
DECLARE nonFleetProfileUserDetails RECORD;
DECLARE groupFleetUserIds text = '';
BEGIN

RAISE NOTICE 'Below users have access of fleet role from group only.';

	FOR nonFleetProfileUserDetails IN 
		SELECT distinct uar."UserId", u."UserName" 
			FROM public."UserAccessRole" uar 
			INNER JOIN "User" u ON u."Id" = uar."UserId"
		WHERE uar."IsDeleted" = false AND u."IsDeleted" = false AND 
		uar."UserId" NOT IN (
			SELECT DISTINCT ua."UserId"
			FROM public."UserAccessRole" ua
			Inner join public."OrganisationEligibleRole" oer ON oer."Id" = ua."OrganisationEligibleRoleId"
			Inner join public."CcsAccessRole" car ON car."Id" = oer."CcsAccessRoleId"
			WHERE ua."IsDeleted" = false and oer."IsDeleted" = false and car."IsDeleted" = false and
			car."CcsAccessRoleNameKey" = 'FP_USER' AND car."CcsAccessRoleName"='Fleet Portal User'
		)
	ORDER BY uar."UserId" ASC

	LOOP 
	--RAISE NOTICE 'User id: % checking for group with Fleet role.', nonFleetProfileUserDetails."UserId";	
		IF EXISTS (
			SELECT DISTINCT ugm."UserId"
			from "UserGroupMembership" ugm
			inner join "OrganisationUserGroup" oug ON oug."Id" = ugm."OrganisationUserGroupId"
			inner join "OrganisationGroupEligibleRole" oger ON oger."OrganisationUserGroupId" = oug."Id"
			inner join "OrganisationEligibleRole" oer ON oer."Id" = oger."OrganisationEligibleRoleId"
			Inner join public."CcsAccessRole" car ON car."Id" = oer."CcsAccessRoleId"
			WHERE ugm."IsDeleted" = false and oug."IsDeleted" = false and oger."IsDeleted" = false and oer."IsDeleted" = false and car."IsDeleted" = false
				and car."CcsAccessRoleNameKey" = 'FP_USER' and car."CcsAccessRoleName" = 'Fleet Portal User'
				and ugm."UserId" = nonFleetProfileUserDetails."UserId"
			ORDER BY "UserId" ASC)
		THEN
			RAISE NOTICE 'User id: % Name: %', nonFleetProfileUserDetails."UserId", nonFleetProfileUserDetails."UserName";
			groupFleetUserIds = CONCAT(groupFleetUserIds, nonFleetProfileUserDetails."UserId", ',');
		END IF; 
	END LOOP;
RAISE NOTICE '%', groupFleetUserIds;

RETURN 1;
END;
$$ LANGUAGE plpgsql;

SELECT GET_FLEET_BY_GROUP_ONLY();

DROP FUNCTION GET_FLEET_BY_GROUP_ONLY;
