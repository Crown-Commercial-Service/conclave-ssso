START TRANSACTION;

UPDATE public."CcsAccessRole" 
SET "SubscriptionTypeEligibility"=0,
"LastUpdatedOnUtc"=Now()
where "CcsAccessRoleNameKey"='EL_JNR_SUPPLIER';

UPDATE public."CcsAccessRole" 
SET "SubscriptionTypeEligibility"=0,
"LastUpdatedOnUtc"=Now()
where "CcsAccessRoleNameKey"='EL_SNR_SUPPLIER';

UPDATE public."CcsAccessRole" 
SET "TradeEligibility"=2,
"SubscriptionTypeEligibility"=1,
"LastUpdatedOnUtc"=Now()
where "CcsAccessRoleNameKey"='JAGGAER_TMP';


COMMIT;

