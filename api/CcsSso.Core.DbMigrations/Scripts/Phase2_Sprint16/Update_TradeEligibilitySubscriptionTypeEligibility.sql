CREATE OR REPLACE FUNCTION Update_TradeEligibilitySubscriptionTypeEligibility() RETURNS integer AS $$

BEGIN
	update public."CcsAccessRole" set "SubscriptionTypeEligibility"=0 , "TradeEligibility"=0 where "CcsAccessRoleNameKey" ='JAEGGER_SUPPLIER';
	update public."CcsAccessRole" set "SubscriptionTypeEligibility"=0 , "TradeEligibility"=1 where "CcsAccessRoleNameKey" ='JAEGGER_BUYER';
	update public."CcsAccessRole" set "SubscriptionTypeEligibility"=1 , "TradeEligibility"=2 where "CcsAccessRoleNameKey" ='ACCESS_EVIDENCE_LOCKER';
	update public."CcsAccessRole" set "SubscriptionTypeEligibility"=1 , "TradeEligibility"=0 where "CcsAccessRoleNameKey" ='EL_JNR_SUPPLIER';
	update public."CcsAccessRole" set "SubscriptionTypeEligibility"=1 , "TradeEligibility"=0 where "CcsAccessRoleNameKey" ='EL_SNR_SUPPLIER';
	update public."CcsAccessRole" set "SubscriptionTypeEligibility"=0 , "TradeEligibility"=1 where "CcsAccessRoleNameKey" ='ACCESS_CAAAC_CLIENT';
	update public."CcsAccessRole" set "SubscriptionTypeEligibility"=0 , "TradeEligibility"=1 where "CcsAccessRoleNameKey" ='CAT_USER';
	update public."CcsAccessRole" set "SubscriptionTypeEligibility"=0 , "TradeEligibility"=1 where "CcsAccessRoleNameKey" ='FP_USER';
	update public."CcsAccessRole" set "SubscriptionTypeEligibility"=0 , "TradeEligibility"=1 where "CcsAccessRoleNameKey" ='ACCESS_FP_CLIENT';

RETURN 1;
END;

$$ LANGUAGE plpgsql;
SELECT Update_TradeEligibilitySubscriptionTypeEligibility();
DROP FUNCTION Update_TradeEligibilitySubscriptionTypeEligibility;
