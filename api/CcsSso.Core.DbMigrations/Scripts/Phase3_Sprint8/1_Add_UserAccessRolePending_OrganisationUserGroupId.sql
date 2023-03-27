START TRANSACTION;

ALTER TABLE "UserAccessRolePending" ADD "OrganisationUserGroupId" integer NULL;

CREATE INDEX "IX_UserAccessRolePending_OrganisationUserGroupId" ON "UserAccessRolePending" ("OrganisationUserGroupId");

ALTER TABLE "UserAccessRolePending" ADD CONSTRAINT "FK_UserAccessRolePending_OrganisationUserGroup_OrganisationUse~" FOREIGN KEY ("OrganisationUserGroupId") REFERENCES "OrganisationUserGroup" ("Id") ON DELETE RESTRICT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230327094219_Add_UserAccessRolePending_OrganisationUserGroupId', '5.0.10');

COMMIT;

