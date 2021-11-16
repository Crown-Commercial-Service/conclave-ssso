CREATE OR REPLACE FUNCTION migrate_user_idps() RETURNS integer AS $$
	
  	DECLARE u record;

    BEGIN
      FOR u in SELECT "Id" as userId, "OrganisationEligibleIdentityProviderId" as idpId
             FROM "User" 
             WHERE "IsDeleted"=false
      LOOP 
        INSERT INTO public."UserIdentityProvider"(
	        "OrganisationEligibleIdentityProviderId", "UserId", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	        VALUES (u.idpId, u.userId, 0, 0, now(), now(), false);
      END LOOP;
    RETURN 1;
	END;
$$ LANGUAGE plpgsql;

SELECT setval('"UserIdentityProvider_Id_seq"', max("Id")) FROM "UserIdentityProvider";
SELECT migrate_user_idps();
DROP FUNCTION migrate_user_idps;
