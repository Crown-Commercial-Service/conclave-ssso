
-- Reporting mode to see the list of groups, orgs, users 
SELECT DISTINCT t.* FROM 
(
	SELECT o."CiiOrganisationId",ogroup."UserGroupName",u."UserName", ogroup."Id" AS "OrganisationUserGroupID",
	o."Id" AS "OrgId",o."LegalName", ar."CcsAccessRoleNameKey",
	ar."CcsAccessRoleName", ar."Id" AS "CcsAccessRoleId", oge."IsDeleted",
	oge."Id" AS "OrgGroupEligibleRoleID"
		FROM "OrganisationUserGroup" AS ogroup
		INNER JOIN "Organisation" AS o ON ogroup."OrganisationId" = o."Id"
		LEFT JOIN "OrganisationGroupEligibleRole" AS oge ON oge."OrganisationUserGroupId" = ogroup."Id"
		LEFT JOIN "OrganisationEligibleRole" AS oer ON oge."OrganisationEligibleRoleId" = oer."Id"
		INNER JOIN "CcsAccessRole" AS ar ON oer."CcsAccessRoleId" = ar."Id"
		LEFT JOIN "UserGroupMembership" AS ugm on ugm."OrganisationUserGroupId"= ogroup."Id"
		LEFT JOIN "User" AS u on u."Id"=ugm."UserId" AND u."IsDeleted"=false
	WHERE 
	ar."CcsAccessRoleNameKey" IN ('FP_USER', 'ACCESS_FP_CLIENT')
	AND o."CiiOrganisationId" = '243431619787053126' --uncomment this line to test for a particular organisation.
) 
AS t
WHERE t."IsDeleted"=false


-- update mode
UPDATE "OrganisationGroupEligibleRole" SET "IsDeleted"=true WHERE "Id" IN 
	(
		SELECT distinct t."OrgGroupEligibleRoleID" FROM 
		(
			SELECT  oge."Id" AS "OrgGroupEligibleRoleID" ,oge."IsDeleted"
				FROM "OrganisationUserGroup" AS ogroup
				INNER JOIN "Organisation" AS o ON ogroup."OrganisationId" = o."Id"
				LEFT JOIN "OrganisationGroupEligibleRole" AS oge ON oge."OrganisationUserGroupId" = ogroup."Id"
				LEFT JOIN "OrganisationEligibleRole" AS oer ON oge."OrganisationEligibleRoleId" = oer."Id"
				INNER JOIN "CcsAccessRole" AS ar ON oer."CcsAccessRoleId" = ar."Id"

				LEFT JOIN "UserGroupMembership" AS ugm on ugm."OrganisationUserGroupId"= ogroup."Id"
				LEFT JOIN "User" AS u on u."Id"=ugm."UserId" AND u."IsDeleted"=false
			WHERE 
			ar."CcsAccessRoleNameKey" IN ('FP_USER', 'ACCESS_FP_CLIENT')
			AND o."CiiOrganisationId" = '243431619787053126' --uncomment this line to test for a particular organisation.
		) 
		AS t
		WHERE t."IsDeleted"=false
	)
		