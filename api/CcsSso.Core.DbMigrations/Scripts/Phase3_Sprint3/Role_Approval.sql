CREATE OR REPLACE FUNCTION Role_Approval() RETURNS integer AS $$
DECLARE fleetDashboardId int;
DECLARE fleetUserId int;

BEGIN

	SELECT "Id" into fleetUserId From "CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'FP_USER' AND "IsDeleted" = false LIMIT 1;
	UPDATE "CcsAccessRole" SET "ApprovalRequired" = 1 WHERE "Id" = fleetUserId;

	IF NOT EXISTS (SELECT "Id" FROM "RoleApprovalConfiguration" WHERE "Id" = fleetUserId) THEN
		INSERT INTO "RoleApprovalConfiguration"("CcsAccessRoleId", "LinkExpiryDurationInMinute", "NotificationEmails", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
		VALUES(fleetUserId, 129600, 'fpnotify@yopmail.com,fpnotify@brickendon.com', 0, 0, now(), now(), false);
	END IF;
	
RETURN 1;
END;

$$ LANGUAGE plpgsql;
SELECT Role_Approval();
DROP FUNCTION Role_Approval;
