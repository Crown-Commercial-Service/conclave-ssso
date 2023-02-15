START TRANSACTION;


CREATE TABLE "CcsServiceRoleGroup" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "Key" text NULL,
    "Name" text NULL,
    "Description" text NULL,
    "OrgTypeEligibility" integer NOT NULL,
    "SubscriptionTypeEligibility" integer NOT NULL,
    "TradeEligibility" integer NOT NULL,
    "MfaEnabled" boolean NOT NULL,
    "DefaultEligibility" text NULL,
    "ApprovalRequired" integer NOT NULL,
    "CreatedUserId" integer NOT NULL,
    "LastUpdatedUserId" integer NOT NULL,
    "CreatedOnUtc" timestamp without time zone NOT NULL,
    "LastUpdatedOnUtc" timestamp without time zone NOT NULL,
    "IsDeleted" boolean NOT NULL,
    "ConcurrencyKey" bytea NULL,
    CONSTRAINT "PK_CcsServiceRoleGroup" PRIMARY KEY ("Id")
);

COMMENT ON TABLE public."CcsServiceRoleGroup"
    IS 'This parent table holds the service role group name and its properties. It will be shared with the group of roles';


CREATE TABLE "CcsServiceRoleMapping" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "CcsServiceRoleGroupId" integer NOT NULL,
    "CcsAccessRoleId" integer NOT NULL,
    CONSTRAINT "PK_CcsServiceRoleMapping" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CcsServiceRoleMapping_CcsAccessRole_CcsAccessRoleId" FOREIGN KEY ("CcsAccessRoleId") REFERENCES "CcsAccessRole" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CcsServiceRoleMapping_CcsServiceRoleGroup_CcsServiceRoleGro~" FOREIGN KEY ("CcsServiceRoleGroupId") REFERENCES "CcsServiceRoleGroup" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_CcsServiceRoleMapping_CcsAccessRoleId" ON "CcsServiceRoleMapping" ("CcsAccessRoleId");

CREATE INDEX "IX_CcsServiceRoleMapping_CcsServiceRoleGroupId" ON "CcsServiceRoleMapping" ("CcsServiceRoleGroupId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230215154838_Add_CcsServiceRoleGroup_CcsServiceRoleMapping', '5.0.10');


COMMENT ON TABLE public."CcsServiceRoleMapping"
    IS 'This child table holds list of roles for each service role group . Parent table : CcsServiceRoleGroup';

COMMIT;

