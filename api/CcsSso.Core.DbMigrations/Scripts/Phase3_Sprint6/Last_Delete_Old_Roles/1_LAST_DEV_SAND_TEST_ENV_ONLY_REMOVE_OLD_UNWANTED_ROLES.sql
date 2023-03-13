CREATE OR REPLACE FUNCTION DEV_SAND_TEST_ENV_ONLY_REMOVE_OLD_UNWANTED_ROLES(
		ccsAccessRoleNameKey varchar(200)
	) RETURNS integer AS $$

DECLARE reportingModeOn boolean = 'true';
DECLARE deleteRoleFromCcsAccessRoleTable boolean = 'false';
DECLARE fromDate timestamp = '2023-03-07 00:00';
DECLARE toDate timestamp = '2023-03-07 23:59';

DECLARE ccsAccessRoleId int;
DECLARE organisationEligibleRoleId int;

DECLARE ccsAccessRoleDetails RECORD;
DECLARE organisationDetails RECORD;
DECLARE groupDetails RECORD;
DECLARE userDetails RECORD;

DECLARE orgsAffectedCount int = 0;
DECLARE groupAffectedCount int = 0;
DECLARE userAffectedCount int = 0;
DECLARE roleMappingDeleteCount int = 0;
DECLARE autoValidationRoleDeletedCount int = 0;
DECLARE roleConfigDeletedCount int = 0;

BEGIN

IF (reportingModeOn = 'true') THEN
	RAISE NOTICE 'Reporting mode is On.';
ELSE
	RAISE NOTICE 'Reporting mode is Off.';
END IF;

RAISE NOTICE 'Date Range: %', CONCAT(fromDate, ' - ', toDate);

FOR ccsAccessRoleDetails IN SELECT "Id" FROM "CcsAccessRole" 
	WHERE "CcsAccessRoleNameKey"= ccsAccessRoleNameKey AND "IsDeleted" = false
LOOP

ccsAccessRoleId = ccsAccessRoleDetails."Id";
orgsAffectedCount = 0;

	IF (ccsAccessRoleId IS null) THEN
		RAISE NOTICE 'Role not found %', ccsAccessRoleNameKey;
		RETURN 1;
	END IF; 

	RAISE NOTICE 'Role found: %', ccsAccessRoleNameKey;


	FOR organisationDetails IN SELECT DISTINCT o."CiiOrganisationId", o."LegalName", oer."Id"
		FROM public."OrganisationEligibleRole" oer
		JOIN public."Organisation" o ON o."Id" = oer."OrganisationId"
		WHERE o."IsDeleted" = false AND o."CreatedOnUtc" >= fromDate AND o."CreatedOnUtc" <= toDate
		AND oer."IsDeleted" = false AND oer."CcsAccessRoleId" = ccsAccessRoleId
	LOOP 
		organisationEligibleRoleId = organisationDetails."Id";
		orgsAffectedCount = orgsAffectedCount + 1;

		RAISE NOTICE 'Removing Role % for Organisation Id: %, Name: %', ccsAccessRoleNameKey, organisationDetails."CiiOrganisationId", organisationDetails."LegalName";
		RAISE NOTICE 'OrganisationEligibleRole: % AND RoleId: %', organisationDetails."Id", ccsAccessRoleId;

		RAISE NOTICE '-------------------Role eligible for deletion from group---------------------------';
		FOR groupDetails IN SELECT er."OrganisationUserGroupId", gr."UserGroupName" FROM public."OrganisationGroupEligibleRole" er
					 INNER JOIN public."OrganisationUserGroup" gr ON gr."Id" = er."OrganisationUserGroupId"
					 WHERE er."IsDeleted" = false AND er."OrganisationEligibleRoleId" = organisationEligibleRoleId
		LOOP
			RAISE NOTICE 'Role % being delete from group Id: %, Name: %', ccsAccessRoleNameKey, groupDetails."OrganisationUserGroupId", groupDetails."UserGroupName";
		END LOOP;

		IF (reportingModeOn = 'false') THEN
			UPDATE "OrganisationGroupEligibleRole" SET "IsDeleted" = true, "LastUpdatedOnUtc" = timezone('utc', now()) 
			WHERE "IsDeleted" = false AND "OrganisationEligibleRoleId" = organisationEligibleRoleId;
			GET DIAGNOSTICS groupAffectedCount = ROW_COUNT;
		END IF; 

		RAISE NOTICE '';
		RAISE NOTICE '-------------------Role eligible for deletion from user---------------------------';
		FOR userDetails IN SELECT uar."UserId", u."UserName" FROM public."UserAccessRole" uar
						   INNER JOIN "User" u ON u."Id" = uar."UserId"
						   WHERE uar."IsDeleted" = false AND uar."OrganisationEligibleRoleId" = organisationEligibleRoleId
		LOOP
			RAISE NOTICE 'Role % being delete from user Id: %, Name: %', ccsAccessRoleNameKey, userDetails."UserId", userDetails."UserName";
		END LOOP;

		IF (reportingModeOn = 'false') THEN
			UPDATE "UserAccessRole" SET "IsDeleted" = true, "LastUpdatedOnUtc" = timezone('utc', now()) 
			WHERE "IsDeleted" = false AND "OrganisationEligibleRoleId" = organisationEligibleRoleId;
			GET DIAGNOSTICS userAffectedCount = ROW_COUNT;
		END IF;

		IF (reportingModeOn = 'false') THEN
			UPDATE "OrganisationEligibleRole" SET "IsDeleted" = true, "LastUpdatedOnUtc" = timezone('utc', now()) 
			WHERE "IsDeleted" = false AND "Id" = organisationEligibleRoleId;
	
		RAISE NOTICE '-------------------Role % deleted from OrganisationEligibleRole Id: % -------------------', ccsAccessRoleNameKey, organisationEligibleRoleId;
		END IF;

		RAISE NOTICE '';
		RAISE NOTICE '***********************************************************';
		RAISE NOTICE 'Deleted Role key: %', ccsAccessRoleNameKey;
		RAISE NOTICE 'No of Groups affected: %', groupAffectedCount;
		RAISE NOTICE 'No of Users affected: %', userAffectedCount;
		RAISE NOTICE 'No of Orgs affected: %',orgsAffectedCount;
		RAISE NOTICE '***********************************************************';

	END LOOP;

	IF (reportingModeOn = 'false') THEN
			DELETE FROM "CcsServiceRoleMapping" WHERE "CcsAccessRoleId" = ccsAccessRoleId;
			GET DIAGNOSTICS roleMappingDeleteCount = ROW_COUNT;
			
			IF(roleMappingDeleteCount > 0) THEN
				RAISE NOTICE 'Role mapping deleted from CcsServiceRoleMapping table for role id: %', ccsAccessRoleId;
			END IF;
	END IF;

	IF (reportingModeOn = 'false') THEN
			DELETE FROM "AutoValidationRole" WHERE "CcsAccessRoleId" = ccsAccessRoleId;
			GET DIAGNOSTICS autoValidationRoleDeletedCount = ROW_COUNT;
			
			IF(autoValidationRoleDeletedCount > 0) THEN
				RAISE NOTICE 'Role deleted from AutoValidationRole table for role id: %', ccsAccessRoleId;
			END IF;
	END IF;

	IF (reportingModeOn = 'false') THEN
			UPDATE "RoleApprovalConfiguration"  SET "IsDeleted" = true, "LastUpdatedOnUtc" = timezone('utc', now()) 
			WHERE "CcsAccessRoleId" = ccsAccessRoleId;
			GET DIAGNOSTICS roleConfigDeletedCount = ROW_COUNT;

			IF(roleConfigDeletedCount > 0) THEN
				RAISE NOTICE 'Role approval configuration deleted from RoleApprovalConfiguration table of role Id: %', ccsAccessRoleId;
			END IF;
	END IF;


	IF (reportingModeOn = 'false' AND deleteRoleFromCcsAccessRoleTable = 'true') THEN
			UPDATE "CcsAccessRole"  SET "IsDeleted" = true, "LastUpdatedOnUtc" = timezone('utc', now()) 
			WHERE "Id" = ccsAccessRoleId;
			RAISE NOTICE '~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~';
			RAISE NOTICE 'Role % deleted from CcsAccessRole table Id: %', ccsAccessRoleNameKey, ccsAccessRoleId;
			RAISE NOTICE '~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~';
	END IF;

END LOOP;

	RAISE NOTICE '-------------------Role removal finished/role not exist %.------------------------------------------------', ccsAccessRoleNameKey;
	RAISE NOTICE '';
	RAISE NOTICE '';
RETURN 1;
END;
$$ LANGUAGE plpgsql;


SELECT DEV_SAND_TEST_ENV_ONLY_REMOVE_OLD_UNWANTED_ROLES('ACCESS_JAGGAER');
SELECT DEV_SAND_TEST_ENV_ONLY_REMOVE_OLD_UNWANTED_ROLES('JAGGAER_USER');
SELECT DEV_SAND_TEST_ENV_ONLY_REMOVE_OLD_UNWANTED_ROLES('ACCESS_CAAAC_CLIENT');
SELECT DEV_SAND_TEST_ENV_ONLY_REMOVE_OLD_UNWANTED_ROLES('ACCESS_FP_CLIENT');
SELECT DEV_SAND_TEST_ENV_ONLY_REMOVE_OLD_UNWANTED_ROLES('DIGITS_MI');
SELECT DEV_SAND_TEST_ENV_ONLY_REMOVE_OLD_UNWANTED_ROLES('DIGITS_DEPARTMENT_ADMIN');
SELECT DEV_SAND_TEST_ENV_ONLY_REMOVE_OLD_UNWANTED_ROLES('DIGITS_CONTRACT_OWNER');
SELECT DEV_SAND_TEST_ENV_ONLY_REMOVE_OLD_UNWANTED_ROLES('SERVICE_ADMIN');
SELECT DEV_SAND_TEST_ENV_ONLY_REMOVE_OLD_UNWANTED_ROLES('PROVIDER_APP');
SELECT DEV_SAND_TEST_ENV_ONLY_REMOVE_OLD_UNWANTED_ROLES('ACCESS_DIGITS_CLIENT');
SELECT DEV_SAND_TEST_ENV_ONLY_REMOVE_OLD_UNWANTED_ROLES('ACCESS_DMP');
SELECT DEV_SAND_TEST_ENV_ONLY_REMOVE_OLD_UNWANTED_ROLES('DMP_SUPPLIER');
SELECT DEV_SAND_TEST_ENV_ONLY_REMOVE_OLD_UNWANTED_ROLES('USER');
SELECT DEV_SAND_TEST_ENV_ONLY_REMOVE_OLD_UNWANTED_ROLES('DIGITS_USER');

DROP FUNCTION DEV_SAND_TEST_ENV_ONLY_REMOVE_OLD_UNWANTED_ROLES;