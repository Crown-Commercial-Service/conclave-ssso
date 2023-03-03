﻿START TRANSACTION;

CREATE TABLE "AutoValidationRole" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "CcsAccessRoleId" integer NOT NULL,
    "IsSupplier" boolean NOT NULL,
    "IsBuyerSuccess" boolean NOT NULL,
    "IsBuyerFailed" boolean NOT NULL,
    "IsBothSuccess" boolean NOT NULL,
    "IsBothFailed" boolean NOT NULL,
    "AssignToOrg" boolean NOT NULL,
    "AssignToAdmin" boolean NOT NULL,
    CONSTRAINT "PK_AutoValidationRole" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AutoValidationRole_CcsAccessRole_CcsAccessRoleId" FOREIGN KEY ("CcsAccessRoleId") REFERENCES "CcsAccessRole" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AutoValidationRole_CcsAccessRoleId" ON "AutoValidationRole" ("CcsAccessRoleId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20221101092341_Add_Table_AutoValidationRole', '5.0.10');

COMMIT;

