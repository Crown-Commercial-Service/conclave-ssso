START TRANSACTION;

UPDATE public."CcsService" 
SET "ServiceName"='Buyer/Supplier Information',
"LastUpdatedOnUtc"=Now()
WHERE "ServiceName"='Evidence Locker';

COMMIT;

