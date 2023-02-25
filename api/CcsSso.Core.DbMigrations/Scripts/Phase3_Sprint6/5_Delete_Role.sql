
START TRANSACTION;

UPDATE "CcsAccessRole" set "IsDeleted"= true WHERE "CcsAccessRoleNameKey" in ('ACCESS_EVIDENCE_LOCKER','ACCESS_CAAAC_CLIENT','ACCESS_JAGGAER','JAGGAER_USER','ACCESS_FP_CLIENT',
'ACCESS_TEST_SAML_CLIENT','ACCESS_TEST_SSO_CLIENT','ACCESS_DMP','ACCESS_DM_CLIENT','ACCESS_DIGITS_CLIENT','ACCESS_RMI_CLIENT');

UPDATE "CcsAccessRole" set "IsDeleted"= false WHERE "CcsAccessRoleNameKey" in ('TEST_SAML_CLIENT_USER');



COMMIT;

