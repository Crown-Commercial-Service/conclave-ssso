
-- reporting mode - Buyer org having the supplier role
select org."Id", org."CiiOrganisationId",org."SupplierBuyerType",org."LegalName", ar."CcsAccessRoleNameKey" 
from  "OrganisationEligibleRole" oer 
inner join "CcsAccessRole" ar on ar."Id"=oer."CcsAccessRoleId"
inner join "Organisation" org on org."Id"=oer."OrganisationId" and org."IsDeleted"=false
where org."SupplierBuyerType"=1 and oer."IsDeleted"=false and ar."CcsAccessRoleNameKey" in ('FP_USER', 'ACCESS_FP_CLIENT')
order by org."CiiOrganisationId"


-- UPDATE THE BUYER ORG TO BOTH 

UPDATE "Organisation" SET "SupplierBuyerType"=2
 WHERE "Id" in (
	select distinct org."Id" 
	from  "OrganisationEligibleRole" oer 
	inner join "CcsAccessRole" ar on ar."Id"=oer."CcsAccessRoleId"
	inner join "Organisation" org on org."Id"=oer."OrganisationId" and org."IsDeleted"=false
	where org."SupplierBuyerType"=1 and oer."IsDeleted"=false and ar."CcsAccessRoleNameKey" in ('FP_USER', 'ACCESS_FP_CLIENT')
	--and org."Id" in (79,48)
	 order by org."Id"
 )
