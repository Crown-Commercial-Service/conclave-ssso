
UPDATE "CcsServiceRoleGroup" set "Key"= 'MANAGE_SUBSCRIPTIONS' WHERE "Key" = 'MANAGE_SUBSCRIPTIONS_GROUP';

UPDATE "CcsServiceRoleGroup" set "Key"= 'CAT_USER',  "Description"='Find and contract suitable suppliers for your procurement project, and ask them about the services they can provide. Progress to one stage further competition.'
WHERE "Key" in ('CAS_USER_GROUP','CAT_USER_GROUP','CAS_USER','CAT_USER','CAS_GROUP');

UPDATE "CcsServiceRoleGroup" set "Key"= 'FP_USER', 
"Description"='A self-service system where customers can obtain live pricing quotes for either purchasing or leasing standard build cars and light commercial vehicles.'
WHERE "Key" in('FP_USER_GROUP','FP_USER');

UPDATE "CcsServiceRoleGroup" set "Key"= 'JAEGGER_BUYER', 
"Description"='The eSourcing tool will help you supply to, or buy for, the public sector, compliantly.'
WHERE "Key" in('JAEGGER_BUYER_GROUP','JAEGGER_BUYER');

UPDATE "CcsServiceRoleGroup" set "Key"= 'JAEGGER_SUPPLIER'  ,
"Description"='The eSourcing tool will help you supply to, or buy for, the public sector, compliantly.'
WHERE "Key" in ('JAEGGER_SUPPLIER_GROUP','JAEGGER_SUPPLIER');

UPDATE "CcsServiceRoleGroup" set "Key"= 'ORG_ADMINISTRATOR' WHERE "Key" = 'ORG_ADMINISTRATOR_GROUP';
UPDATE "CcsServiceRoleGroup" set "Key"= 'ORG_DEFAULT_USER' WHERE "Key" = 'ORG_DEFAULT_USER_GROUP';
UPDATE "CcsServiceRoleGroup" set "Key"= 'ORG_USER_SUPPORT' WHERE "Key" = 'ORG_USER_SUPPORT_GROUP';