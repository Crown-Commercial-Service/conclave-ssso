START TRANSACTION;

UPDATE public."CcsAccessRole" 
SET "IsDeleted"=true,
"SubscriptionTypeEligibility"=1,
"LastUpdatedOnUtc"=Now()
where "CcsAccessRoleName" ='Access Test Client';


COMMIT;