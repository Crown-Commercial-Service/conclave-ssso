

CREATE OR REPLACE FUNCTION AddGroupRoleMapping(
		CcsServiceRoleGroupKey varchar(200),
		CcsAccessRoleNameKey varchar(200),
		CcsAccessRoleName text
	) RETURNS integer AS $$

DECLARE roleId int;
DECLARE roleGroupId int;

begin

select "Id" into roleId from "CcsAccessRole" where "CcsAccessRoleNameKey"=CcsAccessRoleNameKey and "CcsAccessRoleName"=CcsAccessRoleName;
select "Id" into roleGroupId from "CcsServiceRoleGroup" where "Key"=CcsServiceRoleGroupKey;
   
if (roleId is null or roleGroupId is null) then
	raise notice 'No role or group found';
	return 1;
end if; 

	

	  IF NOT EXISTS (SELECT "Id" FROM public."CcsServiceRoleMapping" WHERE "CcsServiceRoleGroupId" = roleGroupId and "CcsAccessRoleId" = roleId LIMIT 1) THEN
		  raise notice 'Adding group role mapping %',CcsAccessRoleNameKey;
			INSERT INTO public."CcsServiceRoleMapping"("CcsServiceRoleGroupId", "CcsAccessRoleId")
			VALUES (roleGroupId,roleId );
		ELSE
			raise notice 'Already Mapping exists role name key %',CcsAccessRoleNameKey;
   	END IF;
	

	RETURN 1;
	END;
$$ LANGUAGE plpgsql;


SELECT AddGroupRoleMapping('EL_USER','EL_USER','Buyer Supplier Information');
SELECT AddGroupRoleMapping('EL_SNR_BUYER','EL_SNR_BUYER','Snr Buyer');
SELECT AddGroupRoleMapping('EL_JNR_BUYER','EL_JNR_BUYER','Jnr Buyer');
SELECT AddGroupRoleMapping('EL_CCS_SNR_ADMIN','EL_CCS_SNR_ADMIN','CCS Snr Admin');
SELECT AddGroupRoleMapping('EL_CCS_JNR_ADMIN','EL_CCS_JNR_ADMIN','CCS Jnr Admin');
SELECT AddGroupRoleMapping('EL_JNR_SUPPLIER','EL_JNR_SUPPLIER','Jnr Supplier');
SELECT AddGroupRoleMapping('EL_SNR_SUPPLIER','EL_SNR_SUPPLIER','Snr Supplier');

SELECT AddGroupRoleMapping('TEST_SAML_CLIENT_USER','TEST_SAML_CLIENT_USER','SAML Client Tile');
SELECT AddGroupRoleMapping('TEST_SAML_CLIENT_USER','TEST_SAML_CLIENT_USER','SAML Client User');

SELECT AddGroupRoleMapping('TEST_SSO_CLIENT_USER','TEST_SSO_CLIENT_USER','SSO Client Tile');
SELECT AddGroupRoleMapping('TEST_SSO_CLIENT_USER','TEST_SSO_CLIENT_USER','Test SSO Client User');

SELECT AddGroupRoleMapping('DMP_SUPPLIER','DMP_SUPPLIER','DMP Supplier');
SELECT AddGroupRoleMapping('DMP_SUPPLIER','DMP_SUPPLIER','Access DMP');

SELECT AddGroupRoleMapping('DATA_MIGRATION','DATA_MIGRATION','Access Data Migration');
SELECT AddGroupRoleMapping('DATA_MIGRATION','DATA_MIGRATION','Data Migration');


SELECT AddGroupRoleMapping('DigiTS_GROUP','DigiTS_USER','Access DigiTS');
SELECT AddGroupRoleMapping('DigiTS_GROUP','DIGITS_DEPARTMENT_ADMIN','Department Admin');
SELECT AddGroupRoleMapping('DigiTS_GROUP','DIGITS_CONTRACT_OWNER','Contract Owner');
SELECT AddGroupRoleMapping('DigiTS_GROUP','DIGITS_MI','MI');
SELECT AddGroupRoleMapping('DigiTS_GROUP','USER','User');
SELECT AddGroupRoleMapping('DigiTS_GROUP','SERVICE_ADMIN','Service Admin');
SELECT AddGroupRoleMapping('DigiTS_GROUP','PROVIDER_APP','API Access Role');

SELECT AddGroupRoleMapping('RMI','RMI_USER','RMI Tile');
SELECT AddGroupRoleMapping('RMI','RMI_USER','RMI User');


DROP FUNCTION AddGroupRoleMapping;