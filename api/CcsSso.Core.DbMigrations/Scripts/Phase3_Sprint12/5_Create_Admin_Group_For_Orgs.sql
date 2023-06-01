CREATE OR REPLACE FUNCTION Create_Admin_Group_For_Orgs(
		ccsAccessRoleNameKey varchar(200),
		ccsAccessRoleName text
	) RETURNS integer AS $$

DECLARE reportMode boolean = true;
DECLARE fromDate timestamp = '2023-04-01 00:00';
DECLARE toDate timestamp = '2023-05-30 23:59';

DECLARE ccsAccessRoleId int;
DECLARE organisationEligibleRoleId int;

DECLARE organisationId int;
DECLARE ciiOrganisationId text;

DECLARE userId int;
DECLARE userName text;

DECLARE organisationAdminGroupId int;
DECLARE organisationAdminGroupEligibleRoleId int;

DECLARE orgCount int = 0;
DECLARE createdGroupCount int = 0;
DECLARE roleAssignToGroupCount int = 0;
DECLARE userAssignToGroupCount int = 0;

BEGIN

SELECT "Id" INTO ccsAccessRoleId FROM "CcsAccessRole" 
WHERE "CcsAccessRoleNameKey"=ccsAccessRoleNameKey AND "CcsAccessRoleName"=ccsAccessRoleName AND "IsDeleted" = false;

RAISE NOTICE '------------------------------------------------------------------------------------------------------------------';
RAISE NOTICE '------------------------------------------------------------------------------------------------------------------';
RAISE NOTICE 'Date Range: %', CONCAT(fromDate, ' - ', toDate);
RAISE NOTICE 'Role: %', CONCAT(ccsAccessRoleId, ' - ', ccsAccessRoleNameKey, ' - ', ccsAccessRoleName);

IF (ccsAccessRoleId IS null) THEN
	RAISE NOTICE 'Role not found';
	RETURN 1;
END IF; 

FOR organisationId IN SELECT DISTINCT o."Id" 
    FROM public."Organisation" o
    WHERE o."IsDeleted" = false AND o."CreatedOnUtc" >= fromDate AND o."CreatedOnUtc" <= toDate
LOOP 
    RAISE NOTICE '---------------------------------------------------------';
    SELECT o."CiiOrganisationId" INTO ciiOrganisationId 
    FROM public."Organisation" o WHERE o."Id" = organisationId;
    RAISE NOTICE 'CiiOrganisationId: %', ciiOrganisationId;
    
    orgCount = orgCount + 1;

    SELECT "Id" INTO organisationEligibleRoleId 
    FROM public."OrganisationEligibleRole" 
    WHERE "IsDeleted" = false AND "CcsAccessRoleId" = ccsAccessRoleId AND "OrganisationId" = organisationId;
    RAISE NOTICE 'Organisation Eligible RoleId: %', organisationEligibleRoleId;
    
    IF (organisationEligibleRoleId is null)
    THEN
        RAISE NOTICE 'Organisation Eligible Role not found';
    ELSE  
        SELECT "Id" INTO organisationAdminGroupId 
        FROM public."OrganisationUserGroup"
        WHERE "IsDeleted" = false AND "OrganisationId" = organisationId
        AND "GroupType" = 1;
        
        RAISE NOTICE 'Organisation Admin Group Id: %', organisationAdminGroupId;
        
        IF (organisationAdminGroupId is null)
        THEN
            RAISE NOTICE 'Organisation Admin Group not found';
            
            IF NOT reportMode THEN	

                INSERT INTO public."OrganisationUserGroup"(
                "OrganisationId", "UserGroupNameKey", "UserGroupName", "MfaEnabled", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted", "GroupType")
                VALUES (OrganisationId, 'ORGANISATION_ADMINISTRATORS', 'Organisation Administrators', true, 0, 0, now(), now(), false, 1);
            
                SELECT "Id" INTO organisationAdminGroupId 
                FROM public."OrganisationUserGroup"
                WHERE "IsDeleted" = false AND "OrganisationId" = organisationId
                AND "GroupType" = 1;
            
                RAISE NOTICE 'Organisation Admin Group Id: %', organisationAdminGroupId;

                createdGroupCount = createdGroupCount + 1;
            
            END IF;
        END IF;               
        
        SELECT "Id" INTO organisationAdminGroupEligibleRoleId 
        FROM public."OrganisationGroupEligibleRole"
        WHERE "IsDeleted" = false 
        AND "OrganisationUserGroupId" = organisationAdminGroupId         
        AND "OrganisationEligibleRoleId" = organisationEligibleRoleId;
        
        RAISE NOTICE 'Organisation Admin Group Role Id: %', organisationAdminGroupEligibleRoleId;
        
        IF (organisationAdminGroupEligibleRoleId is null)
        THEN
            RAISE NOTICE 'Organisation Admin Group Role not found';
            
            IF NOT reportMode THEN

                INSERT INTO public."OrganisationGroupEligibleRole"(
                "OrganisationUserGroupId", "OrganisationEligibleRoleId", "CreatedUserId", "LastUpdatedUserId", 
                    "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
                VALUES (organisationAdminGroupId, organisationEligibleRoleId, 0, 0, now(), now(), false);
            
                SELECT "Id" INTO organisationAdminGroupEligibleRoleId 
                FROM public."OrganisationGroupEligibleRole"
                WHERE "IsDeleted" = false 
                AND "OrganisationUserGroupId" = organisationAdminGroupId         
                AND "OrganisationEligibleRoleId" = organisationEligibleRoleId;

                RAISE NOTICE 'Organisation Admin Group Role Id: %', organisationAdminGroupEligibleRoleId;

                roleAssignToGroupCount = roleAssignToGroupCount + 1;

            END IF;
        END IF;  
        
        FOR userId IN SELECT "UserId" 
                FROM public."UserAccessRole" 
                WHERE "IsDeleted" = false AND "OrganisationEligibleRoleId" = organisationEligibleRoleId
        LOOP
            SELECT u."UserName" INTO userName 
            FROM public."User" u 
            WHERE u."Id" = userId;
            
            RAISE NOTICE 'User Info: %', CONCAT(userId, ' - ', userName);

            IF NOT EXISTS (SELECT "Id"
                           FROM public."UserGroupMembership"
                           WHERE "IsDeleted" = false AND "UserId" = userId 
                           AND "OrganisationUserGroupId" = organisationAdminGroupId)
            THEN
                
                IF NOT reportMode THEN

                    INSERT INTO public."UserGroupMembership"(
                    "OrganisationUserGroupId", "UserId", "MembershipStartDate", "MembershipEndDate",
                        "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
                    VALUES (organisationAdminGroupId, userId, now(), now(), 0, 0, now(), now(), false);
                
                    RAISE NOTICE 'Add user to admin group';
                
                    userAssignToGroupCount = userAssignToGroupCount + 1;

                END IF;
            END IF;  

        END LOOP;
    
    END IF;
    
    RAISE NOTICE '---------------------------------------------------------';
END LOOP;

RAISE NOTICE 'No of orgs found: %', orgCount;
RAISE NOTICE 'No of group created: %', createdGroupCount;
RAISE NOTICE 'No of role assigned to group: %', roleAssignToGroupCount;
RAISE NOTICE 'No of group assigned to user: %', userAssignToGroupCount;

RETURN 1;
END;
$$ LANGUAGE plpgsql;

SELECT Create_Admin_Group_For_Orgs('ORG_ADMINISTRATOR','Organisation Administrator');

DROP FUNCTION Create_Admin_Group_For_Orgs;