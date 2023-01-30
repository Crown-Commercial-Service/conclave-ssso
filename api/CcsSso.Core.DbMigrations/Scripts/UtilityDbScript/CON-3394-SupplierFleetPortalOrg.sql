
--Supplier Org that has the Fleet Portal roles assigned
select org."CiiOrganisationId",org."LegalName", ar."CcsAccessRoleNameKey" 
from  "OrganisationEligibleRole" oer 
inner join "CcsAccessRole" ar on ar."Id"=oer."CcsAccessRoleId"
inner join "Organisation" org on org."Id"=oer."OrganisationId"
where org."SupplierBuyerType"=0 and ar."CcsAccessRoleNameKey" in ('FP_USER', 'ACCESS_FP_CLIENT')
Group by org."CiiOrganisationId",org."LegalName",ar."CcsAccessRoleNameKey"


--If any Org users under this Organisation got these roles
select org."CiiOrganisationId",org."LegalName", uar."UserId",usr."UserName", ar."CcsAccessRoleNameKey" from "UserAccessRole" uar
inner join "OrganisationEligibleRole" oer on oer."Id"=uar."OrganisationEligibleRoleId" 
inner join "CcsAccessRole" ar on ar."Id"=oer."CcsAccessRoleId"
inner join "Organisation" org on org."Id"=oer."OrganisationId"
left join "User" usr on usr."Id"= uar."UserId"
where org."SupplierBuyerType"=0 and ar."CcsAccessRoleNameKey" in ('FP_USER', 'ACCESS_FP_CLIENT')
Group by org."CiiOrganisationId",org."LegalName",uar."UserId",usr."UserName", ar."CcsAccessRoleNameKey"
