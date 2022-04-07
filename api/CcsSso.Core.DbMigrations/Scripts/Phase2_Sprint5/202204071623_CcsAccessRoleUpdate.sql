START TRANSACTION;

UPDATE public."CcsAccessRole" 
SET "SubscriptionTypeEligibility"=0,
"LastUpdatedOnUtc"=Now()
where "CcsAccessRoleNameKey"='EL_JNR_SUPPLIER';

UPDATE public."CcsAccessRole" 
SET "SubscriptionTypeEligibility"=0,
"LastUpdatedOnUtc"=Now()
where "CcsAccessRoleNameKey"='EL_SNR_SUPPLIER';

COMMIT;

