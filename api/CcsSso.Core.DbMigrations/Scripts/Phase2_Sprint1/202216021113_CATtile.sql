START TRANSACTION;

UPDATE public."CcsService" 
SET "ServiceName"='Create and award a contract',
"Description" = 'Find and contact suitable suppliers for your procurement project, and ask them about the services they can provide. Start a capability assessment, set your requirements and evaluation criteria, and then take suppliers through to direct award or further competition. You can choose to do it all online or submit documents for some sections.',
"LastUpdatedOnUtc"=Now()
WHERE "ServiceName"='Cat';

COMMIT;

