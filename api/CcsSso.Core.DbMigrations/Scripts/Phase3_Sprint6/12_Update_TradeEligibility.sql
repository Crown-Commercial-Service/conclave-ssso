START TRANSACTION;

UPDATE "CcsServiceRoleGroup" set "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=0, "TradeEligibility"=1
WHERE "Key" ='CAT_USER'

UPDATE "CcsServiceRoleGroup" set "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=0, "TradeEligibility"=2
WHERE "Key" ='JAEGGER_BUYER'

UPDATE "CcsServiceRoleGroup" set "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=0, "TradeEligibility"=2
WHERE "Key" ='JAEGGER_SUPPLIER'

UPDATE "CcsServiceRoleGroup" set "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=0, "TradeEligibility"=1
WHERE "Key" ='FP_USER'

UPDATE "CcsServiceRoleGroup" set "DisplayOrder"=7
WHERE "Key" ='MANAGE_SUBSCRIPTIONS'

UPDATE "CcsServiceRoleGroup" set "DisplayOrder"=8
WHERE "Key" ='ORG_USER_SUPPORT'

COMMIT;