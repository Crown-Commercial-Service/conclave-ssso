START TRANSACTION;

-- Service Role Group
UPDATE "CcsServiceRoleGroup" set "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=1
WHERE "Key" ='CAT_USER';

UPDATE "CcsServiceRoleGroup" set "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=1
WHERE "Key" ='JAEGGER_BUYER';

UPDATE "CcsServiceRoleGroup" set "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=0, "TradeEligibility"=0
WHERE "Key" ='JAEGGER_SUPPLIER';

UPDATE "CcsServiceRoleGroup" set "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=1
WHERE "Key" ='FP_USER';

-- CCS Access Role

UPDATE "CcsAccessRole" set "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=1
WHERE "CcsAccessRoleNameKey" ='CAT_USER';

UPDATE "CcsAccessRole" set "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=1
WHERE "CcsAccessRoleNameKey" ='JAEGGER_BUYER';

UPDATE "CcsAccessRole" set "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=0, "TradeEligibility"=0
WHERE "CcsAccessRoleNameKey" ='JAEGGER_SUPPLIER';

UPDATE "CcsAccessRole" set "OrgTypeEligibility"=2, "SubscriptionTypeEligibility"=1, "TradeEligibility"=1
WHERE "CcsAccessRoleNameKey" ='FP_USER';

-- Update Display order in service group
UPDATE "CcsServiceRoleGroup" set "DisplayOrder"=1
WHERE "Key" ='CAT_USER';

UPDATE "CcsServiceRoleGroup" set "DisplayOrder"=2
WHERE "Key" ='JAEGGER_BUYER';


UPDATE "CcsServiceRoleGroup" set "DisplayOrder"=3
WHERE "Key" ='JAEGGER_SUPPLIER';

UPDATE "CcsServiceRoleGroup" set "DisplayOrder"=4
WHERE "Key" ='FP_USER';

UPDATE "CcsServiceRoleGroup" set "DisplayOrder"=5
WHERE "Key" ='ORG_ADMINISTRATOR';

UPDATE "CcsServiceRoleGroup" set "DisplayOrder"=6
WHERE "Key" ='ORG_DEFAULT_USER';

UPDATE "CcsServiceRoleGroup" set "DisplayOrder"=7
WHERE "Key" ='MANAGE_SUBSCRIPTIONS';

UPDATE "CcsServiceRoleGroup" set "DisplayOrder"=8
WHERE "Key" ='ORG_USER_SUPPORT';


-- remove description from the ServiceGroup
UPDATE "CcsServiceRoleGroup" set "Description"=''
WHERE "Key" ='RMI';
UPDATE "CcsServiceRoleGroup" set "Description"=''
WHERE "Key" ='DigiTS_GROUP';
UPDATE "CcsServiceRoleGroup" set "Description"=''
WHERE "Key" ='DATA_MIGRATION';
UPDATE "CcsServiceRoleGroup" set "Description"=''
WHERE "Key" ='EL_USER';
UPDATE "CcsServiceRoleGroup" set "Description"=''
WHERE "Key" ='EL_SNR_BUYER';
UPDATE "CcsServiceRoleGroup" set "Description"=''
WHERE "Key" ='EL_JNR_BUYER';

UPDATE "CcsServiceRoleGroup" set "Description"=''
WHERE "Key" ='EL_CCS_SNR_ADMIN';
UPDATE "CcsServiceRoleGroup" set "Description"=''
WHERE "Key" ='EL_CCS_JNR_ADMIN';
UPDATE "CcsServiceRoleGroup" set "Description"=''
WHERE "Key" ='EL_JNR_SUPPLIER';
UPDATE "CcsServiceRoleGroup" set "Description"=''
WHERE "Key" ='EL_SNR_SUPPLIER';
UPDATE "CcsServiceRoleGroup" set "Description"=''
WHERE "Key" ='TEST_SAML_CLIENT_USER';
UPDATE "CcsServiceRoleGroup" set "Description"=''
WHERE "Key" ='TEST_SSO_CLIENT_USER';
UPDATE "CcsServiceRoleGroup" set "Description"=''
WHERE "Key" ='DMP_SUPPLIER';

COMMIT;

