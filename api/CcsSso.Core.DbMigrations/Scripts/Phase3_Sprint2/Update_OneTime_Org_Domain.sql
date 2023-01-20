CREATE OR REPLACE FUNCTION Update_OneTime_Org_Domain() RETURNS integer AS $$
DECLARE orgId int;
DECLARE domainName text;
DECLARE updateSuccessCount int = 0;
DECLARE updateFailedCount int = 0;
DECLARE organisationEligibleAdminRoleId int;

   BEGIN
    ALTER TABLE "Organisation" ADD COLUMN "DomainName" TEXT NULL;

    FOR orgId IN SELECT "Id" FROM "Organisation" WHERE "DomainName" IS NULL AND "IsDeleted" = false
    LOOP 
         --Get org eligible admin role id
         SELECT oe."Id" Into  organisationEligibleAdminRoleId FROM "OrganisationEligibleRole" oe 
         INNER JOIN "CcsAccessRole" c ON c."Id" = oe."CcsAccessRoleId"
         WHERE c."IsDeleted" = false AND oe."IsDeleted" = false AND 
         c."CcsAccessRoleNameKey" = 'ORG_ADMINISTRATOR' AND oe."OrganisationId" = orgId
         LIMIT 1;

         SELECT substr(u."UserName",(strpos(u."UserName", '@') + 1)) INTO domainName 
         FROM "User" u
         INNER JOIN "Person" p ON p."PartyId" = u."PartyId" AND p."IsDeleted" = false
         INNER JOIN "Organisation" o ON p."OrganisationId" = o."Id"  AND o."IsDeleted" = false
         INNER JOIN "UserAccessRole" ur ON ur."UserId" = u."Id"  AND ur."IsDeleted" = false
         WHERE u."IsDeleted" = false AND u."UserType" = 0 
         AND o."Id" = orgId
         AND ur."OrganisationEligibleRoleId" = organisationEligibleAdminRoleId
         ORDER BY u."CreatedOnUtc" ASC 
         LIMIT 1;

        IF(domainName IS NULL) THEN
            RAISE NOTICE 'Domain updated failed for org id: %', orgId;
            updateFailedCount:= updateFailedCount + 1;
        ELSE
            UPDATE "Organisation" SET "DomainName" = domainName WHERE "Id"= orgId;
            updateSuccessCount:= updateSuccessCount + 1;
            RAISE NOTICE 'Domain updated success for org id % with domain: %', orgId, domainName;
        END IF;

    END LOOP;
    RAISE NOTICE 'Domain update success for total % orgs.', updateSuccessCount;
    RAISE NOTICE 'Domain update failed for total % orgs.', updateFailedCount;
    RAISE NOTICE 'Domain update processed for total % orgs.', updateSuccessCount + updateFailedCount;

    RETURN 1;
   END;

$$ LANGUAGE plpgsql;
SELECT Update_OneTime_Org_Domain();
DROP FUNCTION Update_OneTime_Org_Domain;