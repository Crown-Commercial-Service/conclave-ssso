START TRANSACTION;

UPDATE "CcsServiceRoleGroup" SET "Description"='This service will help you supply to, or buy for, the public sector compliantly using CCS commercial agreements.' 
WHERE "Key" ='JAEGGER_BUYER';

UPDATE "CcsServiceRoleGroup" SET "Description"='This service will help you supply to, or buy for, the public sector compliantly using CCS commercial agreements.' 
WHERE "Key" ='JAEGGER_SUPPLIER';

UPDATE "CcsService" SET "Description"='This service will help you supply to, or buy for, the public sector compliantly using CCS commercial agreements.' 
WHERE "ServiceCode" ='JAEGGER_SUPPLIER_DS';

UPDATE "CcsService" SET "Description"='This service will help you supply to, or buy for, the public sector compliantly using CCS commercial agreements.' 
WHERE "ServiceCode" ='JAEGGER_BUYER_DS';

UPDATE "CcsService" SET "Description"='This service will help you supply to, or buy for, the public sector compliantly using CCS commercial agreements.' 
WHERE "ServiceCode" ='JAGGAER';

COMMIT;