START TRANSACTION;

UPDATE public."CcsService" 
SET
"Description" = 'Vehicle lease and purchase',
"LastUpdatedOnUtc"=Now()
WHERE "ServiceName"='Fleet Portal';

COMMIT;
