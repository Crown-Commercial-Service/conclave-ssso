Create or replace procedure SP_ManageRole(
   CcsAccessRoleNameKey varchar(200),
   DisableRole boolean
)
language plpgsql    
as $$
DECLARE AccessRoleNameKeyID integer;
DECLARE LegalNameText varchar(500);
DECLARE OrgId integer;
DECLARE roleItems RECORD;
DECLARE DisableRoleText varchar(5);  

begin

	if(DisableRole='t') then
		DisableRoleText= 'true';
	else 
		DisableRoleText= 'false';
	end if; 
	 
   select "Id" into AccessRoleNameKeyID from "CcsAccessRole" where "CcsAccessRoleNameKey"=CcsAccessRoleNameKey;
      RAISE NOTICE 'Access RoleNameKey ID % ', AccessRoleNameKeyID;
      RAISE NOTICE 'Access RoleNameKey ID %', CcsAccessRoleNameKey;

   FOR roleItems IN SELECT "Id","OrganisationId" FROM public."OrganisationEligibleRole" where  "CcsAccessRoleId"=AccessRoleNameKeyID
      LOOP
      
	  RAISE NOTICE 'DisableRole %',DisableRole;
	  RAISE NOTICE 'DisableRoleText %',DisableRoleText;
	  
	  
	  UPDATE "OrganisationEligibleRole" 
      SET "IsDeleted" =CAST(DisableRoleText as bool),
	  "LastUpdatedOnUtc"=timezone('UTC',now())
      WHERE "Id" = roleItems."Id";
		
	  RAISE NOTICE 'Id %',roleItems."Id";
	  RAISE NOTICE 'OrganisationId %',roleItems."OrganisationId";

	  select "LegalName" into LegalNameText from public."Organisation" where "Id"=roleItems."OrganisationId";
	  RAISE NOTICE 'LegalName %',LegalNameText;
	  
	  UPDATE "UserAccessRole" 
      SET "IsDeleted" =CAST(DisableRoleText as bool),
	  "LastUpdatedOnUtc"=timezone('UTC',now())
      WHERE "OrganisationEligibleRoleId" = roleItems."Id";

   END LOOP;
   
    UPDATE "CcsAccessRole" 
    SET "IsDeleted" =CAST(DisableRoleText as bool),
	"LastUpdatedOnUtc"=timezone('UTC',now())
    WHERE "Id" = AccessRoleNameKeyID;
	  
   commit;
end;$$


call SP_ManageRole('ACCESS_TEST_SAML_CLIENT',true)
DROP PROCEDURE sp_removerolename