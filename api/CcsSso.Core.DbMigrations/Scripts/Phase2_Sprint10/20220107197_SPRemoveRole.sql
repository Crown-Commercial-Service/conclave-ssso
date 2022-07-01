START TRANSACTION;

create or replace procedure SP_RemoveRoleName(
   CcsAccessRoleNameKey varchar(200),
   RoleEnable bool
)
language plpgsql    
as $$
DECLARE AccessRoleNameKeyID integer;
items RECORD;
begin
   select "Id" into AccessRoleNameKeyID from "CcsAccessRole" where "CcsAccessRoleNameKey"=CcsAccessRoleNameKey;
   RAISE NOTICE 'Access RoleNameKey ID %d ', AccessRoleNameKeyID;
   
   FOR items IN SELECT "Id" FROM public."OrganisationEligibleRole" where "IsDeleted"='false' AND "CcsAccessRoleId"=AccessRoleNameKeyID
      LOOP
      
	  UPDATE "OrganisationEligibleRole" 
      SET "IsDeleted" =RoleEnable,
	  "LastUpdatedOnUtc"=NOW()
      WHERE "Id" = items."Id";
	  
      RAISE NOTICE 'Id %',items;
   END LOOP;
	
   commit;
end;$$


OMMIT;





