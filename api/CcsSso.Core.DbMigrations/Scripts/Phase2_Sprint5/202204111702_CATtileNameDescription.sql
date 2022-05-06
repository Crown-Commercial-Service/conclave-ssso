START TRANSACTION;

UPDATE public."CcsService" 
SET "ServiceName"='Contract Award Service',
"Description" = 'Find and contact suitable suppliers for your procurement project, and ask them about the services they can provide. Progress to one stage further competition.',
"LastUpdatedOnUtc"=Now()
WHERE "ServiceName"='Create and award a contract';

COMMIT;

