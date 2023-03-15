CREATE OR REPLACE FUNCTION Assign_Service_To_Org_User_Group(
		oldCcsAccessRoleNameKey varchar(200),
		oldCcsAccessRoleName text,
		newCcsAccessRoleNameKey varchar(200),
		newCcsAccessRoleName text
	) RETURNS integer AS $$

DECLARE fromDate timestamp = '2023-03-07 00:00';
DECLARE toDate timestamp = '2023-03-07 23:59';

DECLARE oldCcsAccessRoleId int;
DECLARE newCcsAccessRoleId int;
DECLARE oldOrganisationEligibleRoleId int;
DECLARE newOrganisationEligibleRoleId int;

DECLARE organisationId int;
DECLARE ciiOrganisationId text;
DECLARE userId int;
DECLARE userName text;
DECLARE groupId int;
DECLARE groupName text;

DECLARE roleAssignToOrgCount int = 0;
DECLARE roleAssignToUserCount int = 0;
DECLARE roleAssignToGroupCount int = 0;

BEGIN

SELECT "Id" INTO oldCcsAccessRoleId FROM "CcsAccessRole" 
WHERE "CcsAccessRoleNameKey"=oldCcsAccessRoleNameKey AND "CcsAccessRoleName"=oldCcsAccessRoleName AND "IsDeleted" = false;

SELECT "Id" INTO newCcsAccessRoleId FROM "CcsAccessRole" 
WHERE "CcsAccessRoleNameKey"=newCcsAccessRoleNameKey AND "CcsAccessRoleName"=newCcsAccessRoleName AND "IsDeleted" = false;

RAISE NOTICE '------------------------------------------------------------------------------------------------------------------';
RAISE NOTICE '------------------------------------------------------------------------------------------------------------------';
RAISE NOTICE 'Date Range: %', CONCAT(fromDate, ' - ', toDate);
RAISE NOTICE 'Current Role: %', CONCAT(oldCcsAccessRoleId, ' - ', oldCcsAccessRoleNameKey, ' - ', oldCcsAccessRoleName);
RAISE NOTICE 'New Role: %', CONCAT(newCcsAccessRoleId, ' - ', newCcsAccessRoleNameKey, ' - ', newCcsAccessRoleName);

IF (oldCcsAccessRoleId IS null or newCcsAccessRoleId IS null) THEN
	RAISE NOTICE 'Role not found';
	RETURN 1;
END IF; 


FOR organisationId IN SELECT DISTINCT o."Id" 
    FROM public."OrganisationEligibleRole" oer
    JOIN public."Organisation" o ON o."Id" = oer."OrganisationId"
    WHERE o."IsDeleted" = false AND o."CreatedOnUtc" >= fromDate AND o."CreatedOnUtc" <= toDate
    AND oer."IsDeleted" = false AND oer."CcsAccessRoleId" = oldCcsAccessRoleId
LOOP 
    RAISE NOTICE '---------------------------------------------------------';
    SELECT o."CiiOrganisationId" INTO ciiOrganisationId FROM public."Organisation" o WHERE o."Id" = organisationId;
    RAISE NOTICE 'CiiOrganisationId: %', ciiOrganisationId;
    
    SELECT "Id" INTO oldOrganisationEligibleRoleId FROM public."OrganisationEligibleRole" WHERE "IsDeleted" = false AND "CcsAccessRoleId" = oldCcsAccessRoleId AND "OrganisationId" = organisationId;
    RAISE NOTICE 'Current Organisation Eligible RoleId: %', oldOrganisationEligibleRoleId;
    
    SELECT "Id" INTO newOrganisationEligibleRoleId FROM public."OrganisationEligibleRole" WHERE "IsDeleted" = false AND "CcsAccessRoleId" = newCcsAccessRoleId AND "OrganisationId" = organisationId;    
    RAISE NOTICE 'New Organisation Eligible RoleId: %', newOrganisationEligibleRoleId;
    
    IF (newOrganisationEligibleRoleId is null)
    THEN
        RAISE NOTICE 'Add missing role to org';
        
        INSERT INTO public."OrganisationEligibleRole"(
        "OrganisationId", "CcsAccessRoleId", "MfaEnabled", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
        SELECT organisationId, newCcsAccessRoleId, "MfaEnabled", 0, 0, now(), now(), false
        FROM public."OrganisationEligibleRole" WHERE "Id" = oldOrganisationEligibleRoleId;
        
        SELECT "Id" INTO newOrganisationEligibleRoleId FROM public."OrganisationEligibleRole" 
        WHERE "IsDeleted" = false AND "CcsAccessRoleId" = newCcsAccessRoleId AND "OrganisationId" = organisationId;
        
        RAISE NOTICE 'New Organisation Eligible RoleId: %', newOrganisationEligibleRoleId;
        
        roleAssignToOrgCount = roleAssignToOrgCount + 1;
    END IF;  
                
    FOR userId IN SELECT "UserId" FROM public."UserAccessRole" 
                  WHERE "IsDeleted" = false AND "OrganisationEligibleRoleId" = oldOrganisationEligibleRoleId
    LOOP
        SELECT u."UserName" INTO userName FROM public."User" u WHERE u."Id" = userId;
        RAISE NOTICE 'User Info: %', CONCAT(userId, ' - ', userName);

        IF NOT EXISTS (SELECT "UserId" FROM public."UserAccessRole"
                       WHERE "IsDeleted" = false AND "UserId" = userId AND "OrganisationEligibleRoleId" = newOrganisationEligibleRoleId)
        THEN
            RAISE NOTICE 'Add missing role to user';

            INSERT INTO public."UserAccessRole"(
            "UserId", "OrganisationEligibleRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
            VALUES(userId, newOrganisationEligibleRoleId, 0, 0, now(), now(), false);
            
            roleAssignToUserCount = roleAssignToUserCount + 1;
        END IF;  
    
    END LOOP;
    
    FOR groupId IN SELECT "OrganisationUserGroupId" FROM public."OrganisationGroupEligibleRole" 
                    WHERE "IsDeleted" = false AND "OrganisationEligibleRoleId" = oldOrganisationEligibleRoleId
    LOOP
        SELECT og."UserGroupName" INTO groupName FROM public."OrganisationUserGroup" og WHERE og."Id" = groupId;
        RAISE NOTICE 'Group Info: %', CONCAT(groupId, ' - ', groupName);
        
        IF NOT EXISTS (SELECT "OrganisationUserGroupId" FROM public."OrganisationGroupEligibleRole"
                       WHERE "IsDeleted" = false AND "OrganisationUserGroupId" = groupId 
                       AND "OrganisationEligibleRoleId" = newOrganisationEligibleRoleId)
        THEN
            RAISE NOTICE 'Add missing role to group';

            INSERT INTO public."OrganisationGroupEligibleRole"(
            "OrganisationUserGroupId", "OrganisationEligibleRoleId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
            VALUES(groupId, newOrganisationEligibleRoleId, 0, 0, now(), now(), false);
            
            roleAssignToGroupCount = roleAssignToGroupCount + 1;
        END IF;  
    
    END LOOP;
    
    RAISE NOTICE '---------------------------------------------------------';
END LOOP;

RAISE NOTICE 'No of role assigned to org: %', roleAssignToOrgCount;
RAISE NOTICE 'No of role assigned to user: %', roleAssignToUserCount;
RAISE NOTICE 'No of role assigned to group: %', roleAssignToGroupCount;

RETURN 1;
END;
$$ LANGUAGE plpgsql;

SELECT Assign_Service_To_Org_User_Group('TEST_SAML_CLIENT_USER','Test SAML Client User','TEST_SAML_CLIENT_USER','SAML Client Tile');

SELECT Assign_Service_To_Org_User_Group('TEST_SSO_CLIENT_USER','Test SSO Client User','TEST_SSO_CLIENT_USER','SSO Client Tile');

SELECT Assign_Service_To_Org_User_Group('DATA_MIGRATION','Data Migration','DATA_MIGRATION','Access Data Migration');

SELECT Assign_Service_To_Org_User_Group('ACCESS_DIGITS_CLIENT','Access DigiTS','DigiTS_USER','Access DigiTS');    

SELECT Assign_Service_To_Org_User_Group('RMI_USER','RMI User','RMI_USER','RMI Tile');    

DROP FUNCTION Assign_Service_To_Org_User_Group;