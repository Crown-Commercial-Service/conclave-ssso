UPDATE "CcsServiceRoleGroup" SET 
"Description" = 'Find and contact suitable suppliers for your procurement project, and ask them about the services they can provide. Progress to one stage further competition.'
WHERE "Key" = 'CAT_USER';

UPDATE "CcsService" SET "Description"='Find and contact suitable suppliers for your procurement project, and ask them about the services they can provide. Progress to one stage further competition.' 
WHERE "ServiceCode" ='CAT_USER_DS';
