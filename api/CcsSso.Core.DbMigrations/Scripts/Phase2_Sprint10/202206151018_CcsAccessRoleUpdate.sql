START TRANSACTION;

UPDATE public."CcsService" 
SET "ServiceName"='eSourcing',
"Description"='The eSourcing tool will help you supply to, or buy for, the public sector, compliantly',
"LastUpdatedOnUtc"=Now()
where "ServiceCode"='JAGGAER';

UPDATE public."CcsAccessRole"
SET "CcsAccessRoleName" = 'Jaggaer Buyer',
"LastUpdatedOnUtc" = Now()
where "CcsAccessRoleNameKey"='JAEGGER_BUYER';

COMMIT;





