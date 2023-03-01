-- These roles are added using the previous assumption (8_AddAutoValidationRoleMapping.sql) Where Autovalidation buyer/supplier/both information is wrong 

START TRANSACTION;
DELETE from "AutoValidationRole" where "CcsAccessRoleId" = (SELECT "Id" from  "CcsAccessRole" where "CcsAccessRoleNameKey"='JAEGGER_BUYER' and "CcsAccessRoleName"='eSourcing Tile for Buyer User');
DELETE from "AutoValidationRole" where "CcsAccessRoleId" =(SELECT "Id" from  "CcsAccessRole" where "CcsAccessRoleNameKey"='JAEGGER_BUYER' and "CcsAccessRoleName"='eSourcing buyer role to access Jagger');
DELETE from "AutoValidationRole" where "CcsAccessRoleId" =(SELECT "Id" from  "CcsAccessRole" where "CcsAccessRoleNameKey"='FP_USER' and "CcsAccessRoleName"='Fleet Portal Tile');
DELETE from "AutoValidationRole" where "CcsAccessRoleId" =(SELECT "Id" from  "CcsAccessRole" where "CcsAccessRoleNameKey"='CAS_USER' and "CcsAccessRoleName"='Contract Award Service (CAS) - add to dashboard');
COMMIT;