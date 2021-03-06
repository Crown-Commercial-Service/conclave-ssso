CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "AdapterConsumer" (
    "Id" integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
    "Name" text NULL,
    "ConsumerKey" text NULL,
    "CreatedOnUtc" timestamp without time zone NOT NULL,
    "LastUpdatedOnUtc" timestamp without time zone NOT NULL,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_AdapterConsumer" PRIMARY KEY ("Id")
);

CREATE TABLE "AdapterFormat" (
    "Id" integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
    "FomatFileType" text NULL,
    "CreatedOnUtc" timestamp without time zone NOT NULL,
    "LastUpdatedOnUtc" timestamp without time zone NOT NULL,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_AdapterFormat" PRIMARY KEY ("Id")
);

CREATE TABLE "ConclaveEntity" (
    "Id" integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
    "Name" text NULL,
    "CreatedOnUtc" timestamp without time zone NOT NULL,
    "LastUpdatedOnUtc" timestamp without time zone NOT NULL,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_ConclaveEntity" PRIMARY KEY ("Id")
);

CREATE TABLE "AdapterConsumerEntity" (
    "Id" integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
    "Name" text NULL,
    "AdapterConsumerId" integer NOT NULL,
    "CreatedOnUtc" timestamp without time zone NOT NULL,
    "LastUpdatedOnUtc" timestamp without time zone NOT NULL,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_AdapterConsumerEntity" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AdapterConsumerEntity_AdapterConsumer_AdapterConsumerId" FOREIGN KEY ("AdapterConsumerId") REFERENCES "AdapterConsumer" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AdapterSubscription" (
    "Id" integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
    "SubscriptionType" text NULL,
    "SubscriptionUrl" text NULL,
    "AdapterConsumerId" integer NOT NULL,
    "ConclaveEntityId" integer NOT NULL,
    "CreatedOnUtc" timestamp without time zone NOT NULL,
    "LastUpdatedOnUtc" timestamp without time zone NOT NULL,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_AdapterSubscription" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AdapterSubscription_AdapterConsumer_AdapterConsumerId" FOREIGN KEY ("AdapterConsumerId") REFERENCES "AdapterConsumer" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AdapterSubscription_ConclaveEntity_ConclaveEntityId" FOREIGN KEY ("ConclaveEntityId") REFERENCES "ConclaveEntity" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ConclaveEntityAttribute" (
    "Id" integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
    "AttributeName" text NULL,
    "ConclaveEntityId" integer NOT NULL,
    "CreatedOnUtc" timestamp without time zone NOT NULL,
    "LastUpdatedOnUtc" timestamp without time zone NOT NULL,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_ConclaveEntityAttribute" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ConclaveEntityAttribute_ConclaveEntity_ConclaveEntityId" FOREIGN KEY ("ConclaveEntityId") REFERENCES "ConclaveEntity" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AdapterConsumerEntityAttribute" (
    "Id" integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
    "AttributeName" text NULL,
    "AdapterConsumerEntityId" integer NOT NULL,
    "CreatedOnUtc" timestamp without time zone NOT NULL,
    "LastUpdatedOnUtc" timestamp without time zone NOT NULL,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_AdapterConsumerEntityAttribute" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AdapterConsumerEntityAttribute_AdapterConsumerEntity_Adapte~" FOREIGN KEY ("AdapterConsumerEntityId") REFERENCES "AdapterConsumerEntity" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AdapterConclaveAttributeMapping" (
    "Id" integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
    "AdapterConsumerEntityAttributeId" integer NOT NULL,
    "ConclaveEntityAttributeId" integer NOT NULL,
    "CreatedOnUtc" timestamp without time zone NOT NULL,
    "LastUpdatedOnUtc" timestamp without time zone NOT NULL,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_AdapterConclaveAttributeMapping" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AdapterConclaveAttributeMapping_AdapterConsumerEntityAttrib~" FOREIGN KEY ("AdapterConsumerEntityAttributeId") REFERENCES "AdapterConsumerEntityAttribute" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AdapterConclaveAttributeMapping_ConclaveEntityAttribute_Con~" FOREIGN KEY ("ConclaveEntityAttributeId") REFERENCES "ConclaveEntityAttribute" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AdapterConclaveAttributeMapping_AdapterConsumerEntityAttrib~" ON "AdapterConclaveAttributeMapping" ("AdapterConsumerEntityAttributeId");

CREATE INDEX "IX_AdapterConclaveAttributeMapping_ConclaveEntityAttributeId" ON "AdapterConclaveAttributeMapping" ("ConclaveEntityAttributeId");

CREATE INDEX "IX_AdapterConsumerEntity_AdapterConsumerId" ON "AdapterConsumerEntity" ("AdapterConsumerId");

CREATE INDEX "IX_AdapterConsumerEntityAttribute_AdapterConsumerEntityId" ON "AdapterConsumerEntityAttribute" ("AdapterConsumerEntityId");

CREATE INDEX "IX_AdapterSubscription_AdapterConsumerId" ON "AdapterSubscription" ("AdapterConsumerId");

CREATE INDEX "IX_AdapterSubscription_ConclaveEntityId" ON "AdapterSubscription" ("ConclaveEntityId");

CREATE INDEX "IX_ConclaveEntityAttribute_ConclaveEntityId" ON "ConclaveEntityAttribute" ("ConclaveEntityId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210506115542_InitialMigration', '5.0.2');

COMMIT;

