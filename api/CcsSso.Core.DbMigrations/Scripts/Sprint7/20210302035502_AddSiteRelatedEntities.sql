START TRANSACTION;

ALTER TABLE "ContactPoint" ADD "IsSite" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "ContactPoint" ADD "SiteName" text NULL;

CREATE TABLE "SiteContact" (
    "Id" integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
    "ContactPointId" integer NOT NULL,
    "ContactId" integer NOT NULL,
    "CreatedPartyId" integer NOT NULL,
    "LastUpdatedPartyId" integer NOT NULL,
    "CreatedOnUtc" timestamp without time zone NOT NULL,
    "LastUpdatedOnUtc" timestamp without time zone NOT NULL,
    "IsDeleted" boolean NOT NULL,
    "ConcurrencyKey" bytea NULL,
    CONSTRAINT "PK_SiteContact" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SiteContact_ContactPoint_ContactPointId" FOREIGN KEY ("ContactPointId") REFERENCES "ContactPoint" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_SiteContact_ContactPointId" ON "SiteContact" ("ContactPointId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210302035502_AddSiteRelatedEntities', '5.0.2');

COMMIT;

