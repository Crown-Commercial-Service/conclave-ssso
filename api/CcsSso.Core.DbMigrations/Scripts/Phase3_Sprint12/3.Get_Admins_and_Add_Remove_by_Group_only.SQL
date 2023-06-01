CREATE OR REPLACE FUNCTION GET_ADMINS_AND_ADD_REMOVE_BY_GROUP_ONLY() RETURNS integer AS $$
DECLARE nonAdminProfileUserDetails RECORD;
DECLARE groupAdminUserIds text = '';
DECLARE ccsAccessRoleId int;
DECLARE orgId int;
DECLARE orgEligibleRoleId int;
DECLARE reportMode boolean= true;

BEGIN

RAISE NOTICE 'Reporting Mode: % ',reportMode;
RAISE NOTICE 'Below users have access of admin role from group only.';

	SELECT "Id" INTO ccsAccessRoleId FROM "CcsAccessRole" 
	WHERE "CcsAccessRoleNameKey"='ORG_ADMINISTRATOR' AND "IsDeleted" = false;

	FOR nonAdminProfileUserDetails IN 
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
			car."CcsAccessRoleNameKey" = 'ORG_ADMINISTRATOR'
		)
	ORDER BY uar."UserId" ASC

	LOOP 
	--RAISE NOTICE 'User id: % checking for group with admin role.', nonAdminProfileUserDetails."UserId";	
		IF EXISTS (
			SELECT DISTINCT ugm."UserId"
			from "UserGroupMembership" ugm
			inner join "OrganisationUserGroup" oug ON oug."Id" = ugm."OrganisationUserGroupId"
			inner join "OrganisationGroupEligibleRole" oger ON oger."OrganisationUserGroupId" = oug."Id"
			inner join "OrganisationEligibleRole" oer ON oer."Id" = oger."OrganisationEligibleRoleId"
			Inner join public."CcsAccessRole" car ON car."Id" = oer."CcsAccessRoleId"
			WHERE ugm."IsDeleted" = false and oug."IsDeleted" = false and oger."IsDeleted" = false and oer."IsDeleted" = false and car."IsDeleted" = false
				and car."CcsAccessRoleNameKey" = 'ORG_ADMINISTRATOR'
				and ugm."UserId" = nonAdminProfileUserDetails."UserId"
			ORDER BY "UserId" ASC)
		THEN
			-- assign admin role to user
			
			SELECT p0."OrganisationId" INTO orgId
			FROM "User" AS u
			INNER JOIN "Party" AS p ON u."PartyId" = p."Id"
			LEFT JOIN "Person" AS p0 ON p."Id" = p0."PartyId"
			where u."Id" =nonAdminProfileUserDetails."UserId"; 
		  
			SELECT oer."Id" INTO orgEligibleRoleId 
			FROM public."OrganisationEligibleRole" oer
			WHERE "CcsAccessRoleId"= ccsAccessRoleId AND "OrganisationId" = orgId;

			RAISE NOTICE 'User id: % Name: %', nonAdminProfileUserDetails."UserId", nonAdminProfileUserDetails."UserName";
			RAISE NOTICE 'Org Id: % Org Eligible Role Id: %', orgId, orgEligibleRoleId;
		
			IF NOT reportMode THEN		
				IF NOT EXISTS (select "Id" from "UserAccessRole" where "UserId"=nonAdminProfileUserDetails."UserId" and "OrganisationEligibleRoleId"=	orgEligibleRoleId and "IsDeleted"=false)
				THEN 
					RAISE NOTICE 'inside insert';
					INSERT INTO public."UserAccessRole"(
						"UserId", "OrganisationEligibleRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted" )
							VALUES (nonAdminProfileUserDetails."UserId",orgEligibleRoleId ,0, 0, now(), now(), false);
				END IF;
			END IF;
			
			groupAdminUserIds = CONCAT(groupAdminUserIds, nonAdminProfileUserDetails."UserId", ',');
			
		END IF; 
	END LOOP;
RAISE NOTICE '%', groupAdminUserIds;

RAISE NOTICE 'Remove Org Admin Role from Groups';

IF NOT reportMode THEN
	UPDATE "OrganisationGroupEligibleRole"
	SET "IsDeleted"=true 
	WHERE "OrganisationEligibleRoleId" IN (    
        SELECT oer."Id"
        FROM public."OrganisationEligibleRole" oer
        WHERE oer."CcsAccessRoleId" = ccsAccessRoleId);
END IF;
			
RETURN 1;
END;
$$ LANGUAGE plpgsql;

SELECT GET_ADMINS_AND_ADD_REMOVE_BY_GROUP_ONLY();

DROP FUNCTION GET_ADMINS_AND_ADD_REMOVE_BY_GROUP_ONLY;
