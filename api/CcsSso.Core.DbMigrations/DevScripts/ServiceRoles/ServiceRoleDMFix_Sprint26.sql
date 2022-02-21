UPDATE public."CcsService"
	SET "ServiceCode"='DM_CLIENT'
	WHERE "ServiceCode"='DM';

UPDATE public."CcsAccessRole"
	SET "CcsAccessRoleNameKey"='ACCESS_DM_CLIENT'
	WHERE "CcsAccessRoleNameKey"='ACCESS_DM';

UPDATE public."ServicePermission"
	SET "ServicePermissionName"='ACCESS_DM_CLIENT'
	WHERE "ServicePermissionName"='ACCESS_DM_PERMISSION';

UPDATE public."CcsAccessRole"
	SET "CcsAccessRoleNameKey"='DATA_MIGRATION', "CcsAccessRoleName"='Data Migration', "CcsAccessRoleDescription"='Data Migration'
	WHERE "CcsAccessRoleNameKey"='DM_ADMIN';

UPDATE public."ServicePermission"
	SET "ServicePermissionName"='DATA_MIGRATION_PERMISSION'
	WHERE "ServicePermissionName"='DM_ADMIN_PERMISSION';
