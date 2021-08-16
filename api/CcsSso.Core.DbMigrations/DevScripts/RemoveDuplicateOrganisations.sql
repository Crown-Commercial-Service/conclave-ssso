DELETE FROM "Organisation" a
WHERE a."IsDeleted"!=true AND a."Id" <> (SELECT max(b."Id")
                 FROM   "Organisation" b
                 WHERE  a."CiiOrganisationId" = b."CiiOrganisationId");
